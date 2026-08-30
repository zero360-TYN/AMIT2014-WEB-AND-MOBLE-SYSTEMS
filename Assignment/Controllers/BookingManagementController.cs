using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Assignment.Models;

namespace Assignment.Controllers
{
    public class BookingManagementController(DB db) : Controller
    {
        // Auto-complete expired confirmed bookings on every action request
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

        // Access: BookingManagement/Create
        public IActionResult Create()
        {
            PopulateCreateDropdowns();
            return View(new BookingCreateViewModel());
        }

        // POST: BookingManagement/Create
        [HttpPost]
        public IActionResult Create(BookingCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                PopulateCreateDropdowns(model);
                return View(model);
            }

            // Validate Customer existence
            var customer = db.Accounts
                .Include(a => a.AccountStatus)
                .FirstOrDefault(a => a.Id == model.AccountId && a.AccountStatus.Status != AccountStatusType.deleted);
            if (customer == null)
            {
                ModelState.AddModelError("AccountId", "Selected customer does not exist or is deleted.");
                PopulateCreateDropdowns(model);
                return View(model);
            }

            // Validate Staff existence
            var staff = db.Staffs
                .Include(s => s.Account)
                    .ThenInclude(a => a.AccountStatus)
                .FirstOrDefault(s => s.Id == model.StaffId && s.Account.AccountStatus.Status == AccountStatusType.active);
            if (staff == null)
            {
                ModelState.AddModelError("StaffId", "Selected staff does not exist or is inactive.");
                PopulateCreateDropdowns(model);
                return View(model);
            }

            // Validate Service existence
            var service = db.Services.FirstOrDefault(s => s.Id == model.ServiceId && !s.IsDeleted);
            if (service == null)
            {
                ModelState.AddModelError("ServiceId", "Selected service does not exist or is deleted.");
                PopulateCreateDropdowns(model);
                return View(model);
            }

            // Validate Room existence
            var room = db.Rooms.Include(r => r.RoomType).FirstOrDefault(r => r.Id == model.RoomId && !r.IsDeleted);
            if (room == null)
            {
                ModelState.AddModelError("RoomId", "Selected room does not exist or is deleted.");
                PopulateCreateDropdowns(model);
                return View(model);
            }

            // Validate Category consistency
            if (room.RoomType?.ServiceCategoryId != service.ServiceCategoryId)
            {
                ModelState.AddModelError("RoomId", "Selected room's category does not match the service's category.");
                PopulateCreateDropdowns(model);
                return View(model);
            }

            // Calculate EndTime
            var endTime = model.StartTime.AddMinutes(service.DurationMinutes);

            // Room schedule conflict check
            var roomConflict = db.Bookings.Any(b => b.RoomId == model.RoomId &&
                b.Status != BookingStatus.cancelled &&
                model.StartTime < b.EndTime && endTime > b.StartTime);

            if (roomConflict)
            {
                ModelState.AddModelError("RoomId", "This room is already booked during the selected time slot.");
                PopulateCreateDropdowns(model);
                return View(model);
            }

            // Staff schedule conflict check
            var staffConflict = db.Bookings.Any(b => b.StaffId == model.StaffId &&
                b.Status != BookingStatus.cancelled &&
                model.StartTime < b.EndTime && endTime > b.StartTime);

            if (staffConflict)
            {
                ModelState.AddModelError("StaffId", "This staff member is already assigned to another booking during this time slot.");
                PopulateCreateDropdowns(model);
                return View(model);
            }

            // Calculate Total Price
            var totalPrice = service.Price + (room.RoomType?.BasePrice ?? 0m);

            var booking = new Booking
            {
                AccountId = model.AccountId,
                StaffId = model.StaffId,
                ServiceId = model.ServiceId,
                RoomId = model.RoomId,
                StartTime = model.StartTime,
                EndTime = endTime,
                Status = BookingStatus.confirmed,
                TotalPrice = totalPrice,
                CreatedAt = DateTime.Now,
                BookingDetail = new BookingDetail
                {
                    PokemonName = model.PokemonName,
                    Notes = model.Notes
                }
            };

            db.Bookings.Add(booking);
            db.SaveChanges();

            TempData["AlertType"] = "success";
            TempData["AlertMessage"] = $"Booking #{booking.Id} for {model.PokemonName} created successfully.";
            return RedirectToAction("List");
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

        // Private helpers
        private void PopulateCreateDropdowns(BookingCreateViewModel? model = null)
        {
            var customers = db.Accounts
                .Include(a => a.AccountDetail)
                .Include(a => a.AccountStatus)
                .Where(a => a.AccountDetail.Role.RoleName == "Member" && a.AccountStatus.Status == AccountStatusType.active)
                .Select(a => new
                {
                    a.Id,
                    DisplayName = $"{a.AccountDetail.Username} ({a.Email})"
                })
                .ToList();

            var staffs = db.Staffs
                .Include(s => s.Account)
                    .ThenInclude(a => a.AccountDetail)
                .Include(s => s.Account)
                    .ThenInclude(a => a.AccountStatus)
                .Where(s => s.Account.AccountStatus.Status == AccountStatusType.active)
                .Select(s => new
                {
                    s.Id,
                    DisplayName = s.Account.AccountDetail.Username
                })
                .ToList();

            var services = db.Services
                .Include(s => s.ServiceCategory)
                .Where(s => !s.IsDeleted)
                .Select(s => new
                {
                    s.Id,
                    DisplayName = $"{s.Name} ({s.ServiceCategory.Name}) - {s.DurationMinutes}m, RM{s.Price}"
                })
                .ToList();

            var selectedService = (model != null && model.ServiceId > 0)
                ? db.Services.FirstOrDefault(s => s.Id == model.ServiceId && !s.IsDeleted)
                : null;

            var roomQuery = db.Rooms
                .Include(r => r.RoomType)
                    .ThenInclude(rt => rt.ServiceCategory)
                .Where(r => !r.IsDeleted);

            if (selectedService != null)
            {
                roomQuery = roomQuery.Where(r => r.RoomType.ServiceCategoryId == selectedService.ServiceCategoryId);
            }
            else
            {
                roomQuery = roomQuery.Where(r => false);
            }

            var rooms = roomQuery
                .Select(r => new
                {
                    r.Id,
                    DisplayName = $"Room {r.RoomNumber} ({r.RoomType.Name}) +RM{r.RoomType.BasePrice:F2}"
                })
                .ToList();

            ViewBag.Customers = new SelectList(customers, "Id", "DisplayName", model?.AccountId);
            ViewBag.Staffs = new SelectList(staffs, "Id", "DisplayName", model?.StaffId);
            ViewBag.Services = new SelectList(services, "Id", "DisplayName", model?.ServiceId);
            ViewBag.Rooms = new SelectList(rooms, "Id", "DisplayName", model?.RoomId);
        }
    }
}
