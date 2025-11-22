using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using GameHub.Models;

namespace GameHub.Controllers
{
    public class TopSellersController : Controller
    {
        private GameManagementMISEntities db = new GameManagementMISEntities();

        // GET: TopSellers
        public ActionResult Index()
        {
            // Get top 20 games by transaction count
            var topGames = db.Transactions
                .GroupBy(t => t.GameID)
                .Select(g => new { GameID = g.Key, SalesCount = g.Count() })
                .OrderByDescending(g => g.SalesCount)
                .Take(20)
                .ToList();

            var gameIds = topGames.Select(g => g.GameID).ToList();
            var games = db.Games
                .Where(g => gameIds.Contains(g.GameID))
                .ToList()
                .OrderBy(g => gameIds.IndexOf(g.GameID)); // Maintain sales order

            // Get user library status
            var userId = (Session["UserID"] as int?) ?? 0;
            if (userId > 0)
            {
                var userLibrary = db.UserLibraries
                    .Where(ul => ul.UserID == userId)
                    .Select(ul => new { ul.GameID, ul.PurchaseDate })
                    .ToList();
                ViewBag.UserLibrary = userLibrary.ToDictionary(
                    ul => ul.GameID, 
                    ul => ul.PurchaseDate != null ? "owned" : "wishlist"
                );
            }
            else
            {
                ViewBag.UserLibrary = new Dictionary<int, string>();
            }

            // Pass sales data to view
            ViewBag.SalesData = topGames.ToDictionary(g => g.GameID, g => g.SalesCount);
            ViewBag.GenresItems = db.Genres.OrderBy(g => g.GenreName).ToList();

            return View(games.ToList());
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}