using FarmTrack.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using QRCoder;
using FarmTrack.Services;
using System.Configuration;
using System.Threading.Tasks;
using System.Drawing;

namespace FarmTrack.Controllers
{
    public class LivestockController : Controller
    {
        private FarmTrackContext db = new FarmTrackContext();
        public ActionResult Index(string search)
        {
            // Fetch all livestock initially
            var livestockList = db.Livestocks.AsQueryable();

            // If a search query is provided, filter based on it
            if (!string.IsNullOrEmpty(search))
            {
                livestockList = livestockList.Where(l =>
                    l.Type.Contains(search) ||
                    l.Breed.Contains(search) ||
                    l.TagNumber.Contains(search) ||
                    l.Status.Contains(search)
                );
            }

            // Pass the search term back to the view for display in the input field
            ViewBag.SearchQuery = search;

            return View(livestockList.ToList());
        }

        // GET: Add Livestock
        public ActionResult Create()
        {
            string currentUserRole = Session["Role"]?.ToString();

            if (currentUserRole != "Owner")
            {
                return RedirectToAction("AdminDashboard", "Dashboard"); // Redirect if not authorized
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(Livestock livestock, string InitialEventType, DateTime? InitialEventDate)
        {

            if (!ModelState.IsValid) return View(livestock);

            livestock.UserId = Convert.ToInt32(Session["UserId"]);
            livestock.DateRegistered = DateTime.Now;

            db.Livestocks.Add(livestock);
            db.SaveChanges();

            // QR Code stuff
            string tag = Url.Action("Details", "Livestock", new { id = livestock.LivestockId }, Request.Url.Scheme);
            var qrBitmap = GenerateQrBitmap(tag);

            var blobService = new BlobService(ConfigurationManager.AppSettings["AzureBlobConnection"]);
            string fileName = $"qr-{tag}.png";
            string qrUrl = await blobService.UploadQrCodeAsync(qrBitmap, fileName);

            livestock.QrCodePath = qrUrl;
            db.Entry(livestock).State = EntityState.Modified;
            db.SaveChanges();

            // Add initial weight record
            if (livestock.InitialWeight.HasValue)
            {
                var weightRecord = new WeightRecord
                {
                    LivestockId = livestock.LivestockId,
                    Weight = livestock.InitialWeight.Value,
                    RecordedAt = DateTime.Now,
                    Notes = "Initial weight recorded"
                };
                db.WeightRecords.Add(weightRecord);
            }

            // Add initial health record with weight
            var record = new HealthRecord
            {
                LivestockId = livestock.LivestockId,
                EventType = InitialEventType,
                Date = InitialEventDate ?? DateTime.Now,
                Notes = $"Initial event: {InitialEventType}",
                RecordedBy = Session["FullName"]?.ToString(),
                Weight = livestock.InitialWeight ?? 0

            };
            db.HealthRecords.Add(record);

            // ✅ Update livestock.Weight
            var livestocks = db.Livestocks.Find(livestock.LivestockId);
            if (livestocks != null)
            {
                livestocks.Weight = record.Weight.Value;
                db.Entry(livestocks).State = EntityState.Modified;
            }

            db.SaveChanges();
            TempData["Message"] = "Livestock added with initial weight, health record, and QR code.";
            return RedirectToAction("Index");
        }


        public Bitmap GenerateQrBitmap(string tag)
        {
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(tag, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new QRCode(qrCodeData);
            return qrCode.GetGraphic(20); // 20 is pixel density
        }


        // GET: Edit Livestock
        public ActionResult Edit(int id)
        {
            var livestock = db.Livestocks.Find(id);
            if (livestock == null)
                return HttpNotFound();

            return View(livestock);
        }

        // POST: Edit Livestock
        // POST: Edit Livestock
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Livestock model)
        {
            // Get the existing livestock from database
            var existingLivestock = db.Livestocks.Find(model.LivestockId);
            if (existingLivestock == null)
            {
                return HttpNotFound();
            }

            // Update only the fields that should be editable
            existingLivestock.Weight = model.Weight;
            existingLivestock.IsBreedingStock = model.IsBreedingStock;

            if (existingLivestock.IsBreedingStock)
            {
                // Run the eligibility check using the existing livestock with updated values
                if (!Livestock.LivestockBreedingHelper.MeetsBreedingRequirements(existingLivestock))
                {
                    var message = Livestock.LivestockBreedingHelper.GetEligibilityMessage(existingLivestock);
                    ModelState.AddModelError("IsBreedingStock", $"Cannot mark as breeding stock. {message}");

                    // Return the original model for the view, but with the validation error
                    return View(model);
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            db.Entry(existingLivestock).State = EntityState.Modified;
            db.SaveChanges();
            TempData["Message"] = "Livestock updated successfully.";
            return RedirectToAction("Index");
        }

        // GET: Livestock/Details/5
        public ActionResult Details(int id)
        {
            string currentUserRole = Session["Role"]?.ToString();
            if (currentUserRole == "User")
            {
                return RedirectToAction("UserDashboard", "Dashboard");
            }
            var livestock = db.Livestocks.Find(id);
            if (livestock == null)
                return HttpNotFound();

            return View(livestock);
        }


        // GET: Delete Livestock
        public ActionResult Delete(int id)
        {
            string currentUserRole = Session["Role"]?.ToString();

            if (currentUserRole != "Owner")
            {
                return RedirectToAction("AdminDashboard", "Dashboard"); // Redirect if not authorized
            }

            var livestock = db.Livestocks.Find(id);
            if (livestock == null)
                return HttpNotFound();

            // Fetch latest health record (most recent by date)
            var latestEvent = db.HealthRecords
                .Where(hr => hr.LivestockId == id)
                .OrderByDescending(hr => hr.Date)
                .FirstOrDefault();

            ViewBag.LatestEventType = latestEvent?.EventType ?? "No record";


            return View(livestock);
        }

        // POST: Confirm Delete Livestock
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var livestock = db.Livestocks.Find(id);
            if (livestock != null)
            {
                string livestockName = livestock.Type; // Store name before deletion
                db.Livestocks.Remove(livestock);
                db.SaveChanges();

                // Log activity
                int userId = Convert.ToInt32(Session["UserId"]);
                db.LogActivity(userId, $"Deleted livestock: {livestockName} (ID: {id})");
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        public ActionResult Add(int livestockId)
        {
            ViewBag.EventTypes = new List<string> { "Birth", "Vaccination", "Checkup", "Death", "Sold", "Purchased" };
            return View(new HealthRecord { LivestockId = livestockId, Date = DateTime.Now });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Add(HealthRecord record)
        {
            if (ModelState.IsValid)
            {
                record.RecordedBy = Session["FullName"]?.ToString() ?? "Unknown";
                db.HealthRecords.Add(record);

                // Store weight separately in weight record if available
                if (record.Weight.HasValue)
                {
                    var weightRecord = new WeightRecord
                    {
                        LivestockId = record.LivestockId,
                        Weight = record.Weight.Value,
                        
                        RecordedAt = record.Date,
                        Notes = $"Weight logged during {record.EventType}"
                    };
                    db.WeightRecords.Add(weightRecord);

                    // ✅ Update the Livestock.Weight
                    var livestock = db.Livestocks.Find(record.LivestockId);
                    if (livestock != null)
                    {
                        livestock.Weight = record.Weight.Value;
                        db.Entry(livestock).State = EntityState.Modified;
                    }
                }

                db.SaveChanges();
                return RedirectToAction("Timeline", new { livestockId = record.LivestockId });
            }

            ViewBag.EventTypes = new List<string> { "Birth", "Vaccination", "Checkup", "Death", "Sold", "Purchased" };
            return View(record);
        }

        public ActionResult Timeline(int livestockId, string order = "desc")
        {
            var livestock = db.Livestocks.Find(livestockId);
            if (livestock == null)
            {
                return RedirectToAction("Index", "Livestock");
            }

            var records = db.HealthRecords
                .Where(r => r.LivestockId == livestockId)
                .OrderByDescending(r => r.Date)
                .ToList();

            if (order == "asc")
                records = records.OrderBy(r => r.Date).ToList();

            // Step 1: Pull from DB without formatting
            var rawWeights = db.WeightRecords
                .Where(w => w.LivestockId == livestockId)
                .OrderBy(w => w.RecordedAt)
                .ToList();

            // Step 2: Format in memory
            var weights = rawWeights.Select(w => new
            {
                Date = w.RecordedAt.ToString("yyyy-MM-dd"),
                w.Weight
            }).ToList();

            ViewBag.LivestockId = livestockId;
            ViewBag.LivestockName = livestock.TagNumber;
            ViewBag.Order = order;
            ViewBag.WeightChartData = weights;

            return View(records);
        }

        public ActionResult ScanQR()
        {
            return View();
        }




    }
}