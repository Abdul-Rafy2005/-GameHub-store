using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using GameHub.Models;
using GameHub.Filters;

namespace GameHub.Controllers
{
    public class GamesController : Controller
    {
        private GameManagementMISEntities db = new GameManagementMISEntities();

        // GET: Games
        public ActionResult Index(string searchTerm, int? genreFilter, bool? freeOnly, decimal? minPrice, decimal? maxPrice)
        {
            try
            {
                var games = db.Games.Include(g => g.Genre1).AsQueryable();
                
                // Search by game name/title
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    games = games.Where(g => g.Title.Contains(searchTerm));
                }
                
                // Filter by genre
                if (genreFilter.HasValue && genreFilter.Value > 0)
                {
                    games = games.Where(g => g.GenreID == genreFilter.Value);
                }

                // Price filters
                if (freeOnly == true)
                {
                    games = games.Where(g => g.Price == 0);
                }
                else
                {
                    if (minPrice.HasValue)
                    {
                        games = games.Where(g => g.Price >= minPrice.Value);
                    }
                    if (maxPrice.HasValue)
                    {
                        games = games.Where(g => g.Price <= maxPrice.Value);
                    }
                }
                
                // Store search and filter parameters for the view
                ViewBag.SearchTerm = searchTerm;
                ViewBag.GenreFilter = genreFilter ?? 0;
                ViewBag.MinPrice = minPrice;
                ViewBag.MaxPrice = maxPrice;
                ViewBag.FreeOnly = freeOnly == true;
                
                // Safely load genres
                try
                {
                    ViewBag.Genres = new SelectList(db.Genres.OrderBy(g => g.GenreName).ToList(), "GenreID", "GenreName");
                    ViewBag.GenresItems = db.Genres.OrderBy(g => g.GenreName).ToList();
                }
                catch
                {
                    ViewBag.Genres = new SelectList(new List<Genre>(), "GenreID", "GenreName");
                    ViewBag.GenresItems = new List<Genre>();
                }

                // Get current user's library status for all games
                var userId = (Session["UserID"] as int?) ?? 0;
                if (userId > 0)
                {
                    var userLibrary = db.UserLibraries
                        .Where(ul => ul.UserID == userId)
                        .Select(ul => new { ul.GameID, ul.PurchaseDate })
                        .ToList();
                    ViewBag.UserLibrary = userLibrary.ToDictionary(ul => ul.GameID, ul => ul.PurchaseDate != null ? "owned" : "wishlist");
                }
                else
                {
                    ViewBag.UserLibrary = new Dictionary<int, string>();
                }

                // Get active discounts for all games
                var now = DateTime.UtcNow;
                var activeDiscounts = db.Discounts
                    .Where(d => (!d.StartDate.HasValue || d.StartDate <= now) && (!d.EndDate.HasValue || d.EndDate >= now))
                    .Include(d => d.Games)
                    .ToList();
                
                var gameDiscounts = new Dictionary<int, decimal>();
                foreach (var disc in activeDiscounts)
                {
                    if (disc.Games != null && disc.DiscountPercent.HasValue)
                    {
                        foreach (var game in disc.Games)
                        {
                            if (!gameDiscounts.ContainsKey(game.GameID) || gameDiscounts[game.GameID] < disc.DiscountPercent.Value)
                            {
                                gameDiscounts[game.GameID] = disc.DiscountPercent.Value;
                            }
                        }
                    }
                }
                ViewBag.GameDiscounts = gameDiscounts;

                // Get featured games for hero carousel (top 5 criteria):
                // 1. Games with active discounts (priority)
                // 2. Newest releases
                // 3. Highest rated
                var featuredGames = new List<Game>();
                
                // Priority 1: Games with biggest discounts
                var discountedGameIds = gameDiscounts.OrderByDescending(kv => kv.Value).Take(3).Select(kv => kv.Key).ToList();
                featuredGames.AddRange(db.Games.Include(g => g.Genre1).Where(g => discountedGameIds.Contains(g.GameID)).ToList());
                
                // Priority 2: Recent releases
                if (featuredGames.Count < 5)
                {
                    var recentGames = db.Games.Include(g => g.Genre1)
                        .Where(g => g.ReleaseDate.HasValue && !discountedGameIds.Contains(g.GameID))
                        .OrderByDescending(g => g.ReleaseDate)
                        .Take(5 - featuredGames.Count)
                        .ToList();
                    featuredGames.AddRange(recentGames);
                }
                
                // Priority 3: Fill remaining with highest rated
                if (featuredGames.Count < 5)
                {
                    var excludeIds = featuredGames.Select(g => g.GameID).ToList();
                    var topRated = db.Games.Include(g => g.Genre1)
                        .Where(g => !excludeIds.Contains(g.GameID))
                        .OrderByDescending(g => g.Rating)
                        .Take(5 - featuredGames.Count)
                        .ToList();
                    featuredGames.AddRange(topRated);
                }
                
                ViewBag.FeaturedGames = featuredGames.Take(5).ToList();

                // Get top sellers (most purchased games)
                var topSellers = db.Transactions
                    .GroupBy(t => t.GameID)
                    .Select(g => new { GameID = g.Key, Count = g.Count() })
                    .OrderByDescending(g => g.Count)
                    .Take(10)
                    .Select(g => g.GameID)
                    .ToList();
                ViewBag.TopSellerIds = topSellers;

                // Get new releases (last 30 days)
                var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
                var newReleaseIds = db.Games
                    .Where(g => g.ReleaseDate.HasValue && g.ReleaseDate >= thirtyDaysAgo)
                    .Select(g => g.GameID)
                    .ToList();
                ViewBag.NewReleaseIds = newReleaseIds;
                
                return View(games.ToList());
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Unable to load games. Please check your database connection.";
                ViewBag.Genres = new SelectList(new List<Genre>(), "GenreID", "GenreName");
                ViewBag.GenresItems = new List<Genre>();
                ViewBag.UserLibrary = new Dictionary<int, string>();
                ViewBag.GameDiscounts = new Dictionary<int, decimal>();
                ViewBag.FeaturedGames = new List<Game>();
                ViewBag.TopSellerIds = new List<int>();
                ViewBag.NewReleaseIds = new List<int>();
                
                // Log error for debugging
                System.Diagnostics.Debug.WriteLine($"Error loading games: {ex.Message}");
                
                return View(new List<Game>());
            }
        }

