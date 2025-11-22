using System;
using System.Linq;
using System.Web.Mvc;
using GameHub.Models;
using GameHub.Filters;

namespace GameHub.Controllers
{
    [AdminAuthorize]
    public class UserManagementController : Controller
    {
        private GameManagementMISEntities db = new GameManagementMISEntities();

        // GET: UserManagement/ResetPassword/{id}
        public ActionResult ResetPassword(int id)
        {
            var user = db.Users.Find(id);
            if (user == null)
            {
                TempData["ToastError"] = "User not found.";
                return RedirectToAction("Index", "Users");
            }

            ViewBag.User = user;
            return View();
        }

        // POST: UserManagement/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(int userId, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword))
            {
                TempData["ToastError"] = "Password cannot be empty.";
                return RedirectToAction("ResetPassword", new { id = userId });
            }

            if (newPassword != confirmPassword)
            {
                TempData["ToastError"] = "Passwords do not match.";
                return RedirectToAction("ResetPassword", new { id = userId });
            }

            var user = db.Users.Find(userId);
            if (user == null)
            {
                TempData["ToastError"] = "User not found.";
                return RedirectToAction("Index", "Users");
            }

            // Hash the password using Base64 (same as registration)
            user.PasswordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(newPassword));
            db.SaveChanges();

            TempData["ToastSuccess"] = $"Password reset successfully for {user.FullName}. New password: {newPassword}";
            return RedirectToAction("Index", "Users");
        }

        // POST: UserManagement/FixAllPasswords
        // This will re-encode all existing passwords to Base64 format
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult FixAllPasswords()
        {
            try
            {
                var users = db.Users.ToList();
                int fixedCount = 0;

                foreach (var user in users)
                {
                    // Check if password is already Base64 encoded (will have = at end usually)
                    // or if it's plain text
                    if (!string.IsNullOrEmpty(user.PasswordHash))
                    {
                        try
                        {
                            // Try to decode - if it fails, it's not Base64
                            var decoded = Convert.FromBase64String(user.PasswordHash);
                            // Already Base64, skip
                        }
                        catch
                        {
                            // Not Base64, encode it
                            user.PasswordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(user.PasswordHash));
                            fixedCount++;
                        }
                    }
                }

                db.SaveChanges();
                TempData["ToastSuccess"] = $"Fixed {fixedCount} user password(s) to proper format.";
            }
            catch (Exception ex)
            {
                TempData["ToastError"] = "Error fixing passwords: " + ex.Message;
            }

            return RedirectToAction("Index", "Users");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}