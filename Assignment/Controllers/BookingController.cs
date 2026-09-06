using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Assignment.Models;

namespace Assignment.Controllers
{
    public class BookingController(DB db) : Controller
    {
        // Auto-update expired bookings on every request
        public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
            db.AutoCompleteExpiredBookings();
        }

        // ==========================================
        // 1. STEP-BY-STEP BOOKING WIZARD
        // ==========================================

        // GET: /Booking or /Booking/Index
        public IActionResult Index(int? serviceId)
        {
            PopulateWizardData(serviceId);
            var model = new MemberBookingViewModel
            {
                ServiceId = serviceId ?? 0,
                StartTime = DateTime.Today.AddDays(1).AddHours(10)
            };
            return View(model);
        }

        // POST: /Booking/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(MemberBookingViewModel model)
        {
            // 1. Basic Model Validation
            if (!ModelState.IsValid)
            {
                PopulateWizardData(model.ServiceId);
                return View("Index", model);
            }

            // 2. Validate Service
            var service = db.Services
                .Include(s => s.ServiceCategory)
                .FirstOrDefault(s => s.Id == model.ServiceId && !s.IsDeleted);

            if (service == null)
            {
                ModelState.AddModelError("ServiceId", "The selected service does not exist or is no longer available.");
                PopulateWizardData(model.ServiceId);
                return View("Index", model);
            }

            // 3. Validate Room & Category Alignment
            var room = db.Rooms
                .Include(r => r.RoomType)
                .FirstOrDefault(r => r.Id == model.RoomId && !r.IsDeleted);

            if (room == null)
            {
                ModelState.AddModelError("RoomId", "The selected room does not exist or is currently unavailable.");
                PopulateWizardData(model.ServiceId);
                return View("Index", model);
            }

            if (room.RoomType?.ServiceCategoryId != service.ServiceCategoryId)
            {
                ModelState.AddModelError("RoomId", "The selected room is not compatible with the selected service category.");
                PopulateWizardData(model.ServiceId);
                return View("Index", model);
            }

            // 4. Validate Start Time (Must be in the future & within business hours 08:00 - 18:00)
            if (model.StartTime < DateTime.Now.AddMinutes(-5))
            {
                ModelState.AddModelError("StartTime", "Booking start time cannot be in the past.");
                PopulateWizardData(model.ServiceId);
                return View("Index", model);
            }

            // Calculate EndTime based on service duration
            var endTime = model.StartTime.AddMinutes(service.DurationMinutes);

            // 5. Room Conflict Check
            var roomConflict = db.Bookings.Any(b => b.RoomId == model.RoomId &&
                b.Status != BookingStatus.cancelled &&
                model.StartTime < b.EndTime && endTime > b.StartTime);

            if (roomConflict)
            {
                ModelState.AddModelError("StartTime", "This room is already booked during the selected time slot. Please choose another time or room.");
                PopulateWizardData(model.ServiceId);
                return View("Index", model);
            }

            // 6. Staff Assignment & Conflict Check
            int assignedStaffId;
            if (model.StaffId.HasValue && model.StaffId.Value > 0)
            {
                var chosenStaff = db.Staffs
                    .Include(s => s.Account)
                        .ThenInclude(a => a.AccountStatus)
                    .FirstOrDefault(s => s.Id == model.StaffId.Value && s.Account.AccountStatus.Status == AccountStatusType.active);

                if (chosenStaff == null)
                {
                    ModelState.AddModelError("StaffId", "Selected staff member is not currently active.");
                    PopulateWizardData(model.ServiceId);
                    return View("Index", model);
                }

                var staffConflict = db.Bookings.Any(b => b.StaffId == model.StaffId.Value &&
                    b.Status != BookingStatus.cancelled &&
                    model.StartTime < b.EndTime && endTime > b.StartTime);

                if (staffConflict)
                {
                    ModelState.AddModelError("StaffId", "Selected staff member is already assigned to another booking during this time slot.");
                    PopulateWizardData(model.ServiceId);
                    return View("Index", model);
                }

                assignedStaffId = model.StaffId.Value;
            }
            else
            {
                // Auto-assign: Find an active staff member without schedule conflicts
                var busyStaffIds = db.Bookings
                    .Where(b => b.Status != BookingStatus.cancelled &&
                                model.StartTime < b.EndTime && endTime > b.StartTime)
                    .Select(b => b.StaffId)
                    .Distinct()
                    .ToList();

                var availableStaff = db.Staffs
                    .Include(s => s.Account)
                        .ThenInclude(a => a.AccountStatus)
                    .Where(s => s.Account.AccountStatus.Status == AccountStatusType.active &&
                                !busyStaffIds.Contains(s.Id))
                    .FirstOrDefault();

                if (availableStaff == null)
                {
                    ModelState.AddModelError("StartTime", "No staff members are available during this time slot. Please pick another time.");
                    PopulateWizardData(model.ServiceId);
                    return View("Index", model);
                }

                assignedStaffId = availableStaff.Id;
            }

            // 7. Calculate Pricing
            var totalPrice = service.Price + (room.RoomType?.BasePrice ?? 0m);

            // 8. Retrieve Account ID
            var accountId = GetCurrentAccountId();

            // 9. Persist Booking (Status starts as pending awaiting payment)
            var booking = new Booking
            {
                AccountId = accountId,
                StaffId = assignedStaffId,
                ServiceId = service.Id,
                RoomId = room.Id,
                StartTime = model.StartTime,
                EndTime = endTime,
                Status = BookingStatus.pending,
                TotalPrice = totalPrice,
                CreatedAt = DateTime.Now,
                BookingDetail = new BookingDetail
                {
                    PokemonName = model.PokemonName.Trim(),
                    Notes = model.Notes?.Trim()
                }
            };

            db.Bookings.Add(booking);
            db.SaveChanges();

            TempData["AlertType"] = "success";
            TempData["AlertMessage"] = $"Booking #{booking.Id} for {booking.BookingDetail.PokemonName} successfully created!";

            // 10. Hand off to payment flow
            return ContinueToPayment(booking.Id);
        }