        // GET: Games/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Game game = db.Games.Find(id);
            if (game == null)
            {
                return HttpNotFound();
            }

            var feedbacks = db.GameFeedbacks
                .Include(f => f.User)
                .Where(f => f.GameID == game.GameID)
                .OrderByDescending(f => f.FeedbackDate)
                .ToList();

            decimal average = 0m;
            if (feedbacks.Any(f => f.Rating.HasValue))
            {
                average = Math.Round((decimal)feedbacks.Where(f => f.Rating.HasValue).Average(f => f.Rating.Value), 1);
            }

            ViewBag.AverageRating = average;
            ViewBag.FeedbackCount = feedbacks.Count;
            ViewBag.Feedbacks = feedbacks;
            ViewBag.GenresItems = db.Genres.OrderBy(g => g.GenreName).ToList();

            // Get current user's library status for this game
            var userId = (Session["UserID"] as int?) ?? 0;
            string libraryStatus = "none";
            if (userId > 0)
            {
                var library = db.UserLibraries.FirstOrDefault(ul => ul.UserID == userId && ul.GameID == game.GameID);
                if (library != null)
                {
                    libraryStatus = library.PurchaseDate != null ? "owned" : "wishlist";
                }
            }
            ViewBag.LibraryStatus = libraryStatus;

