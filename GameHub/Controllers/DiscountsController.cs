using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using GameHub.Models;
using GameHub.Filters;

namespace GameHub.Controllers
{
    [AdminAuthorize]
    public class DiscountsController : Controller
    {
        private GameManagementMISEntities db = new GameManagementMISEntities();

        // GET: Discounts
        public ActionResult Index()
        {
            return View(db.Discounts.Include(d => d.Games).ToList());
        }

        // GET: Discounts/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Discount discount = db.Discounts.Include(d => d.Games).FirstOrDefault(d => d.DiscountID == id);
            if (discount == null)
            {
                return HttpNotFound();
            }
            return View(discount);
        }

        // GET: Discounts/Create
        public ActionResult Create()
        {
            ViewBag.Games = db.Games.OrderBy(g => g.Title).ToList();
            return View();
        }

        // POST: Discounts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "DiscountID,DiscountName,DiscountPercent,StartDate,EndDate")] Discount discount, int[] selectedGames)
        {
            if (ModelState.IsValid)
            {
                db.Discounts.Add(discount);
                
                // Assign selected games to discount
                if (selectedGames != null && selectedGames.Any())
                {
                    foreach (var gameId in selectedGames)
                    {
                        var game = db.Games.Find(gameId);
                        if (game != null)
                        {
                            discount.Games.Add(game);
                        }
                    }
                }
                
                db.SaveChanges();
                TempData["ToastSuccess"] = "Discount created successfully!";
                return RedirectToAction("Index");
            }

            ViewBag.Games = db.Games.OrderBy(g => g.Title).ToList();
            return View(discount);
        }

        // GET: Discounts/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Discount discount = db.Discounts.Include(d => d.Games).FirstOrDefault(d => d.DiscountID == id);
            if (discount == null)
            {
                return HttpNotFound();
            }
            
            ViewBag.Games = db.Games.OrderBy(g => g.Title).ToList();
            ViewBag.SelectedGameIds = discount.Games.Select(g => g.GameID).ToList();
            return View(discount);
        }

        // POST: Discounts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "DiscountID,DiscountName,DiscountPercent,StartDate,EndDate")] Discount discount, int[] selectedGames)
        {
            if (ModelState.IsValid)
            {
                var existingDiscount = db.Discounts.Include(d => d.Games).FirstOrDefault(d => d.DiscountID == discount.DiscountID);
                if (existingDiscount == null)
                {
                    return HttpNotFound();
                }

                // Update discount properties
                existingDiscount.DiscountName = discount.DiscountName;
                existingDiscount.DiscountPercent = discount.DiscountPercent;
                existingDiscount.StartDate = discount.StartDate;
                existingDiscount.EndDate = discount.EndDate;

                // Clear existing game associations
                existingDiscount.Games.Clear();

                // Add new game associations
                if (selectedGames != null && selectedGames.Any())
                {
                    foreach (var gameId in selectedGames)
                    {
                        var game = db.Games.Find(gameId);
                        if (game != null)
                        {
                            existingDiscount.Games.Add(game);
                        }
                    }
                }

                db.SaveChanges();
                TempData["ToastSuccess"] = "Discount updated successfully!";
                return RedirectToAction("Index");
            }
            
            ViewBag.Games = db.Games.OrderBy(g => g.Title).ToList();
            ViewBag.SelectedGameIds = selectedGames?.ToList() ?? new List<int>();
            return View(discount);
        }

        // GET: Discounts/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Discount discount = db.Discounts.Include(d => d.Games).FirstOrDefault(d => d.DiscountID == id);
            if (discount == null)
            {
                return HttpNotFound();
            }
            return View(discount);
        }

        // POST: Discounts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Discount discount = db.Discounts.Include(d => d.Games).FirstOrDefault(d => d.DiscountID == id);
            
            // Clear game associations before deleting
            discount.Games.Clear();
            
            db.Discounts.Remove(discount);
            db.SaveChanges();
            TempData["ToastSuccess"] = "Discount deleted successfully!";
            return RedirectToAction("Index");
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
}
