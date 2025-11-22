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
    public class TransactionsController : Controller
    {
        private GameManagementMISEntities db = new GameManagementMISEntities();

        // Public parameterless constructor for MVC
        public TransactionsController() { }

        // Internal constructor for testing
        internal TransactionsController(GameManagementMISEntities context)
        {
            db = context ?? new GameManagementMISEntities();
        }

        // GET: Validate discount code (AJAX endpoint)
        [HttpGet]
        public JsonResult ValidateDiscount(string code, int gameId)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return Json(new { valid = false, message = "Please enter a discount code" }, JsonRequestBehavior.AllowGet);
            }

            var discount = GetValidDiscountForGame(code.Trim(), gameId);
            if (discount != null && discount.DiscountPercent.HasValue)
            {
                return Json(new { 
                    valid = true, 
                    percent = discount.DiscountPercent.Value,
                    code = discount.DiscountName 
                }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { valid = false, message = "Invalid or expired discount code" }, JsonRequestBehavior.AllowGet);
        }

        // GET: Transactions
        [AdminAuthorize]
        public ActionResult Index()
        {
            var transactions = db.Transactions.Include(t => t.Game).Include(t => t.User);
            return View(transactions.ToList());
        }

        // GET: Transactions/Checkout
        [HttpGet]
        public ActionResult Checkout(int gameId, int? userId, string discountCode = null)
        {
            var game = db.Games.Find(gameId);
            if (game == null)
            {
                return HttpNotFound();
            }

            var model = new CheckoutViewModel
            {
                GameID = game.GameID,
                GameTitle = game.Title,
                OriginalPrice = game.Price,
                DiscountCode = discountCode,
                PaymentMethod = "Card",
                UserID = userId
            };

            // Resolve discount preview if a code is provided
            if (!string.IsNullOrWhiteSpace(discountCode))
            {
                var discount = GetValidDiscountForGame(discountCode, game.GameID);
                if (discount != null && discount.DiscountPercent.HasValue)
                {
                    var percent = discount.DiscountPercent.Value;
                    model.DiscountPercent = percent;
                    model.DiscountAmount = Math.Round(game.Price * (percent / 100m), 2);
                }
            }

            model.FinalPrice = Math.Max(0, Math.Round(model.OriginalPrice - model.DiscountAmount, 2));

            ViewBag.Users = new SelectList(db.Users.OrderBy(u => u.FullName).ToList(), "UserID", "FullName", userId);
            return View(model);
        }

        // Workflow: Purchase a game
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Purchase(int userId, int gameId, string paymentMethod = "Card", string discountCode = "")
        {
            var user = db.Users.Find(userId);
            var game = db.Games.Find(gameId);

            if (user == null || game == null)
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = "Invalid user or game." });
                }
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid user or game.");
            }

            // Check if user already purchased this game
            bool alreadyPurchased = db.Transactions.Any(t => t.UserID == userId && t.GameID == gameId);
            if (alreadyPurchased)
            {
                string msg = "You already purchased this game.";
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = msg });
                }
                TempData["PurchaseError"] = msg;
                return RedirectToAction("Index", "UserLibraries");
            }

            // Determine discount if applicable
            decimal discountPercent = 0m;
            if (!string.IsNullOrWhiteSpace(discountCode))
            {
                var discount = GetValidDiscountForGame(discountCode, gameId);
                if (discount != null && discount.DiscountPercent.HasValue)
                {
                    discountPercent = Math.Max(0, discount.DiscountPercent.Value);
                }
            }

            var discountAmount = Math.Round(game.Price * (discountPercent / 100m), 2);
            var finalPrice = Math.Max(0, Math.Round(game.Price - discountAmount, 2));

            using (var tx = TryBeginTransaction())
            {
                try
                {
                    // Create transaction record
                    var transaction = new Transaction
                    {
                        UserID = userId,
                        GameID = gameId,
                        PurchaseDate = DateTime.UtcNow,
                        PriceAtPurchase = finalPrice,
                        DiscountApplied = discountPercent,
                        PaymentMethod = string.IsNullOrWhiteSpace(paymentMethod) ? "Card" : paymentMethod
                    };
                    db.Transactions.Add(transaction);

                    // Update or create UserLibrary entry
                    var library = db.UserLibraries.FirstOrDefault(ul => ul.UserID == userId && ul.GameID == gameId);
                    if (library == null)
                    {
                        // Create new library entry
                        library = new UserLibrary
                        {
                            UserID = userId,
                            GameID = gameId,
                            PurchaseDate = DateTime.UtcNow,
                            ActivationCode = GenerateUniqueActivationCode()
                        };
                        db.UserLibraries.Add(library);
                    }
                    else
                    {
                        // Update existing wishlist entry
                        library.PurchaseDate = DateTime.UtcNow;
                        library.ActivationCode = GenerateUniqueActivationCode();
                        db.Entry(library).State = EntityState.Modified;
                    }

                    // Persist changes
                    db.SaveChanges();
                    if (tx != null) tx.Commit();

                    string successMsg = $"Purchase successful! '{game.Title}' - ${finalPrice}. Activation Code: {library.ActivationCode}";
                    
                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { 
                            success = true, 
                            message = successMsg,
                            activationCode = library.ActivationCode,
                            finalPrice = finalPrice
                        });
                    }

                    TempData["PurchaseSuccess"] = successMsg;
                    TempData["ActivationCode"] = library.ActivationCode;
                    return RedirectToAction("Index", "UserLibraries");
                }
                catch (Exception ex)
                {
                    if (tx != null) tx.Rollback();
                    
                    // Log error for debugging
                    System.Diagnostics.Debug.WriteLine($"Purchase transaction failed: {ex.Message}");
                    
                    string errorMsg = "Purchase failed. Please try again.";
                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = false, message = errorMsg });
                    }
                    
                    TempData["PurchaseError"] = errorMsg;
                    return RedirectToAction("Index", "UserLibraries");
                }
            }
        }

        private DbContextTransaction TryBeginTransaction()
        {
            try
            {
                return db.Database.BeginTransaction();
            }
            catch
            {
                return null;
            }
        }

        private Discount GetValidDiscountForGame(string code, int gameId)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            var now = DateTime.UtcNow;
            code = code.Trim();
            var discount = db.Discounts
                .Where(d => d.DiscountName == code && (!d.StartDate.HasValue || d.StartDate <= now) && (!d.EndDate.HasValue || d.EndDate >= now)
                    && d.Games.Any(g => g.GameID == gameId))
                .FirstOrDefault();
            return discount;
        }

        private string GenerateUniqueActivationCode()
        {
            string code;
            do
            {
                code = Guid.NewGuid().ToString("N").Substring(0, 16).ToUpperInvariant();
            } while (db.UserLibraries.Any(u => u.ActivationCode == code));
            return code;
        }

        // GET: Transactions/Details/5
        [AdminAuthorize]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Transaction transaction = db.Transactions.Find(id);
            if (transaction == null)
            {
                return HttpNotFound();
            }
            return View(transaction);
        }

        // GET: Transactions/Create
        [AdminAuthorize]
        public ActionResult Create()
        {
            ViewBag.GameID = new SelectList(db.Games, "GameID", "Title");
            ViewBag.UserID = new SelectList(db.Users, "UserID", "FullName");
            return View();
        }

        // POST: Transactions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize]
        public ActionResult Create([Bind(Include = "TransactionID,UserID,GameID,PurchaseDate,PriceAtPurchase,DiscountApplied,PaymentMethod")] Transaction transaction)
        {
            if (ModelState.IsValid)
            {
                db.Transactions.Add(transaction);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.GameID = new SelectList(db.Games, "GameID", "Title", transaction.GameID);
            ViewBag.UserID = new SelectList(db.Users, "UserID", "FullName", transaction.UserID);
            return View(transaction);
        }

        // GET: Transactions/Edit/5
        [AdminAuthorize]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Transaction transaction = db.Transactions.Find(id);
            if (transaction == null)
            {
                return HttpNotFound();
            }
            ViewBag.GameID = new SelectList(db.Games, "GameID", "Title", transaction.GameID);
            ViewBag.UserID = new SelectList(db.Users, "UserID", "FullName", transaction.UserID);
            return View(transaction);
        }

        // POST: Transactions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize]
        public ActionResult Edit([Bind(Include = "TransactionID,UserID,GameID,PurchaseDate,PriceAtPurchase,DiscountApplied,PaymentMethod")] Transaction transaction)
        {
            if (ModelState.IsValid)
            {
                db.Entry(transaction).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.GameID = new SelectList(db.Games, "GameID", "Title", transaction.GameID);
            ViewBag.UserID = new SelectList(db.Users, "UserID", "FullName", transaction.UserID);
            return View(transaction);
        }

        // GET: Transactions/Delete/5
        [AdminAuthorize]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Transaction transaction = db.Transactions.Find(id);
            if (transaction == null)
            {
                return HttpNotFound();
            }
            return View(transaction);
        }

        // POST: Transactions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [AdminAuthorize]
        public ActionResult DeleteConfirmed(int id)
        {
            Transaction transaction = db.Transactions.Find(id);
            db.Transactions.Remove(transaction);
            db.SaveChanges();
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

    public class CheckoutViewModel
    {
        public int GameID { get; set; }
        public string GameTitle { get; set; }
        public int? UserID { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalPrice { get; set; }
        public string DiscountCode { get; set; }
        public string PaymentMethod { get; set; }
    }
}