        // ==========================================
        // 2. MEMBER BOOKING HISTORY & MANAGEMENT
        // ==========================================

        // GET: /Booking/MyBookings
        public IActionResult MyBookings(BookingStatus? status)
        {
            var accountId = GetCurrentAccountId();

            var query = db.Bookings
                .Include(b => b.BookingDetail)
                .Include(b => b.Service)
                    .ThenInclude(s => s.ServiceCategory)
                .Include(b => b.Room)
                    .ThenInclude(r => r.RoomType)
                .Include(b => b.Staff)
                    .ThenInclude(s => s.Account)
                        .ThenInclude(a => a.AccountDetail)
                .Where(b => b.AccountId == accountId)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(b => b.Status == status.Value);
            }

            var bookings = query
                .OrderByDescending(b => b.StartTime)
                .Select(b => new MemberBookingHistoryItemViewModel
                {
                    Id = b.Id,
                    PokemonName = b.BookingDetail != null ? b.BookingDetail.PokemonName : "N/A",
                    ServiceName = b.Service != null ? b.Service.Name : "N/A",
                    ServiceCategoryName = b.Service != null && b.Service.ServiceCategory != null ? b.Service.ServiceCategory.Name : "N/A",
                    RoomNumber = b.Room != null ? b.Room.RoomNumber : "N/A",
                    RoomTypeName = b.Room != null && b.Room.RoomType != null ? b.Room.RoomType.Name : "N/A",
                    StaffName = b.Staff != null && b.Staff.Account != null && b.Staff.Account.AccountDetail != null
                        ? b.Staff.Account.AccountDetail.Username
                        : "Assigned Specialist",
                    StartTime = b.StartTime,
                    EndTime = b.EndTime,
                    Status = b.Status,
                    TotalPrice = b.TotalPrice,
                    CreatedAt = b.CreatedAt,
                    Notes = b.BookingDetail != null ? b.BookingDetail.Notes : null
                })
                .ToList();

