using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
