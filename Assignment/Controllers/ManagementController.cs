using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Controllers
{
    public class ManagementController(DB db, IWebHostEnvironment en, IConfiguration cf) : Controller
    {
        public IActionResult User(UserVM vm)
        {
            var users = db.Accounts
                         .Include(a => a.AccountDetail)
                         .Select(a => new UserVM
                         {
                             Id = a.Id,
                             Email = a.Email,
                             accountDetail = a.AccountDetail,
                         }).ToList();

            return View(users); 
        }



        public IActionResult SearchUser()
        {
            return View();
        }
    }
}