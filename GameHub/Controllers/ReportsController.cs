using System;
using System.Linq;
using System.Web.Mvc;
using GameHub.Models;
using GameHub.Filters;

namespace GameHub.Controllers
{
    [AdminAuthorize]
    public class ReportsController : Controller
    {
        private readonly GameManagementMISEntities db = new GameManagementMISEntities();

        // GET: Reports/SalesSummary
        public ActionResult SalesSummary(DateTime? from, DateTime? to)
        {
            var start = from ?? DateTime.UtcNow.AddMonths(-1);
            var end = to ?? DateTime.UtcNow;

            var transactions = db.Transactions.Where(t => t.PurchaseDate >= start && t.PurchaseDate <= end);
            var totalSales = transactions.Any() ? transactions.Sum(t => (decimal?)t.PriceAtPurchase) ?? 0 : 0;
            var totalCount = transactions.Count();
            var byGame = transactions
                .GroupBy(t => t.Game.Title)
                .Select(g => new SalesItem { Label = g.Key, Quantity = g.Count(), Amount = g.Sum(x => x.PriceAtPurchase) })
                .OrderByDescending(x => x.Amount)
                .ToList();

            var model = new SalesSummaryViewModel
            {
                From = start,
                To = end,
                TotalSales = totalSales,
                TotalTransactions = totalCount,
                Items = byGame
            };

            return View(model);
        }

        // GET: Reports/ActivityLogs
        public ActionResult ActivityLogs(DateTime? from, DateTime? to)
        {
            var start = from ?? DateTime.UtcNow.AddMonths(-1);
            var end = to ?? DateTime.UtcNow;

            var backups = db.BackupLogs.Where(b => b.BackupDate >= start && b.BackupDate <= end)
                .OrderByDescending(b => b.BackupDate)
                .ToList();

            return View(backups);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public class SalesSummaryViewModel
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public decimal TotalSales { get; set; }
        public int TotalTransactions { get; set; }
        public System.Collections.Generic.List<SalesItem> Items { get; set; }
    }

    public class SalesItem
    {
        public string Label { get; set; }
        public int Quantity { get; set; }
        public decimal Amount { get; set; }
    }
}
