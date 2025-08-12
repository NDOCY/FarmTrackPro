using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using FarmTrack.Helpers;
using Newtonsoft.Json.Linq;
using FarmTrack.Models;
using System.Threading.Tasks;
using FarmTrack.Services;

namespace FarmPro.Controllers
{
    public class CropsController : Controller
    {
        private FarmTrackContext db = new FarmTrackContext();

        // GET: Crops
        public ActionResult Index()
        {
            var crops = db.Crops.Include(c => c.CropRequirement);
            return View(crops.ToList());
        }

        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Crop crop = db.Crops.Include(c => c.CropRequirement).FirstOrDefault(c => c.Id == id);

            if (crop == null)
                return HttpNotFound();

            // ✅ Skip re-fetching if requirements already exist (and return view)
            if (crop.CropRequirement != null)
                return View(crop);

            // Otherwise, optionally try fetching from API
            // Or just show "No crop data available" message
            return View(crop);
        }


        // GET: Crops/Create
        public ActionResult Create()
        {
            ViewBag.Id = new SelectList(db.CropRequirement, "CropId", "ScientificName");
            return View();
        }

        // POST: Crops/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Name,Variety")] Crop crop)
        {
            if (ModelState.IsValid)
            {
                db.Crops.Add(crop);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.Id = new SelectList(db.CropRequirement, "CropId", "ScientificName", crop.Id);
            return View(crop);
        }

        // GET: Crops/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Crop crop = db.Crops.Find(id);
            if (crop == null)
            {
                return HttpNotFound();
            }
            ViewBag.Id = new SelectList(db.CropRequirement, "CropId", "ScientificName", crop.Id);
            return View(crop);
        }

        // POST: Crops/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Name,Variety")] Crop crop)
        {
            if (ModelState.IsValid)
            {
                db.Entry(crop).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.Id = new SelectList(db.CropRequirement, "CropId", "ScientificName", crop.Id);
            return View(crop);
        }

        // GET: Crops/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Crop crop = db.Crops.Find(id);
            if (crop == null)
            {
                return HttpNotFound();
            }
            return View(crop);
        }

        // POST: Crops/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Crop crop = db.Crops.Find(id);
            db.Crops.Remove(crop);
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

        public ActionResult FetchRequirements(int id)
        {
            var crop = db.Crops.Find(id);
            if (crop == null) return HttpNotFound();

            // Simulated API response (replace with real API call later)
            var requirements = new CropRequirements
            {
                CropId = crop.Id,
                ScientificName = "Sample scientific name for " + crop.Name,
                PreferredSoil = "Loamy",
                ExpectedYieldKgPerHectare = 100,
                PlantingSeason = "Summer",
                CommonPestsDiseases = "Full sun"
            };

            db.CropRequirement.Add(requirements);
            db.SaveChanges();

            return RedirectToAction("Details", new { id = crop.Id });
        }
        private readonly TrefleApiService _trefleApiService = new TrefleApiService();

        [HttpPost]
        public ActionResult FetchFromApi(int id)
        {
            var crop = db.Crops.Find(id);
            if (crop == null)
                return HttpNotFound();

            // ✅ Check if requirements already exist
            var existingRequirements = db.CropRequirement.FirstOrDefault(r => r.CropId == id);
            if (existingRequirements != null)
            {
                return Json(new { success = false, message = "Requirements already exist for this crop." }, JsonRequestBehavior.AllowGet);
            }

            // 🧪 Fetch mock data
            var mockData = MockCropRequirementService.GetRequirementsByCropName(crop.Name);
            if (mockData == null)
                return Json(new { success = false, message = "No data found." }, JsonRequestBehavior.AllowGet);

            // ✅ Assign crop ID and add
            mockData.CropId = crop.Id;
            db.CropRequirement.Add(mockData);
            db.SaveChanges();

            return Json(new { success = true }, JsonRequestBehavior.AllowGet);
        }


    }
}
