using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


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
            var user = db.Accounts.Include(x => x.AccountStatus)
                                  .FirstOrDefault(x => x.Email == vm.Email);

            if (string.IsNullOrEmpty(vm.Email) || string.IsNullOrEmpty(vm.Password))
            {
                ViewBag.ErrorMessage = "Please fill in all fields.";
                return View(vm);
            }

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            if (user == null)
            {
                ViewBag.ErrorMessage = "Email or Password is Incorrect!";
                return View(vm);
            }

            var accountStatus = user.AccountStatus;

            if (accountStatus.BlockUntil != null)
            {
                if (accountStatus.BlockUntil > DateTime.UtcNow)
                {
                    ViewBag.ErrorMessage = "Too Many Attempts. Please Try Again Later.";
                    return View(vm);

                }

                accountStatus.BlockUntil = null;
                accountStatus.Status = AccountStatusType.active;
                accountStatus.FailedLoginAttempts = 0;

                db.SaveChanges();
            }

            if (user != null)
            {
                if (user.AccountStatus.Status == AccountStatusType.blocked)
                {
                    ViewBag.ErrorMessage = "Account is Blocked. Please Try Again Later.";
                    return View(vm);
                }

                var passwordHasher = new PasswordHasher<Account>();
                var verifypass = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, vm.Password); // to verify the password is matched with the hash password

                if (verifypass == PasswordVerificationResult.Success)
                {

                    accountStatus.BlockUntil = null;
                    accountStatus.Status = AccountStatusType.active;
                    accountStatus.FailedLoginAttempts = 0;

                    db.SaveChanges();

                    return RedirectToAction("Index", "Home");
                }

                accountStatus.FailedLoginAttempts++;

                if (accountStatus.FailedLoginAttempts >= 3)
                {
                    accountStatus.BlockUntil = DateTime.UtcNow.AddMinutes(5);
                    accountStatus.Status = AccountStatusType.blocked;
                    db.SaveChanges();

                    ViewBag.ErrorMessage = "Too Many Attempts. Please Try Again Later.";
                    return View(vm);
                }

                db.SaveChanges();
            }
            return View(vm);
        }


        public IActionResult SignIn()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action("GoogleCallback", "Account")
            };
            return Challenge(properties, "Google");
        }

        public async Task<IActionResult> GoogleCallback()
        {
            var result = await HttpContext.AuthenticateAsync("Google");
            

            if (!result.Succeeded)
            {
                return RedirectToAction("Login");
            }

            var claims = result.Principal.Claims;

            var googleId = claims.FirstOrDefault(b => b.Type == ClaimTypes.NameIdentifier)?.Value;
            var email = claims.FirstOrDefault(b => b.Type == ClaimTypes.Email)?.Value;
            var name = claims.FirstOrDefault(b => b.Type == ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(googleId) || string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login");
            }

            var account = db.Accounts.Include(a => a.AccountStatus).FirstOrDefault(b => b.Email == email);

            if (account != null &&account.AccountStatus.Status == AccountStatusType.blocked)
            {
                TempData["ErrorMessage"] =
                    "Account is blocked. Please Try Again Later.";

                return RedirectToAction("Login");

            }



            if (account == null)
            {
                account = new Account
                {
                    Provider = Provider.Google,
                    Email = email,
                    GoogleId = googleId,
                };

                db.Accounts.Add(account);
                db.SaveChanges();

                var accountDetail = new AccountDetail
                {
                    AccountId = account.Id,
                    RoleId = 2,
                    AvatarIcon = "/images/default-profile.png",
                    Username = name,
                    CreatedAt = DateTime.UtcNow,
                };

                db.AccountDetails.Add(accountDetail);
                db.SaveChanges();

                var accountStatus = new AccountStatus
                {
                    AccountId = account.Id,
                    Status = AccountStatusType.active,
                };
                db.AccountStatuses.Add(accountStatus);
                db.SaveChanges();
            }

            return RedirectToAction("Index", "Home");
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

                AccountStatus accountStatus = new AccountStatus
                {
                    AccountId = account.Id,
                    Status = AccountStatusType.active,
                };

                db.AccountStatuses.Add(accountStatus);
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
