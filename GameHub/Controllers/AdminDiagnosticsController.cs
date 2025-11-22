using System;
using System.Linq;
using System.Web.Mvc;
using GameHub.Filters;
using GameHub.Models;

namespace GameHub.Controllers
{
    [AdminAuthorize]
    public class AdminDiagnosticsController : Controller
    {
        private GameManagementMISEntities db = new GameManagementMISEntities();

        // GET: AdminDiagnostics/CheckUser?email=...&password=...
        // Admin only. Returns simple text about password/hash comparison.
        public ActionResult CheckUser(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Content("Provide email and password as query parameters. Example: /AdminDiagnostics/CheckUser?email=alice@example.com&password=alice123");
            }

            var user = db.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                return Content($"User not found for email: {email}");
            }

            var stored = user.PasswordHash ?? "(empty)";
            string computed = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password ?? string.Empty));
            bool match = string.Equals(stored, computed, StringComparison.Ordinal);

            var msg = $"User: {user.FullName} (ID {user.UserID})\n" +
                      $"Stored PasswordHash: {stored}\n" +
                      $"Computed from provided password: {computed}\n" +
                      $"Match: {match}\n";

            return Content(msg, "text/plain");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
