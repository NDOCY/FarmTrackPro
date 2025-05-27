using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
//using farm.Models;
using FarmTrack.Models;

namespace FarmTrack.Controllers
{
    public class ActivityLogsController : Controller
    {
        private FarmTrackContext db = new FarmTrackContext();

        // GET: ActivityLogs
        public ActionResult Index(string search)
        {
            var activityLogs = db.ActivityLogs.Include(a => a.User).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                activityLogs = activityLogs.Where(a =>
                    a.User.FullName.Contains(search) ||
                    a.Description.Contains(search)
                );
            }

            ViewBag.SearchQuery = search;
            return View(activityLogs.ToList());
        }

        // GET: ActivityLogs/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            ActivityLog activityLog = db.ActivityLogs.Find(id);
            if (activityLog == null)
                return HttpNotFound();

            return View(activityLog);
        }

        // GET: ActivityLogs/Create
        public ActionResult Create()
        {
            ViewBag.UserId = new SelectList(db.Users, "UserId", "FullName");
            return View();
        }

        // POST: ActivityLogs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "LogId,UserId,Description,Timestamp")] ActivityLog activityLog)
        {
            if (ModelState.IsValid)
            {
                db.ActivityLogs.Add(activityLog);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.UserId = new SelectList(db.Users, "UserId", "FullName", activityLog.UserId);
            return View(activityLog);
        }

        // GET: ActivityLogs/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            ActivityLog activityLog = db.ActivityLogs.Find(id);
            if (activityLog == null)
                return HttpNotFound();

            ViewBag.UserId = new SelectList(db.Users, "UserId", "FullName", activityLog.UserId);
            return View(activityLog);
        }

        // POST: ActivityLogs/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "LogId,UserId,Description,Timestamp")] ActivityLog activityLog)
        {
            if (ModelState.IsValid)
            {
                db.Entry(activityLog).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.UserId = new SelectList(db.Users, "UserId", "FullName", activityLog.UserId);
            return View(activityLog);
        }

        // GET: ActivityLogs/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            ActivityLog activityLog = db.ActivityLogs.Find(id);
            if (activityLog == null)
                return HttpNotFound();

            return View(activityLog);
        }

        // POST: ActivityLogs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            ActivityLog activityLog = db.ActivityLogs.Find(id);
            db.ActivityLogs.Remove(activityLog);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}
