using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Assignment.Controllers
{
    public class AccountController(DB db, IWebHostEnvironment en, IConfiguration cf) : Controller
    { 
        // Access: Account/Login
        public IActionResult Login()
        {
            return View();
        }

        public IActionResult SignIn()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = "/index"
            };
            return Challenge(properties, "Google");
        }

        // Access: Account/SignUp
        public IActionResult SignUp()
        {
            return View();
        }

        public IActionResult Reset_Password()
        {
            return View();
        }
    }
}
