using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Assignment.Controllers
{
    public class AnalyticsController(DB db) : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction(nameof(UserAnalytics));
        }

        // GET: /Analytics/UserAnalytics?year=2026
        public IActionResult UserAnalytics(int? year)
        {
            var selectedYear = year ?? DateTime.Today.Year;

            var months = Enumerable.Range(1, 12)
                .Select(m => CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(m))
                .ToList();

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
                yAxisName: "User Count",
                height: "400px"
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
    }
}
