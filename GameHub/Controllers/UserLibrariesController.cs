using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using GameHub.Models;

namespace GameHub.Controllers
{
    public class UserLibrariesController : Controller
    {
        private GameManagementMISEntities db = new GameManagementMISEntities();

        // GET: UserLibraries
        public ActionResult Index()
        {
            // Get current user ID from session
            var userId = (Session["UserID"] as int?) ?? 0;
            
            if (userId == 0)
            {
                // Not logged in - redirect to login
                return RedirectToAction("Login", "Account");
            }

            // Only get libraries for current user
            var userLibraries = db.UserLibraries
                .Include(u => u.Game)
                .Include(u => u.Game.Genre1)
                .Include(u => u.User)
                .Where(u => u.UserID == userId)
                .OrderByDescending(u => u.PurchaseDate)
                .ToList();

            return View(userLibraries);
        }

        // GET: UserLibraries/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            UserLibrary userLibrary = db.UserLibraries.Find(id);
            if (userLibrary == null)
            {
                return HttpNotFound();
            }
            return View(userLibrary);
        }

        // GET: UserLibraries/Create
        public ActionResult Create()
        {
            ViewBag.GameID = new SelectList(db.Games, "GameID", "Title");
            ViewBag.UserID = new SelectList(db.Users, "UserID", "FullName");
            return View();
        }

        // POST: UserLibraries/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "LibraryID,UserID,GameID,PurchaseDate,ActivationCode")] UserLibrary userLibrary)
        {
            if (ModelState.IsValid)
            {
                db.UserLibraries.Add(userLibrary);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.GameID = new SelectList(db.Games, "GameID", "Title", userLibrary.GameID);
            ViewBag.UserID = new SelectList(db.Users, "UserID", "FullName", userLibrary.UserID);
            return View(userLibrary);
        }

        // GET: UserLibraries/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            UserLibrary userLibrary = db.UserLibraries.Find(id);
            if (userLibrary == null)
            {
                return HttpNotFound();
            }
            ViewBag.GameID = new SelectList(db.Games, "GameID", "Title", userLibrary.GameID);
            ViewBag.UserID = new SelectList(db.Users, "UserID", "FullName", userLibrary.UserID);
            return View(userLibrary);
        }

        // POST: UserLibraries/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "LibraryID,UserID,GameID,PurchaseDate,ActivationCode")] UserLibrary userLibrary)
        {
            if (ModelState.IsValid)
            {
                db.Entry(userLibrary).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.GameID = new SelectList(db.Games, "GameID", "Title", userLibrary.GameID);
            ViewBag.UserID = new SelectList(db.Users, "UserID", "FullName", userLibrary.UserID);
            return View(userLibrary);
        }

        // GET: UserLibraries/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            UserLibrary userLibrary = db.UserLibraries.Find(id);
            if (userLibrary == null)
            {
                return HttpNotFound();
            }
            return View(userLibrary);
        }

        // POST: UserLibraries/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            UserLibrary userLibrary = db.UserLibraries.Find(id);
            db.UserLibraries.Remove(userLibrary);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // POST: UserLibraries/RemoveFromWishlist
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveFromWishlist(int libraryId)
        {
            var userId = (Session["UserID"] as int?) ?? 0;
            if (userId == 0)
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = "Not authenticated" });
                }
                return RedirectToAction("Login", "Account");
            }

            var library = db.UserLibraries.Find(libraryId);
            if (library == null || library.UserID != userId)
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = "Item not found or access denied" });
                }
                TempData["ToastError"] = "Item not found";
                return RedirectToAction("Index");
            }

            // Only allow removal if not purchased
            if (library.PurchaseDate != null)
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = "Cannot remove purchased games" });
                }
                TempData["ToastError"] = "Cannot remove purchased games";
                return RedirectToAction("Index");
            }

            db.UserLibraries.Remove(library);
            db.SaveChanges();

            if (Request.IsAjaxRequest())
            {
                return Json(new { success = true, message = "Removed from wishlist" });
            }

            TempData["ToastSuccess"] = "Removed from wishlist";
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
