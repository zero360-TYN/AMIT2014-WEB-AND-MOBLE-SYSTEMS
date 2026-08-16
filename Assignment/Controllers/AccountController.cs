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

        [HttpPost]
        public IActionResult SignUp(SignUpVM model)
        {
            string ErrorMessage = "";
            if (string.IsNullOrWhiteSpace(model.Email) || 
                string.IsNullOrWhiteSpace(model.Username) || 
                string.IsNullOrWhiteSpace(model.Password) || 
                string.IsNullOrWhiteSpace(model.ConfirmPassword))
            {
                ViewBag.ErrorMessage = "Please fill in all fields.";
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Reset_Password()
        {
            return View();
        }
    }
}