            return View(game);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SubmitFeedback(int gameId, int rating, string review)
        {
            // Ensure user is authenticated
            var sessionUserId = (Session["UserID"] as int?) ?? 0;
            if (sessionUserId <= 0)
            {
                TempData["FeedbackError"] = "You must be signed in to submit feedback.";
                return RedirectToAction("Details", new { id = gameId });
            }

            // Basic validation
            if (rating < 1 || rating > 5)
            {
                TempData["FeedbackError"] = "Rating must be between 1 and 5.";
                return RedirectToAction("Details", new { id = gameId });
            }

            var game = db.Games.Find(gameId);
            if (game == null)
            {
                TempData["FeedbackError"] = "Invalid game.";
                return RedirectToAction("Details", new { id = gameId });
            }

            // Prevent duplicate feedback (optional): one per user per game
            var existing = db.GameFeedbacks.FirstOrDefault(f => f.GameID == gameId && f.UserID == sessionUserId);
            if (existing != null)
            {
                TempData["FeedbackError"] = "You have already submitted feedback for this game.";
                return RedirectToAction("Details", new { id = gameId });
            }

            var feedback = new GameFeedback
            {
                GameID = gameId,
                UserID = sessionUserId,
                Rating = rating,
                Review = string.IsNullOrWhiteSpace(review) ? null : review.Trim(),
                FeedbackDate = DateTime.UtcNow
            };

            db.GameFeedbacks.Add(feedback);
            db.SaveChanges();

            // Recalculate and persist average rating on Game
            var ratings = db.GameFeedbacks.Where(f => f.GameID == gameId && f.Rating.HasValue).Select(f => f.Rating.Value);
            if (ratings.Any())
            {
                var avg = (decimal)Math.Round(ratings.Average(), 1);
                game.Rating = avg;
                db.Entry(game).State = EntityState.Modified;
                db.SaveChanges();
            }

            TempData["FeedbackSuccess"] = "Thanks for your feedback!";
            return RedirectToAction("Details", new { id = gameId });
        }

        // GET: Games/Create
        [AdminAuthorize]
        public ActionResult Create()
        {
            ViewBag.GenreID = new SelectList(db.Genres, "GenreID", "GenreName");
            return View();
        }

        // POST: Games/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize]
        public ActionResult Create([Bind(Include = "GameID,Title,Genre,Description,Rating,Price,ReleaseDate,IsAvailable,GenreID")] Game game, HttpPostedFileBase coverImage)
        {
            if (ModelState.IsValid)
            {
                db.Games.Add(game);
                db.SaveChanges();

                // Handle cover image upload (force JPEG {GameID}.jpg)
                if (coverImage != null && coverImage.ContentLength > 0)
                {
                    try
                    {
                        var dir = Server.MapPath("~/Content/covers/");
                        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                        var jpgPath = Path.Combine(dir, game.GameID + ".jpg");
                        coverImage.InputStream.Position = 0;
                        using (var img = Image.FromStream(coverImage.InputStream))
                        {
                            img.Save(jpgPath, ImageFormat.Jpeg);
                        }
                    }
                    catch (Exception ex)
                    {
                        TempData["ToastError"] = "Game created but image upload failed: " + ex.Message;
                    }
                }

                TempData["ToastSuccess"] = "Game created successfully!";
                return RedirectToAction("Index");
            }

            ViewBag.GenreID = new SelectList(db.Genres, "GenreID", "GenreName", game.GenreID);
            return View(game);
        }

