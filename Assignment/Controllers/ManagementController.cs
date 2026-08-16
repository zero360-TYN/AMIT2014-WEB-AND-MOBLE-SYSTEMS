using Microsoft.AspNetCore.Mvc;

namespace Assignment.Controllers
{
    public class ManagementController(DB db, IWebHostEnvironment en, IConfiguration cf) : Controller
    {
        public IActionResult User()
        {
            return View(); 
        }
    }
}