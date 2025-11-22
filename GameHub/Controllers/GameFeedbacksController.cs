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
    public class GameFeedbacksController : Controller
    {
        private GameManagementMISEntities db = new GameManagementMISEntities();

        // GET: GameFeedbacks
        public ActionResult Index()
        {
            var gameFeedbacks = db.GameFeedbacks.Include(g => g.Game).Include(g => g.User);
            return View(gameFeedbacks.ToList());
        }

        // GET: GameFeedbacks/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            GameFeedback gameFeedback = db.GameFeedbacks.Find(id);
            if (gameFeedback == null)
            {
                return HttpNotFound();
            }
            return View(gameFeedback);
        }

        // GET: GameFeedbacks/Create
        public ActionResult Create()
        {
            ViewBag.GameID = new SelectList(db.Games, "GameID", "Title");
            ViewBag.UserID = new SelectList(db.Users, "UserID", "FullName");
            return View();
        }

        // POST: GameFeedbacks/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "FeedbackID,UserID,GameID,Rating,Review,FeedbackDate")] GameFeedback gameFeedback)
        {
            if (ModelState.IsValid)
            {
                db.GameFeedbacks.Add(gameFeedback);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.GameID = new SelectList(db.Games, "GameID", "Title", gameFeedback.GameID);
            ViewBag.UserID = new SelectList(db.Users, "UserID", "FullName", gameFeedback.UserID);
            return View(gameFeedback);
        }

        // GET: GameFeedbacks/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            GameFeedback gameFeedback = db.GameFeedbacks.Find(id);
            if (gameFeedback == null)
            {
                return HttpNotFound();
            }
            ViewBag.GameID = new SelectList(db.Games, "GameID", "Title", gameFeedback.GameID);
            ViewBag.UserID = new SelectList(db.Users, "UserID", "FullName", gameFeedback.UserID);
            return View(gameFeedback);
        }

        // POST: GameFeedbacks/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "FeedbackID,UserID,GameID,Rating,Review,FeedbackDate")] GameFeedback gameFeedback)
        {
            if (ModelState.IsValid)
            {
                db.Entry(gameFeedback).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.GameID = new SelectList(db.Games, "GameID", "Title", gameFeedback.GameID);
            ViewBag.UserID = new SelectList(db.Users, "UserID", "FullName", gameFeedback.UserID);
            return View(gameFeedback);
        }

        // GET: GameFeedbacks/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            GameFeedback gameFeedback = db.GameFeedbacks.Find(id);
            if (gameFeedback == null)
            {
                return HttpNotFound();
            }
            return View(gameFeedback);
        }

        // POST: GameFeedbacks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            GameFeedback gameFeedback = db.GameFeedbacks.Find(id);
            db.GameFeedbacks.Remove(gameFeedback);
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
}