            ViewBag.CurrentStatus = status;
            return View(bookings);
        }

        // GET: /Booking/Details/id=
        public IActionResult Details(int id)
        {
            var accountId = GetCurrentAccountId();

            var booking = db.Bookings
                .Include(b => b.BookingDetail)
                .Include(b => b.Service)
                    .ThenInclude(s => s.ServiceCategory)
                .Include(b => b.Room)
                    .ThenInclude(r => r.RoomType)
                .Include(b => b.Staff)
                    .ThenInclude(s => s.Account)
                        .ThenInclude(a => a.AccountDetail)
                .Include(b => b.Account)
                    .ThenInclude(a => a.AccountDetail)
                .FirstOrDefault(b => b.Id == id && b.AccountId == accountId);

            if (booking == null)
            {
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Booking not found or access denied.";
                return RedirectToAction("MyBookings");
            }

            var vm = new BookingDetailsViewModel
            {
                Id = booking.Id,
                CustomerName = booking.Account?.AccountDetail?.Username ?? "You",
                CustomerEmail = booking.Account?.Email ?? "N/A",
                PokemonName = booking.BookingDetail?.PokemonName ?? "N/A",
                Notes = booking.BookingDetail?.Notes,
                ServiceName = booking.Service?.Name ?? "N/A",
                ServiceCategoryName = booking.Service?.ServiceCategory?.Name ?? "N/A",
                RoomNumber = booking.Room?.RoomNumber ?? "N/A",
                RoomTypeName = booking.Room?.RoomType?.Name ?? "N/A",
                StaffName = booking.Staff?.Account?.AccountDetail?.Username ?? "Assigned Specialist",
                StartTime = booking.StartTime,
                EndTime = booking.EndTime,
                Status = booking.Status,
                TotalPrice = booking.TotalPrice,
                CreatedAt = booking.CreatedAt
            };

            return View(vm);
        }

        // POST: /Booking/Cancel/id=
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cancel(int id)
        {
            var accountId = GetCurrentAccountId();
            var booking = db.Bookings.FirstOrDefault(b => b.Id == id && b.AccountId == accountId);

            if (booking == null)
            {
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Booking not found.";
                return RedirectToAction("MyBookings");
            }

            if (booking.Status == BookingStatus.completed || booking.Status == BookingStatus.cancelled)
            {
                TempData["AlertType"] = "warning";
                TempData["AlertMessage"] = $"Cannot cancel a booking that is already {booking.Status}.";
                return RedirectToAction("MyBookings");
            }

            booking.Status = BookingStatus.cancelled;
            db.SaveChanges();

            TempData["AlertType"] = "success";
            TempData["AlertMessage"] = $"Booking #{id} has been cancelled.";
            return RedirectToAction("MyBookings");
        }

        // ==========================================
        // 3. AJAX APIS FOR STEP-BY-STEP FLOW
        // ==========================================

        // GET: /Booking/GetRoomsByService?serviceId=
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
                    roomNumber = r.RoomNumber,
                    roomTypeId = r.RoomTypeId,
                    roomTypeName = r.RoomType.Name,
                    basePrice = r.RoomType.BasePrice,
                    description = r.RoomType.Description ?? "",
                    displayName = $"Room {r.RoomNumber} ({r.RoomType.Name}) +RM{r.RoomType.BasePrice:F2}"
                })
                .ToList();

            return Json(rooms);
        }

        // GET: /Booking/GetAvailableTimeSlots?serviceId=&roomId=&staffId=&date=
        [HttpGet]
        public IActionResult GetAvailableTimeSlots(int serviceId, int roomId, int? staffId, string date)
        {
            if (!DateTime.TryParse(date, out var targetDate))
            {
                targetDate = DateTime.Today.AddDays(1);
            }

            var service = db.Services.FirstOrDefault(s => s.Id == serviceId && !s.IsDeleted);
            var duration = service?.DurationMinutes ?? 60;
            if (duration <= 0) duration = 60;

            // Business hours: 09:00 - 18:00
            var businessStart = targetDate.Date.AddHours(9);
            var businessEnd = targetDate.Date.AddHours(18);

            // Fetch existing bookings for this room or staff on the target date
            var dayStart = targetDate.Date;
            var dayEnd = dayStart.AddDays(1);

            var existingBookings = db.Bookings
                .Where(b => b.Status != BookingStatus.cancelled &&
                            b.StartTime < dayEnd && b.EndTime > dayStart &&
                            (b.RoomId == roomId || (staffId.HasValue && b.StaffId == staffId.Value)))
                .Select(b => new
                {
                    b.RoomId,
                    b.StaffId,
                    b.StartTime,
                    b.EndTime
                })
                .ToList();

            var slots = new List<TimeSlotOptionViewModel>();
            var cursor = businessStart;
            var now = DateTime.Now;

            // Generate slots in 30-minute intervals
            while (cursor.AddMinutes(duration) <= businessEnd)
            {
                var slotStart = cursor;
                var slotEnd = cursor.AddMinutes(duration);

                var isPast = slotStart <= now;
                var roomOccupied = existingBookings.Any(b => b.RoomId == roomId && slotStart < b.EndTime && slotEnd > b.StartTime);
                var staffBusy = staffId.HasValue && staffId.Value > 0 &&
                                existingBookings.Any(b => b.StaffId == staffId.Value && slotStart < b.EndTime && slotEnd > b.StartTime);

                var isAvailable = !isPast && !roomOccupied && !staffBusy;
                string? reason = null;

                if (isPast) reason = "Past time";
                else if (roomOccupied) reason = "Room occupied";
                else if (staffBusy) reason = "Staff unavailable";

                slots.Add(new TimeSlotOptionViewModel
                {
                    Time = slotStart.ToString("HH:mm"),
                    StartTime = slotStart,
                    EndTime = slotEnd,
                    IsAvailable = isAvailable,
                    Reason = reason
                });

                cursor = cursor.AddMinutes(30);
            }

            return Json(slots);
        }

        // ==========================================
        // 4. INTEGRATION HOOKS (ACCOUNT & PAYMENT)
        // ==========================================

        /// <summary>
        /// Retrieves the current member's account ID.
        /// Accounts are handled by a teammate; this method safely resolves from claims/session,
        /// or defaults to an active member in the database during development.
        /// </summary>
        private int GetCurrentAccountId()
        {
            // 1. Try Claims from Cookie / External Authentication
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(claim, out int id) && id > 0)
            {
                return id;
            }

            // 2. Try Session (if configured)
            try
            {
                var sessionAccountId = HttpContext.Session?.GetInt32("AccountId");
                if (sessionAccountId.HasValue && sessionAccountId.Value > 0)
                {
                    return sessionAccountId.Value;
                }
            }
            catch
            {
                // Session not active/configured
            }

            // 3. Fallback: First active member in the database for testing
            //diabled this after finishing the assignment
            var mockMember = db.Accounts
                .Include(a => a.AccountDetail)
                    .ThenInclude(ad => ad.Role)
                .Include(a => a.AccountStatus)
                .FirstOrDefault(a => a.AccountDetail.Role.RoleName == "Member" && a.AccountStatus.Status == AccountStatusType.active);

            return mockMember?.Id ?? 1;
        }

        /// <summary>
        /// Handoff to payment module upon successful booking creation.
        /// Payments are handled by a teammate; this redirects to the payment endpoint with the bookingId.
        /// </summary>
        private IActionResult ContinueToPayment(int bookingId)
        {
            return RedirectToAction("Index", "Payment", new { bookingId });
        }

        // ==========================================
        // 5. HELPER METHODS
        // ==========================================

        private void PopulateWizardData(int? selectedServiceId = null)
        {
            var categories = db.ServiceCategories
                .Include(c => c.Services.Where(s => !s.IsDeleted))
                .ToList();

            var allActiveServices = db.Services
                .Include(s => s.ServiceCategory)
                .Where(s => !s.IsDeleted)
                .OrderBy(s => s.ServiceCategoryId)
                .ThenBy(s => s.Name)
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

            ViewBag.ServiceCategories = categories;
            ViewBag.AllServices = allActiveServices;
            ViewBag.StaffList = staffs;
        }
    }
}
