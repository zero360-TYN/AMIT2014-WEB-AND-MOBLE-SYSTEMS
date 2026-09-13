using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Controllers
{
    public class StaffManagementController(DB db) : Controller
    {
        // Access: StaffManagement/Index
        public IActionResult Index()
        {
            return View();
        }

        // Access: StaffManagement/Assign
        public IActionResult Assign()
        {
            var acc = db.Accounts.Include(a => a.AccountDetail)
                                 .ThenInclude(ad => ad.Role)
                                 .Include(a => a.AccountStatus)
                                 .Where(a => a.AccountDetail.Role.RoleName == "Member" &&
                                        a.AccountStatus.Status != AccountStatusType.deleted)
                                 .ToList();
            var tableData = new TableListingViewModel
            {
                Headers = new List<string> { "Account ID", "Username", "Email", "Role", "Status", "Actions" }
            };
            foreach (var a in acc)
            {
                var row = new List<string>
                {
                    a.Id.ToString(),
                    a.AccountDetail?.Username ?? "N/A",
                    a.Email ?? "N/A",
                    a.AccountDetail?.Role?.RoleName ?? "N/A",
                    a.AccountStatus?.Status.ToString() ?? "N/A",
                    $"<button type='submit' name='accountId' value='{a.Id}'>Assign</button>"
                };
                tableData.Rows.Add(row);
            }
            return View(tableData);
        }
        // POST: StaffManagement/Assign
        [HttpPost]
        public IActionResult Assign(int accountId)
        {
            var account = db.Accounts.Include(a => a.AccountDetail)
                                        .ThenInclude(ad => ad.Role)
                                     .Include(a => a.AccountStatus)
                                     .FirstOrDefault(a => a.Id == accountId &&
                                                     a.AccountDetail.Role.RoleName == "Member" &&
                                                     a.AccountStatus.Status != AccountStatusType.deleted);
            if (account == null)
            {
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Account not found or cannot be assigned.";
                return RedirectToAction("Assign");
            }
            // Change role to Staff
            var staffRole = db.Roles.FirstOrDefault(r => r.RoleName == "Staff");
            if (staffRole == null)
            {
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Staff role not found.";
                return RedirectToAction("Assign");
            }
            account.AccountDetail.RoleId = staffRole.Id;
            //insert new staff record in staffs table
            var newStaff = new Staff
            {
                AccountId = account.Id
            };
            db.Staffs.Add(newStaff);

            db.SaveChanges();
            TempData["AlertType"] = "success";
            TempData["AlertMessage"] = $"Account {account.AccountDetail?.Username ?? "N/A"} has been assigned as Staff.";
            return RedirectToAction("List");
        }
        //Access: StaffManagement/List
        public IActionResult List(string? search, string? searchBy)
        {
            var query = db.Staffs.Include(s => s.Account)
                                    .ThenInclude(a => a.AccountDetail)
                                  .Include(s => s.Account)
                                    .ThenInclude(a => a.AccountStatus)
                                  .Where(s => s.Account.AccountStatus.Status != AccountStatusType.deleted)
                                  .SearchBy(search, searchBy);

            var staffs = query.ToList();

            var tableData = new TableListingViewModel
            {
                Headers = new List<string> { "Staff ID", "Staff Name", "Email", "Actions" }
            };
            foreach (var s in staffs)
            {
                var row = new List<string>
                {
                    s.Id.ToString(),
                    s.Account?.AccountDetail?.Username ?? "N/A",
                    s.Account?.Email ?? "N/A",
                    $"<a href='/StaffManagement/StaffDetails/{s.Id}'>Details</a>"
                };
                tableData.Rows.Add(row);
            }

            if (Request.IsAjax())
            {
                return PartialView("_TableList", tableData);
            }

            return View(tableData);
        }
        //Access: StaffManagement/StaffDetails/{id}
        public IActionResult StaffDetails(int? id)
        {
            if (id == null || id <= 0)
            {
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Invalid Staff ID.";
                return RedirectToAction("List");
            }

            var staff = db.Staffs
                          .Include(s => s.Account)
                              .ThenInclude(a => a.AccountDetail)
                                  .ThenInclude(ad => ad.Role)
                          .Include(s => s.Account)
                              .ThenInclude(a => a.AccountStatus)
                          .Include(s => s.HandledBookings)
                              .ThenInclude(b => b.Service)
                          .Include(s => s.HandledBookings)
                              .ThenInclude(b => b.Account)
                                  .ThenInclude(a => a.AccountDetail)
                          .Include(s => s.HandledBookings)
                              .ThenInclude(b => b.BookingDetail)
                          .Include(s => s.HandledBookings)
                              .ThenInclude(b => b.Room)
                          .FirstOrDefault(s => s.Id == id &&
                                          s.Account.AccountStatus.Status != AccountStatusType.deleted);

            if (staff == null)
            {
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Staff not found.";
                return RedirectToAction("List");
            }

            // Map handled bookings to calendar slot event items with structured metadata payload
            var bookingEvents = (staff.HandledBookings ?? new List<Booking>())
                .Where(b => b.Status != BookingStatus.cancelled)
                .Select(b => new SlotEventData
                {
                    Id = b.Id.ToString(),
                    // Concise and clean display on calendar grid
                    Text = $"{b.Service?.Name ?? "Service"}",
                    Start = b.StartTime,
                    End = b.EndTime,
                    Status = b.Status.ToString(),
                    BackColor = b.Status switch
                    {
                        BookingStatus.confirmed => "#0d6efd", // Confirmed (Blue)
                        BookingStatus.pending => "#ffc107",   // Pending (Amber)
                        BookingStatus.completed => "#198754", // Completed (Green)
                        BookingStatus.cancelled => "#dc3545", // Cancelled (Red)
                        _ => "#6c757d"
                    },
                    // Detailed business metadata for clear modal presentation
                    Data = new
                    {
                        bookingId = b.Id,
                        customerName = b.Account?.AccountDetail?.Username ?? "N/A",
                        customerEmail = b.Account?.Email ?? "N/A",
                        pokemonName = b.BookingDetail?.PokemonName ?? "N/A",
                        serviceName = b.Service?.Name ?? "N/A",
                        roomNumber = b.Room != null ? $"Room {b.Room.RoomNumber}" : "N/A",
                        totalPrice = b.TotalPrice.ToString("C", new System.Globalization.CultureInfo("en-MY")),
                        status = b.Status.ToString()
                    }
                }).ToList();

            var vm = new StaffDetailsViewModel
            {
                Id = staff.Id,
                Username = staff.Account?.AccountDetail?.Username ?? "N/A",
                Email = staff.Account?.Email ?? "N/A",
                RoleName = staff.Account?.AccountDetail?.Role?.RoleName ?? "N/A",
                AvatarIcon = GetIconUrl(staff.Account?.AccountDetail?.AvatarIcon),
                Status = staff.Account?.AccountStatus?.Status ?? AccountStatusType.active,
                BlockingReason = staff.Account?.AccountStatus?.BlockingReason,
                BlockBy = staff.Account?.AccountStatus?.BlockBy,
                BookingEvents = bookingEvents
            };

            return View(vm);
        }

        //Access: StaffManagement/Edit/{id}
        public IActionResult Edit(int? id)
        {
            if (id == null || id <= 0)
            {
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Invalid Staff ID.";
                return RedirectToAction("List");
            }
            var staff = db.Staffs.Include(s => s.Account)
                                .ThenInclude(a => a.AccountDetail)
                            .Include(s => s.Account)
                                .ThenInclude(a => a.AccountStatus)
                            .FirstOrDefault(s => s.Id == id &&
                                            s.Account.AccountStatus.Status != AccountStatusType.deleted);
            if(staff == null)
            {
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Staff not found.";
                return RedirectToAction("List");
            }

            var vm = new StaffEditViewModel
            {
                Id = staff.Id,
                Username = staff.Account?.AccountDetail?.Username ?? "N/A",
                AvatarIcon = staff.Account?.AccountDetail?.AvatarIcon ?? "N/A",
                Status = staff.Account?.AccountStatus?.Status ?? AccountStatusType.active
            };
            return View(vm);
        }
        // POST: StaffManagement/Edit
        [HttpPost]
        public IActionResult Edit(StaffEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            if (model.Status == AccountStatusType.blocked && string.IsNullOrWhiteSpace(model.BlockingReason))
            {
                ModelState.AddModelError("BlockingReason", "Reason is required when status is blocked.");
                return View(model);
            }

            var staff = db.Staffs.Include(s => s.Account)
                                    .ThenInclude(a => a.AccountDetail)
                                 .Include(s => s.Account)
                                    .ThenInclude(a => a.AccountStatus)
                                 .FirstOrDefault(s => s.Id == model.Id &&
                                                 s.Account.AccountStatus.Status != AccountStatusType.deleted);
            if (staff == null)
            {
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Staff not found.";
                return RedirectToAction("List");
            }

            staff.Account.AccountDetail.Username = model.Username;
            staff.Account.AccountStatus.Status = model.Status;
            if(model.RemoveAvatar)
            {
                staff.Account.AccountDetail.AvatarIcon = "unknown";
            }
            switch(model.Status)
            {
                case AccountStatusType.blocked:
                    staff.Account.AccountStatus.BlockBy = "Admin";
                    staff.Account.AccountStatus.BlockingReason = model.BlockingReason;
                    break;
                case AccountStatusType.deleted:
                    staff.Account.AccountStatus.DeletedAt = DateTime.Now;
                    break;
                case AccountStatusType.active:
                    staff.Account.AccountStatus.BlockBy = null;
                    staff.Account.AccountStatus.BlockingReason = null;
                    break;
            }
            db.SaveChanges();

            if (model.Status == AccountStatusType.deleted)
            {
                TempData["AlertType"] = "success";
                TempData["AlertMessage"] = "Staff account deleted successfully.";
                return RedirectToAction("List");
            }
            else
            {
                TempData["AlertType"] = "success";
                TempData["AlertMessage"] = "Staff details updated successfully.";
            }

            return RedirectToAction("StaffDetails", new { id = model.Id });
        }

        private string GetIconUrl(string? iconName)
        {
            //TODO: replace the url if something chenges in the future !!!
            string url = $"~/images/avatars/{iconName}.png";
            //if the iconName is not found return the default user.png icon
            string physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "avatars", $"{iconName}.png");
            if (!System.IO.File.Exists(physicalPath))
            {
                url = "~/images/user.png";
            }
            return Url.Content(url);
        }
    }
}
