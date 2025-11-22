using System;
using System.Linq;
using System.Web.Mvc;
using GameHub.Models;

namespace GameHub.Controllers
{
    public class UpcomingController : Controller
    {
        private readonly GameManagementMISEntities db = new GameManagementMISEntities();
        public ActionResult Index()
        {
            var now = DateTime.UtcNow.Date;
            var games = db.Games.Where(g => g.ReleaseDate > now).OrderBy(g => g.ReleaseDate).Take(24).ToList();
            ViewBag.GenresItems = db.Genres.OrderBy(g => g.GenreName).ToList();
            return View(games);
        }
    }
}