        // GET: Games/Edit/5
        [AdminAuthorize]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Game game = db.Games.Find(id);
            if (game == null)
            {
                return HttpNotFound();
            }
            ViewBag.GenreID = new SelectList(db.Genres, "GenreID", "GenreName", game.GenreID);
            return View(game);
        }

        // POST: Games/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize]
        public ActionResult Edit([Bind(Include = "GameID,Title,Genre,Description,Rating,Price,ReleaseDate,IsAvailable,GenreID")] Game game, HttpPostedFileBase coverImage)
        {
            if (ModelState.IsValid)
            {
                db.Entry(game).State = EntityState.Modified;
                db.SaveChanges();

                // Handle cover image upload (force JPEG {GameID}.jpg)
                if (coverImage != null && coverImage.ContentLength > 0)
                {
                    try
                    {
                        var dir = Server.MapPath("~/Content/covers/");
                        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                        var jpgPath = Path.Combine(dir, game.GameID + ".jpg");
                        if (System.IO.File.Exists(jpgPath)) System.IO.File.Delete(jpgPath);
                        coverImage.InputStream.Position = 0;
                        using (var img = Image.FromStream(coverImage.InputStream))
                        {
                            img.Save(jpgPath, ImageFormat.Jpeg);
                        }
                        TempData["ToastSuccess"] = "Game updated with new cover image!";
                    }
                    catch (Exception ex)
                    {
                        TempData["ToastError"] = "Game updated but image upload failed: " + ex.Message;
                    }
                }
                else
                {
                    TempData["ToastSuccess"] = "Game updated successfully!";
                }

                return RedirectToAction("Index");
            }
            ViewBag.GenreID = new SelectList(db.Genres, "GenreID", "GenreName", game.GenreID);
            return View(game);
        }

        // GET: Games/Delete/5
        [AdminAuthorize]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Game game = db.Games.Find(id);
            if (game == null)
            {
                return HttpNotFound();
            }
            return View(game);
        }

        // POST: Games/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [AdminAuthorize]
        public ActionResult DeleteConfirmed(int id)
        {
            Game game = db.Games.Find(id);
            db.Games.Remove(game);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // POST: Games/AddToLibrary
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddToLibrary(int gameId, int? userId)
        {
            // Determine user
            int uid = userId ?? (Session["UserID"] as int?) ?? 0;
            if (uid <= 0)
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = "Please sign in to add games to your library." });
                }
                TempData["ToastError"] = "Please sign in to add games to your library.";
                return RedirectToAction("Details", new { id = gameId });
            }

            var game = db.Games.Find(gameId);
            if (game == null)
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = "Game not found." });
                }
                TempData["ToastError"] = "Game not found.";
                return RedirectToAction("Index");
            }

            // Check if user already has this in library (any state)
            var existing = db.UserLibraries.FirstOrDefault(ul => ul.UserID == uid && ul.GameID == gameId);
            if (existing != null)
            {
                string msg = existing.PurchaseDate != null 
                    ? $"You already own '{game.Title}'." 
                    : $"'{game.Title}' is already in your wishlist.";
                
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = msg });
                }
                TempData["ToastInfo"] = msg;
                return RedirectToAction("Index");
            }

            // Create a wishlist entry (no purchase date or activation code)
            var library = new UserLibrary
            {
                UserID = uid,
                GameID = gameId,
                PurchaseDate = null,
                ActivationCode = null
            };
            db.UserLibraries.Add(library);
            db.SaveChanges();

            string successMsg = $"Added '{game.Title}' to your wishlist! Visit your Library to purchase.";
            if (Request.IsAjaxRequest())
            {
                return Json(new { success = true, message = successMsg, status = "wishlist" });
            }
            
            TempData["ToastSuccess"] = successMsg;
            return RedirectToAction("Index");
        }

        // GET: Games/SearchGames (AJAX endpoint for live search)
        [HttpGet]
        public JsonResult SearchGames(string query)
        {
            // Accept either 'query' or 'searchTerm' (see all results uses searchTerm)
            string raw = query ?? Request.QueryString["searchTerm"] ?? string.Empty;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return Json(new { success = false, results = new List<object>() }, JsonRequestBehavior.AllowGet);
            }

            var q = raw.Trim();

            try
            {
                // Load a reasonable pool into memory and perform case-insensitive search to avoid EF translation pitfalls
                var pool = db.Games.Include(g => g.Genre1).ToList();

                var matched = pool
                    .Where(g => (!string.IsNullOrEmpty(g.Title) && g.Title.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                             || (!string.IsNullOrEmpty(g.Description) && g.Description.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                             || (g.Genre1 != null && !string.IsNullOrEmpty(g.Genre1.GenreName) && g.Genre1.GenreName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0))
                    .OrderByDescending(g => g.Rating ?? 0)
                    .ThenBy(g => g.Title)
                    .Take(5)
                    .Select(g => new
                    {
                        gameId = g.GameID,
                        title = g.Title,
                        price = g.Price,
                        genre = g.Genre1 != null ? g.Genre1.GenreName : "Uncategorized",
                        rating = g.Rating ?? 0,
                        releaseDate = g.ReleaseDate,
                        coverUrl = "/Content/covers/" + g.GameID + ".jpg",
                        detailsUrl = Url.Action("Details", "Games", new { id = g.GameID })
                    })
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"SearchGames called. raw=\"{raw}\" normalized=\"{q}\" results={matched.Count}");

                return Json(new { success = true, results = matched, count = matched.Count }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Search error: {ex.Message}");
                return Json(new { success = false, results = new List<object>(), error = "Search failed" }, JsonRequestBehavior.AllowGet);
            }
        }

        // Helper method to check library status for a user and game
        private string GetLibraryStatus(int userId, int gameId)
        {
            var library = db.UserLibraries.FirstOrDefault(ul => ul.UserID == userId && ul.GameID == gameId);
            if (library == null) return "none"; // Not in library
            if (library.PurchaseDate != null) return "owned"; // Purchased
            return "wishlist"; // In wishlist but not purchased
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
