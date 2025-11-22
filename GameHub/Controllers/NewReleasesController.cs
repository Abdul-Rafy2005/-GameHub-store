using System.Linq;
using System.Web.Mvc;
using GameHub.Models;

namespace GameHub.Controllers
{
    public class NewReleasesController : Controller
    {
        private readonly GameManagementMISEntities db = new GameManagementMISEntities();
        public ActionResult Index()
        {
            var games = db.Games.OrderByDescending(g => g.ReleaseDate).Take(24).ToList();
            ViewBag.GenresItems = db.Genres.OrderBy(g => g.GenreName).ToList();
            return View(games);
        }
    }
}