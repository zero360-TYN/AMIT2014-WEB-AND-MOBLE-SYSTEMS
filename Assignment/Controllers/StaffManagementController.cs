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
                          .FirstOrDefault(s => s.Id == id &&
                                          s.Account.AccountStatus.Status != AccountStatusType.deleted);

            if (staff == null)
            {
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Staff not found.";
                return RedirectToAction("List");
            }

            var vm = new StaffDetailsViewModel
            {
                Id = staff.Id,
                Username = staff.Account?.AccountDetail?.Username ?? "N/A",
                Email = staff.Account?.Email ?? "N/A",
                RoleName = staff.Account?.AccountDetail?.Role?.RoleName ?? "N/A",
                AvatarIcon = staff.Account?.AccountDetail?.AvatarIcon ?? "N/A",
                Status = staff.Account?.AccountStatus?.Status ?? AccountStatusType.active,
                BlockingReason = staff.Account?.AccountStatus?.BlockingReason,
                BlockBy = staff.Account?.AccountStatus?.BlockBy
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
    }
}
