using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Controllers
{
    public class ManagementController(DB db, IWebHostEnvironment en, IConfiguration cf) : Controller
    {
        public IActionResult User()
        {
            var users = db.Accounts
                         .Include(a => a.AccountDetail).Include(a => a.AccountStatus)
                         .Select(a => new UserVM
                         {
                             Id = a.Id,
                             Email = a.Email,
                             accountDetail = a.AccountDetail,
                             accountStatus = a.AccountStatus,
                         }).ToList();

            return View(users); 
        }

        [HttpPost]
        public IActionResult User(string? search)
        {
            var user = db.Accounts.Include(a => a.AccountDetail)
                         .Include(a => a.AccountStatus)
                         .Where(x => string.IsNullOrEmpty(search) ||
                                x.AccountDetail.Username.Contains(search))
                         .Select(x => new UserVM
                         {
                             Id = x.Id,
                             Email = x.Email,
                             accountDetail = x.AccountDetail,
                             accountStatus = x.AccountStatus,
                         }).ToList();

            ViewBag.Search = search;

            return View(user);
        }


        public IActionResult UserBlock(int id, string reason)
        {
            var accountStatus = db.AccountStatuses.FirstOrDefault(a => a.AccountId == id);

            if (accountStatus != null && accountStatus.Status == AccountStatusType.active)
            {
                try
                {
                    accountStatus.Status = AccountStatusType.blocked;
                    accountStatus.BlockingReason = reason;
                    accountStatus.BlockBy = "Admin";

                    db.SaveChanges();
                } catch (Exception ex) 
                {
                    ViewBag.ErrorMessage = "Failed to Block Account, Please Try Again.\n" + ex.ToString();
                    return View();
                }
            }

            return RedirectToAction("User");
        }


        public IActionResult UserUnblock(int id) 
        {
            var accountStatus = db.AccountStatuses.FirstOrDefault(a => a.AccountId == id);

            if (accountStatus != null)
            {
                accountStatus.Status = AccountStatusType.active;
                accountStatus.BlockUntil = null;
                accountStatus.FailedLoginAttempts = 0;
                db.SaveChanges();
            }
            return RedirectToAction("User");
        }
    }
}