using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Assignment.Models;

namespace Assignment.Controllers
{
    public class AnalyticsController(DB db) : Controller
    {
        public IActionResult Index()
        {
            db.AutoCompleteExpiredBookings();

            var serviceCategoryCount = db.ServiceCategories.Count();
            var serviceCount = db.Services.Count(s => !s.IsDeleted);

            var roomTypeCount = db.RoomTypes.Count();
            var roomCount = db.Rooms.Count(r => !r.IsDeleted);

            var roleCounts = db.Roles
                .AsNoTracking()
                .Select(r => new
                {
                    RoleName = r.RoleName,
                    Count = r.AccountDetails.Count()
                })
                .ToDictionary(x => x.RoleName, x => x.Count);

            var bookingStatusCounts = Enum.GetValues<BookingStatus>()
                .ToDictionary(
                    status => status,
                    status => db.Bookings.Count(b => b.Status == status)
                );

            var model = new AnalyticsDashboardViewModel
            {
                ServiceCategoryCount = serviceCategoryCount,
                ServiceCount = serviceCount,
                RoomTypeCount = roomTypeCount,
                RoomCount = roomCount,
                AccountRoleCounts = roleCounts,
                BookingStatusCounts = bookingStatusCounts,
                TotalBookings = db.Bookings.Count()
            };

            return View(model);
        }

        // GET: /Analytics/BookingAnalytics?year=?
        public IActionResult BookingAnalytics(int? year)
        {
            var selectedYear = year ?? DateTime.Today.Year;
            var months = GetMonthsInList();

            var monthlyBookings = db.Bookings
                .AsNoTracking()
                .Where(b => b.StartTime.Year == selectedYear &&
                            (b.Status == BookingStatus.completed || b.Status == BookingStatus.cancelled))
                .GroupBy(b => new { b.StartTime.Month, b.Status })
                .Select(g => new { g.Key.Month, g.Key.Status, Count = g.Count() })
                .ToList();

            var monthlyBookingCounts = monthlyBookings
                .Select(x => (x.Month, x.Status, x.Count));
            var completedSeries = GetMonthlySeries(monthlyBookingCounts, BookingStatus.completed);
            var cancelledSeries = GetMonthlySeries(monthlyBookingCounts, BookingStatus.cancelled);

            var topServices = db.Bookings
                .AsNoTracking()
                .Where(b => b.StartTime.Year == selectedYear &&
                            (b.Status == BookingStatus.completed || b.Status == BookingStatus.cancelled))
                .GroupBy(b => b.Service.Name)
                .Select(g => new
                {
                    ServiceName = g.Key,
                    CompletedCount = g.Count(b => b.Status == BookingStatus.completed),
                    CancelledCount = g.Count(b => b.Status == BookingStatus.cancelled)
                })
                .OrderByDescending(x => x.CompletedCount)
                .ThenBy(x => x.ServiceName)
                .Take(10)
                .ToList();

            var model = new AnalyticsChartsViewModel
            {
                PrimaryChart = EChartHelper.CreateLineChart(
                    $"Completed vs Cancelled Bookings ({selectedYear})", months,
                    [
                        new("Completed", completedSeries, color: "#10b981", area: true),
                        new("Cancelled", cancelledSeries, color: "#ef4444", area: true)
                    ],
                    yAxisName: "Bookings"),
                SecondaryChart = EChartHelper.CreateBarChart(
                    $"Top Performing Services ({selectedYear})",
                    topServices.Select(x => x.ServiceName),
                    [
                        new("Completed Bookings", topServices.Select(x => x.CompletedCount), color: "#10b981"),
                        new("Cancelled Bookings", topServices.Select(x => x.CancelledCount), color: "#ef4444")
                    ],
                    yAxisName: "Bookings",
                    isHorizontal: true,
                    enableLineSwitch: false),
                SelectedYear = selectedYear,
                AvailableYears = GetBookingYears(selectedYear)
            };

            return View(model);
        }

