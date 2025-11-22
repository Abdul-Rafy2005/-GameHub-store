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
using GameHub.Services;

namespace GameHub.Controllers
{
    [AdminAuthorize]
    public class BackupLogsController : Controller
    {
        private GameManagementMISEntities db = new GameManagementMISEntities();

        // GET: BackupLogs
        public ActionResult Index()
        {
            ViewBag.BackupFiles = DatabaseMaintenanceService.ListBackupFiles();
            return View(db.BackupLogs.OrderByDescending(b => b.BackupDate).ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BackupNow()
        {
            try
            {
                var maint = new DatabaseMaintenanceService(db);
                var user = (Session["UserName"] as string) ?? (Session["UserType"] as string) ?? "Admin";
                var path = maint.BackupDatabase(user);
                TempData["BackupSuccess"] = $"Backup completed: {path}";
            }
            catch (Exception ex)
            {
                TempData["BackupError"] = "Backup failed: " + ex.Message;
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Restore(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                TempData["BackupError"] = "Please select a backup file to restore.";
                return RedirectToAction("Index");
            }
            try
            {
                var maint = new DatabaseMaintenanceService(db);
                maint.RestoreDatabase(filePath);
                TempData["BackupSuccess"] = "Restore completed successfully.";
            }
            catch (Exception ex)
            {
                TempData["BackupError"] = "Restore failed: " + ex.Message;
            }
            return RedirectToAction("Index");
        }

        // Simple scheduling placeholder using Windows Task Scheduler guidance
        public ActionResult Schedule()
        {
            return View();
        }

        // GET: BackupLogs/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            BackupLog backupLog = db.BackupLogs.Find(id);
            if (backupLog == null)
            {
                return HttpNotFound();
            }
            return View(backupLog);
        }

        // GET: BackupLogs/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: BackupLogs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "BackupID,BackupDate,FilePath,PerformedBy")] BackupLog backupLog)
        {
            if (ModelState.IsValid)
            {
                db.BackupLogs.Add(backupLog);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(backupLog);
        }

        // GET: BackupLogs/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            BackupLog backupLog = db.BackupLogs.Find(id);
            if (backupLog == null)
            {
                return HttpNotFound();
            }
            return View(backupLog);
        }

        // POST: BackupLogs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "BackupID,BackupDate,FilePath,PerformedBy")] BackupLog backupLog)
        {
            if (ModelState.IsValid)
            {
                db.Entry(backupLog).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(backupLog);
        }

        // GET: BackupLogs/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            BackupLog backupLog = db.BackupLogs.Find(id);
            if (backupLog == null)
            {
                return HttpNotFound();
            }
            return View(backupLog);
        }

        // POST: BackupLogs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            BackupLog backupLog = db.BackupLogs.Find(id);
            db.BackupLogs.Remove(backupLog);
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
