using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using FarmPro.Models;
using FarmTrack.Models;
using FarmTrack.Services;
using QRCoder;

public class ReproductionController : Controller
{
    private FarmTrackContext db = new FarmTrackContext();

    [HttpGet]
    public ActionResult Create()
    {
        /*ViewBag.Females = new SelectList(db.Livestocks.Where(l => l.Sex == "Female" && l.IsBreedingStock), "LivestockId", "TagNumber");
        ViewBag.Males = new SelectList(db.Livestocks.Where(l => l.Sex == "Male"), "LivestockId", "TagNumber");*/
        ViewBag.Females = new SelectList(
        db.Livestocks.Where(l => l.Sex == "Female" && l.IsBreedingStock && l.Status == "In-House"),
        "LivestockId", "TagNumber");

        ViewBag.Males = new SelectList(
            db.Livestocks.Where(l => l.Sex == "Male" && l.Status == "In-House"),
            "LivestockId", "TagNumber");

        // Pass gestation periods to the view
        ViewBag.GestationPeriods = Newtonsoft.Json.JsonConvert.SerializeObject(GestationPeriods);
        ViewBag.FemaleTypes = db.Livestocks
            .Where(l => l.Sex == "Female" && l.IsBreedingStock)
            .Select(l => new { l.LivestockId, l.Type })
            .ToList();

        return View();
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Create(ReproductionRecord model)
    {
        const int BreedingCooldownDays = 90;

        if (!ModelState.IsValid)
        {
            ViewBag.Females = new SelectList(
                db.Livestocks.Where(l => l.Sex == "Female" && l.IsBreedingStock && l.Status == "In-House"),
                "LivestockId", "TagNumber", model.FemaleLivestockId);
            ViewBag.Males = new SelectList(
                db.Livestocks.Where(l => l.Sex == "Male" && l.Status == "In-House"),
                "LivestockId", "TagNumber", model.MaleLivestockId);
            return View(model);
        }

        // Default breeding date to now if not provided
        if (model.BreedingDate == default(DateTime))
            model.BreedingDate = DateTime.Now;

        var female = db.Livestocks.Find(model.FemaleLivestockId);

        if (female == null || !female.IsBreedingStock || female.Status != "In-House")
        {
            ModelState.AddModelError("FemaleLivestockId", "Invalid, non-breeding, or unavailable female livestock selected.");
            ViewBag.Females = new SelectList(
                db.Livestocks.Where(l => l.Sex == "Female" && l.IsBreedingStock && l.Status == "In-House"),
                "LivestockId", "TagNumber", model.FemaleLivestockId);
            ViewBag.Males = new SelectList(
                db.Livestocks.Where(l => l.Sex == "Male" && l.Status == "In-House"),
                "LivestockId", "TagNumber", model.MaleLivestockId);
            return View(model);
        }

        // Find the most recent completed reproduction record (with actual birth date)
        var lastCompletedBirth = db.ReproductionRecords
            .Where(r => r.FemaleLivestockId == model.FemaleLivestockId && r.ActualBirthDate.HasValue)
            .OrderByDescending(r => r.ActualBirthDate.Value)
            .FirstOrDefault();

        if (lastCompletedBirth != null)
        {
            var daysSinceLastBirth = (model.BreedingDate - lastCompletedBirth.ActualBirthDate.Value).TotalDays;

            if (daysSinceLastBirth < BreedingCooldownDays)
            {
                ModelState.AddModelError("FemaleLivestockId",
                    $"Last birth was on {lastCompletedBirth.ActualBirthDate:yyyy-MM-dd}. Please wait at least {BreedingCooldownDays} days between giving birth and next breeding.");

                ViewBag.Females = new SelectList(
                    db.Livestocks.Where(l => l.Sex == "Female" && l.IsBreedingStock && l.Status == "In-House"),
                    "LivestockId", "TagNumber", model.FemaleLivestockId);
                ViewBag.Males = new SelectList(
                    db.Livestocks.Where(l => l.Sex == "Male" && l.Status == "In-House"),
                    "LivestockId", "TagNumber", model.MaleLivestockId);
                return View(model);
            }
        }

        // Update female's status to pregnant
        female.Status = "Pregnant";
        db.Entry(female).State = EntityState.Modified;

        // Set expected due date
        if (GestationPeriods.TryGetValue(female.Type, out int gestationDays))
        {
            model.ExpectedDueDate = model.BreedingDate.AddDays(gestationDays);
        }

        db.ReproductionRecords.Add(model);
        db.SaveChanges();

        // Log breeding in HealthRecords
        var maleTag = db.Livestocks.Find(model.MaleLivestockId)?.TagNumber ?? "Unknown";
        db.HealthRecords.Add(new HealthRecord
        {
            LivestockId = model.FemaleLivestockId,
            Date = model.BreedingDate,
            EventType = "Production",
            Notes = $"Breeding recorded with male: {maleTag}"
        });
        db.SaveChanges();

        TempData["Message"] = "Breeding event recorded successfully.";
        return RedirectToAction("Index");
    }



    public Bitmap GenerateQrBitmap(string tag)
    {
        var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(tag, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new QRCode(qrCodeData);
        return qrCode.GetGraphic(20); // 20 is pixel density
    }


    public ActionResult Details(int id)
    {
        var record = db.ReproductionRecords
                       .Include("FemaleLivestock")
                       .Include("FemaleLivestock.Offspring")
                       .Include("MaleLivestock")
                       .FirstOrDefault(r => r.Id == id);

        if (record == null) return HttpNotFound();

        return View(record);
    }

    public ActionResult Index()
    {
        var records = db.ReproductionRecords
            .Select(r => new ReproductionListViewModel
            {
                Id = r.Id,
                FemaleTag = db.Livestocks.Where(l => l.LivestockId == r.FemaleLivestockId).Select(l => l.TagNumber).FirstOrDefault(),
                MaleTag = db.Livestocks.Where(l => l.LivestockId == r.MaleLivestockId).Select(l => l.TagNumber).FirstOrDefault(),
                BreedingDate = r.BreedingDate,
                IsBirthRecorded = r.IsBirthRecorded,
                NumberOfOffspring = r.NumberOfOffspring,
                ExpectedBirthDate = r.ExpectedDueDate,
            })
            .ToList();

        return View(records);
    }

    private static readonly Dictionary<string, int> GestationPeriods = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        { "Cow", 283 },
        { "Goat", 150 },
        { "Sheep", 152 },
        { "Pig", 115 },
        { "Horse", 340 },
        { "Rabbit", 31 }
        // Add more types as needed
    };

    [HttpGet]
    public ActionResult RecordBirth(int id)
    {
        var record = db.ReproductionRecords
            .Include("FemaleLivestock")
            .Include("MaleLivestock")
            .FirstOrDefault(r => r.Id == id);

        if (record == null || record.IsBirthRecorded)
            return HttpNotFound();

        var vm = new ReproductionBirthViewModel
        {
            ReproductionRecordId = record.Id,
            FemaleTag = record.FemaleLivestock?.TagNumber,
            MaleTag = record.MaleLivestock?.TagNumber
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult RecordBirth(ReproductionBirthViewModel vm)
    {
        var record = db.ReproductionRecords.Find(vm.ReproductionRecordId);
        if (record == null || record.IsBirthRecorded)
            return HttpNotFound();

        if (vm.NumberOfOffspring != vm.OffspringTags.Count)
            ModelState.AddModelError("", "Please enter tag for each offspring.");

        if (ModelState.IsValid)
        {
            record.BirthOutcome = vm.BirthOutcome;
            record.NumberOfOffspring = vm.NumberOfOffspring;
            record.IsBirthRecorded = true;
            db.SaveChanges();

            for (int i = 0; i < vm.OffspringTags.Count; i++)
            {
                var offspring = new Livestock
                {
                    TagNumber = vm.OffspringTags[i],
                    Sex = vm.OffspringSexes[i],
                    DateRegistered = DateTime.Now,
                    ParentId = record.FemaleLivestockId,
                    ReproductionRecordId = record.Id
                };
                db.Livestocks.Add(offspring);
            }

            db.SaveChanges();
            TempData["Message"] = "Birth recorded and offspring added.";
            return RedirectToAction("Index");
        }

        vm.FemaleTag = record.FemaleLivestock?.TagNumber;
        vm.MaleTag = record.MaleLivestock?.TagNumber;
        return View(vm);
    }

    public ActionResult RecordBirthSimple(int id)
    {
        var record = db.ReproductionRecords
            .Include("FemaleLivestock")
            .Include("MaleLivestock")
            .FirstOrDefault(r => r.Id == id);

        if (record == null) return HttpNotFound();

        return View(record);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> RecordBirthSimple(int id, int numberOfOffspring, List<double> Weights)
    {
        var record = db.ReproductionRecords.Find(id);
        if (record == null) return HttpNotFound();

        if (Weights == null || Weights.Count != numberOfOffspring)
        {
            ModelState.AddModelError("", "Please provide weight for each offspring.");
            return View(record);
        }

        record.NumberOfOffspring = numberOfOffspring;
        record.IsBirthRecorded = true;
        record.ActualBirthDate = DateTime.Now;

        var female = db.Livestocks.Find(record.FemaleLivestockId);
        if (female != null)
        {
            female.Status = "In House";
            db.Entry(female).State = EntityState.Modified;
        }

        db.SaveChanges();

        // Log birth event for mother
        db.HealthRecords.Add(new HealthRecord
        {
            LivestockId = record.FemaleLivestockId,
            Date = DateTime.Now,
            EventType = "Production",
            Notes = $"Birth recorded: {record.NumberOfOffspring} offspring."
        });

        for (int i = 0; i < numberOfOffspring; i++)
        {
            string newTag = GenerateUniqueTag();

            var offspring = new Livestock
            {
                TagNumber = newTag,
                Sex = "Unknown",
                Type = female?.Type ?? "Unknown",
                Breed = female?.Breed ?? "Unknown",
                Status = "In House",
                DateRegistered = DateTime.Now,
                DateOfBirth = DateTime.Now,
                ReproductionRecordId = record.Id,
                ParentId = record.FemaleLivestockId,
                Weight = (double)Weights[i],
                UserId = Convert.ToInt32(Session["UserId"])
            };

            db.Livestocks.Add(offspring);
            db.SaveChanges();

            // QR code
            string detailsUrl = Url.Action("Details", "Livestock", new { id = offspring.LivestockId }, Request.Url.Scheme);
            var qrBitmap = GenerateQrBitmap(detailsUrl);
            var blobService = new BlobService(ConfigurationManager.AppSettings["AzureBlobConnection"]);
            string fileName = $"qr-{newTag}.png";
            string qrUrl = await blobService.UploadQrCodeAsync(qrBitmap, fileName);

            offspring.QrCodePath = qrUrl;
            db.Entry(offspring).State = EntityState.Modified;

            db.HealthRecords.Add(new HealthRecord
            {
                LivestockId = offspring.LivestockId,
                Date = DateTime.Now,
                EventType = "Birth",
                Notes = $"Born from {female?.TagNumber}.",
                Weight = (double)Weights[i],
                RecordedBy = Session["FullName"]?.ToString()
            });

            db.WeightRecords.Add(new WeightRecord
            {
                LivestockId = offspring.LivestockId,
                Weight = (double)Weights[i],
                RecordedAt = DateTime.Now,
                Notes = "Initial weight at birth"
            });

            db.SaveChanges();
        }

        TempData["Message"] = "Birth recorded and offspring added successfully.";
        return RedirectToAction("Index");
    }


    private string GenerateUniqueTag()
    {
        string tag;
        do
        {
            tag = "TAG" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper(); // e.g. TAGA1B2C3
        }
        while (db.Livestocks.Any(l => l.TagNumber == tag));
        return tag;
    }


    [HttpGet]
    public JsonResult GetGestationPeriod(int femaleLivestockId)
    {
        var female = db.Livestocks.FirstOrDefault(l => l.LivestockId == femaleLivestockId);
        if (female == null || string.IsNullOrEmpty(female.Type))
            return Json(new { success = false }, JsonRequestBehavior.AllowGet);

        if (GestationPeriods.TryGetValue(female.Type, out int days))
        {
            return Json(new { success = true, gestationDays = days }, JsonRequestBehavior.AllowGet);
        }

        return Json(new { success = false }, JsonRequestBehavior.AllowGet);
    }


}