        // GET: /Analytics/RoomAnalytics?year=?
        public IActionResult RoomAnalytics(int? year)
        {
            var selectedYear = year ?? DateTime.Today.Year;
            var roomTypeBookingCounts = db.Bookings
                .AsNoTracking()
                .Where(b => b.StartTime.Year == selectedYear && b.Status == BookingStatus.confirmed)
                .GroupBy(b => b.Room.RoomType.Name)
                .Select(g => new { RoomTypeName = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            var confirmedBookingsByRoomType = roomTypeBookingCounts
                .Select(x => new KeyValuePair<string, decimal>(x.RoomTypeName, x.Count))
                .ToList();

            var model = new AnalyticsChartsViewModel
            {
                PrimaryChart = EChartHelper.CreatePieChart(
                    $"Confirmed Bookings by Room Type ({selectedYear})", confirmedBookingsByRoomType),
                SelectedYear = selectedYear,
                AvailableYears = GetBookingYears(selectedYear)
            };

            return View(model);
        }

        // GET: /Analytics/StaffAnalytics?year=?
        public IActionResult StaffAnalytics(int? year)
        {
            var selectedYear = year ?? DateTime.Today.Year;
            var bookingsByStaff = db.Bookings
                .AsNoTracking()
                .Where(b => b.StartTime.Year == selectedYear &&
                            (b.Status == BookingStatus.completed || b.Status == BookingStatus.cancelled))
                .GroupBy(b => b.Staff.Account.AccountDetail.Username)
                .Select(g => new
                {
                    StaffName = g.Key,
                    CompletedCount = g.Count(b => b.Status == BookingStatus.completed),
                    CancelledCount = g.Count(b => b.Status == BookingStatus.cancelled)
                })
                .OrderByDescending(x => x.CompletedCount)
                .ThenBy(x => x.StaffName)
                .ToList();

            var model = new AnalyticsChartsViewModel
            {
                PrimaryChart = EChartHelper.CreateBarChart(
                    $"Completed vs Cancelled Bookings by Staff ({selectedYear})",
                    bookingsByStaff.Select(x => x.StaffName),
                    [
                        new("Completed Bookings", bookingsByStaff.Select(x => x.CompletedCount), color: "#10b981"),
                        new("Cancelled Bookings", bookingsByStaff.Select(x => x.CancelledCount), color: "#ef4444")
                    ],
                    yAxisName: "Bookings",
                    isHorizontal: true,
                    enableLineSwitch: false),
                SelectedYear = selectedYear,
                AvailableYears = GetBookingYears(selectedYear)
            };

            return View(model);
        }

        // GET: /Analytics/AccountStatusAnalytics
        public IActionResult AccountStatusAnalytics()
        {
            var accountStatuses = db.AccountStatuses
                .AsNoTracking()
                .GroupBy(a => a.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionary(x => x.Status, x => x.Count);

            var model = EChartHelper.CreatePieChart(
                "Account Status Distribution",
                new Dictionary<string, int>
                {
                    ["Active"] = accountStatuses.GetValueOrDefault(AccountStatusType.active),
                    ["Blocked"] = accountStatuses.GetValueOrDefault(AccountStatusType.blocked),
                    ["Deleted"] = accountStatuses.GetValueOrDefault(AccountStatusType.deleted)
                });

            return View(model);
        }

        // GET: /Analytics/UserAnalytics?year=?
        public IActionResult UserAnalytics(int? year)
        {
            var selectedYear = year ?? DateTime.Today.Year;

            var months = GetMonthsInList();

            var monthlyNewUsers = db.AccountDetails
                .AsNoTracking()
                .Where(a => a.CreatedAt.Year == selectedYear)
                .GroupBy(a => a.CreatedAt.Month)
                .Select(g => new { Month = g.Key, Count = g.Count() })
                .ToList();

            var monthlyActiveUsers = db.Bookings
                .AsNoTracking()
                .Where(b => b.CreatedAt.Year == selectedYear)
                .GroupBy(b => b.CreatedAt.Month)
                .Select(g => new { Month = g.Key, Count = g.Select(b => b.AccountId).Distinct().Count() })
                .ToList();

            var newUsersSeries = new List<int>();
            var activeUsersSeries = new List<int>();

            for (int m = 1; m <= 12; m++)
            {
                var newCount = monthlyNewUsers.FirstOrDefault(x => x.Month == m)?.Count ?? 0;
                newUsersSeries.Add(newCount);

                var activeCount = monthlyActiveUsers.FirstOrDefault(x => x.Month == m)?.Count ?? 0;
                activeUsersSeries.Add(activeCount);
            }

            var chartModel = EChartHelper.CreateLineChart(
                title: $"User Growth & Active Customers ({selectedYear})",
                xLabels: months,
                series:
                [
                    new("New Registered Users", newUsersSeries, color: "#3b82f6", area: true),
                    new("Active Booking Users", activeUsersSeries, color: "#10b981")
                ],
                yAxisName: "User Count"
            );

            ViewBag.SelectedYear = selectedYear;

            var availableYears = db.AccountDetails
                .AsNoTracking()
                .Select(a => a.CreatedAt.Year)
                .Union(db.Bookings.Select(b => b.CreatedAt.Year))
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();

            if (!availableYears.Contains(selectedYear))
            {
                availableYears.Add(selectedYear);
                availableYears.Sort((a, b) => b.CompareTo(a));
            }

            ViewBag.AvailableYears = availableYears;

            if (Request.IsAjax())
            {
                return PartialView("_EChart", chartModel);
            }

            return View(chartModel);
        }

        private IEnumerable<string> GetMonthsInList()
        {
            var months = Enumerable.Range(1, 12)
                .Select(m => CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(m))
                .ToList();
            return months;
        }

        private List<int> GetBookingYears(int selectedYear)
        {
            var years = db.Bookings
                .AsNoTracking()
                .Select(b => b.StartTime.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();

            if (!years.Contains(selectedYear))
            {
                years.Add(selectedYear);
                years.Sort((a, b) => b.CompareTo(a));
            }

            return years;
        }

        private static List<int> GetMonthlySeries(
            IEnumerable<(int Month, BookingStatus Status, int Count)> monthlyBookings,
            BookingStatus status)
        {
            return Enumerable.Range(1, 12)
                .Select(month => monthlyBookings
                    .FirstOrDefault(x => x.Month == month && x.Status == status).Count)
                .ToList();
        }
    }

}
