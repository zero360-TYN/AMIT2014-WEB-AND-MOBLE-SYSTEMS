using Microsoft.AspNetCore.Mvc;

namespace Assignment.Controllers
{
    public class HomeController(DB db) : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    } 
}
