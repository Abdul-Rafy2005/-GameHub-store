using System.Web.Mvc;

namespace GameHub.Controllers
{
    public class HomeController : Controller
    {
        // GET: Home/Index - Redirect to Store
        public ActionResult Index()
        {
            return RedirectToAction("Index", "Games");
        }

        // GET: Home/About
        public ActionResult About()
        {
            ViewBag.Message = "GameHub - Your digital game platform.";
            return View();
        }
    }
}