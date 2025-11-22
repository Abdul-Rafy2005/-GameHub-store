using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using GameHub.Filters;
using GameHub.Models;

namespace GameHub.Controllers
{
    [AdminAuthorize]
    public class AdminController : Controller
    {
        private GameManagementMISEntities db = new GameManagementMISEntities();

        // GET: Admin Dashboard
        public ActionResult Index()
        {
            // Basic counts
            ViewBag.TotalGames = db.Games.Count();
            ViewBag.TotalUsers = db.Users.Where(u => u.UserType != "Admin").Count();
            ViewBag.TotalTransactions = db.Transactions.Count();
            
            // Revenue calculations
            var totalRevenue = db.Transactions.Sum(t => (decimal?)t.PriceAtPurchase) ?? 0;
            ViewBag.TotalRevenue = totalRevenue;

            // Today's stats
            var today = DateTime.Today;
            var todayTransactions = db.Transactions.Where(t => t.PurchaseDate >= today).ToList();
            ViewBag.TodayTransactions = todayTransactions.Count;
            ViewBag.TodayRevenue = todayTransactions.Sum(t => t.PriceAtPurchase);

            // This month stats
            var firstDayOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var monthTransactions = db.Transactions.Where(t => t.PurchaseDate >= firstDayOfMonth).ToList();
            ViewBag.MonthTransactions = monthTransactions.Count;
            ViewBag.MonthRevenue = monthTransactions.Sum(t => t.PriceAtPurchase);

            // Top selling games (Top 5)
            var topGames = db.Transactions
                .GroupBy(t => t.GameID)
                .Select(g => new { 
                    GameID = g.Key, 
                    Sales = g.Count(), 
                    Revenue = g.Sum(t => t.PriceAtPurchase) 
                })
                .OrderByDescending(g => g.Sales)
                .Take(5)
                .ToList();

            var topGamesData = new List<TopGameViewModel>();
            foreach (var tg in topGames)
            {
                var game = db.Games.Find(tg.GameID);
                if (game != null)
                {
                    topGamesData.Add(new TopGameViewModel
                    {
                        GameTitle = game.Title,
                        Sales = tg.Sales,
                        Revenue = tg.Revenue
                    });
                }
            }
            ViewBag.TopGames = topGamesData;

            // Recent transactions (Last 10)
            var recentTransactions = db.Transactions
                .Include(t => t.Game)
                .Include(t => t.User)
                .OrderByDescending(t => t.PurchaseDate)
                .Take(10)
                .ToList();
            ViewBag.RecentTransactions = recentTransactions;

            // Average transaction value
            ViewBag.AverageTransactionValue = totalRevenue > 0 && ViewBag.TotalTransactions > 0
                ? Math.Round(totalRevenue / ViewBag.TotalTransactions, 2)
                : 0;

            return View();
        }

        public class TopGameViewModel
        {
            public string GameTitle { get; set; }
            public int Sales { get; set; }
            public decimal Revenue { get; set; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}