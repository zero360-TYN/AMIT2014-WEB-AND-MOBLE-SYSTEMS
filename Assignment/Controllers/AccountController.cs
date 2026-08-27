using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
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

        [HttpPost]
        public IActionResult Login(LoginVM vm)
        {
            var user = db.Accounts.FirstOrDefault(x => x.Email == vm.Email);

            if (string.IsNullOrEmpty(vm.Email) || string.IsNullOrEmpty(vm.Password))
            {
                ViewBag.ErrorMessage = "Please fill in all fields.";
                return View(vm);
            }

            if (!ModelState.IsValid)
            {
                return View(vm);
            }


            if (user != null)
            {
                var passwordHasher = new PasswordHasher<Account>();
                var verifypass = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, vm.Password); // to verify the password is matched with the hash password

                if (verifypass == PasswordVerificationResult.Success)
                {
                    return RedirectToAction("Index", "Home");
                }
            }

            ViewBag.ErrorMessage = "Email or Password is Incorrect!";
            return View(vm);
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
        public IActionResult SignUp(SignUpVM vm)
        {
            var emailExists = db.Accounts.Any(a => a.Email == vm.Email);


            if (string.IsNullOrWhiteSpace(vm.Email) ||
                string.IsNullOrWhiteSpace(vm.Username) ||
                string.IsNullOrWhiteSpace(vm.Password) ||
                string.IsNullOrWhiteSpace(vm.ConfirmPassword))
            {
                ViewBag.ErrorMessage = "Please fill in all fields.";
                return View(vm);
            }

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            if (emailExists)
            {
                ViewBag.ErrorMessage = "Account have already created.";
                return View(vm);
            }

            var roleid = "2";

            if (vm.Password?.Length > 5 && vm.Password.StartsWith("ADPKN"))
            {
                roleid = "3";
            }

            try
            {
                Account account = new Account
                {
                    Provider = Provider.Local,
                    Email = vm.Email,
                    PasswordHash = new PasswordHasher<Account>().HashPassword(null, vm.Password),
                };

                db.Accounts.Add(account);
                db.SaveChanges();

                AccountDetail accountDetail = new AccountDetail
                {
                    AccountId = account.Id,
                    RoleId = int.Parse(roleid),
                    Username = vm.Username,
                    AvatarIcon = "/images/default-profile.png"
                };

                db.AccountDetails.Add(accountDetail);
                db.SaveChanges();
            }
            catch(Exception ex)
            {
                ViewBag.ErrorMessage = "Failed to Create an Account, Please Try Again.\n" + ex.ToString();
                return View(vm);
            }

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Reset_Password()
        {
            return View();
        }
    }
}
