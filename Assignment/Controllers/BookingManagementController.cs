using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Assignment.Models;

namespace Assignment.Controllers
{
    public class BookingManagementController(DB db) : Controller
    {
        // Auto-update expired bookings (completed for confirmed, cancelled for pending) on every action request
        public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
            db.AutoCompleteExpiredBookings();
        }

        // Access: BookingManagement/Index
        public IActionResult Index()
        {
            ViewBag.TotalBookings = db.Bookings.Count();
            ViewBag.PendingBookings = db.Bookings.Count(b => b.Status == BookingStatus.pending);
            ViewBag.ConfirmedBookings = db.Bookings.Count(b => b.Status == BookingStatus.confirmed);
            ViewBag.CompletedBookings = db.Bookings.Count(b => b.Status == BookingStatus.completed);
            ViewBag.CancelledBookings = db.Bookings.Count(b => b.Status == BookingStatus.cancelled);

            return View();
        }

        // Access: BookingManagement/List
        public IActionResult List(BookingStatus? status, string? search, string? searchBy)
        {
            var query = db.Bookings
                .Include(b => b.Account)
                    .ThenInclude(a => a.AccountDetail)
                .Include(b => b.BookingDetail)
                .Include(b => b.Service)
                .Include(b => b.Room)
                    .ThenInclude(r => r.RoomType)
                .Include(b => b.Staff)
                    .ThenInclude(s => s.Account)
                        .ThenInclude(a => a.AccountDetail)
                .AsQueryable();

            if (status != null)
            {
                query = query.Where(b => b.Status == status.Value);
            }

            query = query.SearchBy(search, searchBy);

            var bookings = query.OrderByDescending(b => b.StartTime).ToList();

            var tableData = new TableListingViewModel
            {
                Headers = new List<string> { "Booking ID", "Customer", "Pokemon", "Service", "Room", "Staff", "Schedule", "Status", "Total Price", "Actions" }
            };

            foreach (var b in bookings)
            {
                var customerName = b.Account?.AccountDetail?.Username ?? "N/A";
                var pokemonName = b.BookingDetail?.PokemonName ?? "N/A";
                var serviceName = b.Service?.Name ?? "N/A";
                var roomNumber = b.Room?.RoomNumber ?? "N/A";
                var staffName = b.Staff?.Account?.AccountDetail?.Username ?? "N/A";
                var schedule = $"{b.StartTime:yyyy-MM-dd HH:mm} - {b.EndTime:HH:mm}";
                var statusText = b.Status.ToString().ToUpper();
                var priceText = b.TotalPrice.ToString("C", new System.Globalization.CultureInfo("en-MY"));

                var actionLinks = $"<a href='/BookingManagement/Details/{b.Id}'>Details</a>";

                var row = new List<string>
                {
                    b.Id.ToString(),
                    customerName,
                    pokemonName,
                    serviceName,
                    roomNumber,
                    staffName,
                    schedule,
                    statusText,
                    priceText,
                    actionLinks
                };
                tableData.Rows.Add(row);
            }

            ViewBag.CurrentStatus = status;

            if (Request.IsAjax())
            {
                return PartialView("_TableList", tableData);
            }

            return View(tableData);
        }



        // Access: BookingManagement/Details/{id}
        public IActionResult Details(int? id)
        {
            if (id == null || id <= 0)
            {
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Invalid Booking ID.";
                return RedirectToAction("List");
            }

            var booking = db.Bookings
                .Include(b => b.Account)
                    .ThenInclude(a => a.AccountDetail)
                .Include(b => b.BookingDetail)
                .Include(b => b.Service)
                    .ThenInclude(s => s.ServiceCategory)
                .Include(b => b.Room)
                    .ThenInclude(r => r.RoomType)
                .Include(b => b.Staff)
                    .ThenInclude(s => s.Account)
                        .ThenInclude(a => a.AccountDetail)
                .FirstOrDefault(b => b.Id == id);

            if (booking == null)
            {
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Booking not found.";
                return RedirectToAction("List");
            }

            var vm = new BookingDetailsViewModel
            {
                Id = booking.Id,
                CustomerName = booking.Account?.AccountDetail?.Username ?? "N/A",
                CustomerEmail = booking.Account?.Email ?? "N/A",
                PokemonName = booking.BookingDetail?.PokemonName ?? "N/A",
                Notes = booking.BookingDetail?.Notes,
                ServiceName = booking.Service?.Name ?? "N/A",
                ServiceCategoryName = booking.Service?.ServiceCategory?.Name ?? "N/A",
                RoomNumber = booking.Room?.RoomNumber ?? "N/A",
                RoomTypeName = booking.Room?.RoomType?.Name ?? "N/A",
                StaffName = booking.Staff != null ? $"<a href='/StaffManagement/StaffDetails/{booking.Staff.Id}'>{booking.Staff.Account.AccountDetail.Username}</a>" : "N/A",
                StartTime = booking.StartTime,
                EndTime = booking.EndTime,
                Status = booking.Status,
                TotalPrice = booking.TotalPrice,
                CreatedAt = booking.CreatedAt
            };

            return View(vm);
        }

        // Access: BookingManagement/GetRoomsByService?serviceId=X
        [HttpGet]
        public IActionResult GetRoomsByService(int serviceId)
        {
            var service = db.Services.FirstOrDefault(s => s.Id == serviceId && !s.IsDeleted);
            if (service == null)
            {
                return Json(new List<object>());
            }

            var rooms = db.Rooms
                .Include(r => r.RoomType)
                .Where(r => !r.IsDeleted && r.RoomType.ServiceCategoryId == service.ServiceCategoryId)
                .Select(r => new
                {
                    id = r.Id,
                    name = $"Room {r.RoomNumber} ({r.RoomType.Name}) +RM{r.RoomType.BasePrice:F2}"
                })
                .ToList();

            return Json(rooms);
        }


    }
}
