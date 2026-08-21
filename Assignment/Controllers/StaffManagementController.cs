using Microsoft.AspNetCore.Mvc;

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
            return View();
        }

        //Access: StaffManagement/List
        public IActionResult List()
        {
            var staffs = db.Staffs.Include(s => s.Account).ToList();
            return View();
        }

    }
}
