using System;
using System.Linq;
using System.Web.Mvc;
using GameHub.Models;

namespace GameHub.Controllers
{
    public class DiagnosticsController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.DatabaseStatus = "Unknown";
            ViewBag.ConnectionString = "";
            ViewBag.GamesCount = 0;
            ViewBag.GenresCount = 0;
            ViewBag.UsersCount = 0;
            ViewBag.Error = null;

            try
            {
                using (var db = new GameManagementMISEntities())
                {
                    ViewBag.ConnectionString = db.Database.Connection.ConnectionString;
                    
                    // Test connection
                    db.Database.Connection.Open();
                    ViewBag.DatabaseStatus = "Connected ?";
                    db.Database.Connection.Close();
                    
                    // Count records
                    ViewBag.GamesCount = db.Games.Count();
                    ViewBag.GenresCount = db.Genres.Count();
                    ViewBag.UsersCount = db.Users.Count();
                }
            }
            catch (Exception ex)
            {
                ViewBag.DatabaseStatus = "Failed ?";
                ViewBag.Error = ex.Message;
                if (ex.InnerException != null)
                {
                    ViewBag.Error += " | Inner: " + ex.InnerException.Message;
                }
            }

            return View();
        }
    }
}