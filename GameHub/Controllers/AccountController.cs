using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using GameHub.Models;
using Microsoft.Owin.Security; // OWIN
using Microsoft.Owin.Security.Cookies; // ensure cookie types
using System.Security.Claims; // for ClaimTypes
using System.Threading.Tasks; // async tasks

namespace GameHub.Controllers
{
    public class AccountController : Controller
    {
        private GameManagementMISEntities db = new GameManagementMISEntities();

        // GET: Account/Login
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string username, string password, string returnUrl)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                TempData["LoginError"] = "Username and password are required.";
                return View();
            }

            username = username.Trim();
            password = password.Trim();

            // Check for admin credentials
            if (username.Equals("admin", StringComparison.OrdinalIgnoreCase) && password == "admin123")
            {
                SetAuthSession(0, "Administrator", "Admin");
                return RedirectToAction("Index", "Admin");
            }

            // Lookup user by email or full name (case-insensitive)
            string unameLower = username.ToLower();
            var user = db.Users.FirstOrDefault(u => u.IsActive == true && (
                (u.Email != null && u.Email.ToLower() == unameLower) ||
                (u.FullName != null && u.FullName.ToLower() == unameLower)
            ));

            if (user == null)
            {
                TempData["LoginError"] = "Account not found. Please use the email you registered with.";
                return View();
            }

            if (!VerifyPassword(password, user.PasswordHash))
            {
                // Helpful hint for debugging — admin can use diagnostics page instead of exposing hashes here
                TempData["LoginError"] = "Invalid username or password.";
                return View();
            }

            SetAuthSession(user.UserID, user.FullName, user.UserType ?? "User");
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Games");
        }

        // GET: Account/Register
        public ActionResult Register()
        {
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(string fullName, string email, string password, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["RegisterError"] = "All fields are required.";
                return View();
            }

            if (password != confirmPassword)
            {
                TempData["RegisterError"] = "Passwords do not match.";
                return View();
            }

            if (db.Users.Any(u => u.Email == email))
            {
                TempData["RegisterError"] = "Email already registered.";
                return View();
            }

            var user = new User
            {
                FullName = fullName,
                Email = email,
                PasswordHash = HashPassword(password),
                JoinDate = DateTime.UtcNow,
                UserType = "User",
                IsActive = true
            };

            db.Users.Add(user);
            db.SaveChanges();

            TempData["RegisterSuccess"] = "Registration successful! Please log in.";
            return RedirectToAction("Login");
        }

        // GET: Account/Logout
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Index", "Games");
        }

        [AllowAnonymous]
        public ActionResult GoogleLogin(string returnUrl)
        {
            // Trigger Google challenge manually
            var redirectUri = Url.Action("GoogleCallback", "Account", new { returnUrl }, protocol: Request.Url.Scheme);
            var properties = new AuthenticationProperties { RedirectUri = redirectUri };
            HttpContext.GetOwinContext().Authentication.Challenge(properties, "Google");
            return new HttpUnauthorizedResult();
        }

        [AllowAnonymous]
        public async Task<ActionResult> GoogleCallback(string returnUrl)
        {
            var auth = HttpContext.GetOwinContext().Authentication;
            var ticket = await auth.AuthenticateAsync("ExternalCookie");
            var externalIdentity = ticket?.Identity;
            if (externalIdentity == null)
            {
                TempData["LoginError"] = "Google login failed.";
                return RedirectToAction("Login");
            }

            var email = externalIdentity.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["LoginError"] = "Google account has no public email.";
                return RedirectToAction("Login");
            }

            var displayName = externalIdentity.Name ?? email;
            var user = db.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                user = new User
                {
                    FullName = displayName?.Trim().Length == 0 ? email : displayName.Trim(),
                    Email = email.Trim(),
                    // Provide random opaque placeholder so DB required column is satisfied and cannot be used to login manually.
                    PasswordHash = GenerateExternalPasswordStub(),
                    JoinDate = DateTime.UtcNow,
                    UserType = "User",
                    IsActive = true
                };
                try
                {
                    db.Users.Add(user);
                    db.SaveChanges();
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                {
                    var details = string.Join("; ", ex.EntityValidationErrors.SelectMany(e => e.ValidationErrors).Select(v => v.PropertyName + ":" + v.ErrorMessage));
                    TempData["LoginError"] = "Account creation failed: " + details;
                    return RedirectToAction("Login");
                }
            }

            SetAuthSession(user.UserID, user.FullName, user.UserType ?? "User");
            auth.SignOut("ExternalCookie");

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Games");
        }

        private void SetAuthSession(int userId, string userName, string userType)
        {
            Session["UserID"] = userId;
            Session["UserName"] = userName;
            Session["UserType"] = userType;
            Session["IsAuthenticated"] = true;
        }

        private string HashPassword(string password)
        {
            // Per user request, store raw password (insecure). Trim whitespace.
            return password?.Trim();
        }

        private bool VerifyPassword(string password, string hash)
        {
            if (password == null || hash == null) return false;
            return password.Trim() == hash;
        }

        private string GenerateExternalPasswordStub()
        {
            return "ext-" + Guid.NewGuid().ToString("N");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}