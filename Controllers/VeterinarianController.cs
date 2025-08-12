using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using FarmTrack.Models;

public class VeterinarianController : Controller
{
    private FarmTrackContext db = new FarmTrackContext();

    // GET: Veterinarian
    public ActionResult Index()
    {
        var veterinarians = db.Veterinarians.OrderBy(v => v.FullName).ToList();


        return View(veterinarians);
    }

    // GET: Veterinarian/Details/5
    public ActionResult Details(int id)
    {
        var veterinarian = db.Veterinarians.Find(id);
        if (veterinarian == null)
            return HttpNotFound();

        // Get appointment history
        var appointments = db.HealthCheckSchedules
            .Include(h => h.AssignedToUser)
            .Where(h => h.VeterinarianId == id)
            .OrderByDescending(h => h.ScheduledDate)
            .Take(10)
            .ToList();

        // Get statistics
        var stats = new VeterinarianStatsViewModel
        {
            TotalAppointments = db.HealthCheckSchedules.Count(h => h.VeterinarianId == id),
            CompletedAppointments = db.HealthCheckSchedules.Count(h => h.VeterinarianId == id && h.Status == "Completed"),
            PendingAppointments = db.HealthCheckSchedules.Count(h => h.VeterinarianId == id && h.Status == "Pending"),
            TotalCost = db.HealthCheckSchedules
                .Where(h => h.VeterinarianId == id && h.ActualCost.HasValue)
                .Sum(h => h.ActualCost) ?? 0,
            AverageRating = 4.5m, // This would come from a rating system if implemented
            LastAppointment = db.HealthCheckSchedules
                .Where(h => h.VeterinarianId == id && h.Status == "Completed")
                .OrderByDescending(h => h.ScheduledDate)
                .Select(h => h.ScheduledDate)
                .FirstOrDefault()
        };

        ViewBag.RecentAppointments = appointments;
        ViewBag.Stats = stats;

        return View(veterinarian);
    }

    // GET: Veterinarian/Create
    public ActionResult Create()
    {
        ViewBag.Specializations = new List<string>
        {
            "General Practice",
            "Large Animal",
            "Small Animal",
            "Equine",
            "Bovine",
            "Swine",
            "Poultry",
            "Emergency Care",
            "Surgery",
            "Reproduction",
            "Internal Medicine",
            "Dermatology",
            "Orthopedics"
        };

        return View();
    }

    // POST: Veterinarian/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Create(Veterinarian veterinarian)
    {
        if (ModelState.IsValid)
        {
            db.Veterinarians.Add(veterinarian);
            db.SaveChanges();

            TempData["Message"] = "Veterinarian added successfully.";
            return RedirectToAction("Index");
        }

        ViewBag.Specializations = new List<string>
        {
            "General Practice",
            "Large Animal",
            "Small Animal",
            "Equine",
            "Bovine",
            "Swine",
            "Poultry",
            "Emergency Care",
            "Surgery",
            "Reproduction",
            "Internal Medicine",
            "Dermatology",
            "Orthopedics"
        };

        return View(veterinarian);
    }

    // GET: Veterinarian/Edit/5
    public ActionResult Edit(int id)
    {
        var veterinarian = db.Veterinarians.Find(id);
        if (veterinarian == null)
            return HttpNotFound();

        ViewBag.Specializations = new List<string>
        {
            "General Practice",
            "Large Animal",
            "Small Animal",
            "Equine",
            "Bovine",
            "Swine",
            "Poultry",
            "Emergency Care",
            "Surgery",
            "Reproduction",
            "Internal Medicine",
            "Dermatology",
            "Orthopedics"
        };

        return View(veterinarian);
    }

    // POST: Veterinarian/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Edit(Veterinarian veterinarian)
    {
        if (ModelState.IsValid)
        {
            db.Entry(veterinarian).State = EntityState.Modified;
            db.SaveChanges();

            TempData["Message"] = "Veterinarian updated successfully.";
            return RedirectToAction("Index");
        }

        ViewBag.Specializations = new List<string>
        {
            "General Practice",
            "Large Animal",
            "Small Animal",
            "Equine",
            "Bovine",
            "Swine",
            "Poultry",
            "Emergency Care",
            "Surgery",
            "Reproduction",
            "Internal Medicine",
            "Dermatology",
            "Orthopedics"
        };

        return View(veterinarian);
    }

    // POST: Veterinarian/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Delete(int id)
    {
        var veterinarian = db.Veterinarians.Find(id);
        if (veterinarian == null)
            return HttpNotFound();

        // Check if veterinarian has any appointments
        var hasAppointments = db.HealthCheckSchedules.Any(h => h.VeterinarianId == id);

        if (hasAppointments)
        {
            // Don't delete, just deactivate
            veterinarian.IsActive = false;
            db.Entry(veterinarian).State = EntityState.Modified;
            db.SaveChanges();

            TempData["Warning"] = "Veterinarian has existing appointments and has been deactivated instead of deleted.";
        }
        else
        {
            db.Veterinarians.Remove(veterinarian);
            db.SaveChanges();

            TempData["Message"] = "Veterinarian deleted successfully.";
        }

        return RedirectToAction("Index");
    }

    // GET: Veterinarian/Schedule/5
    public ActionResult Schedule(int id, DateTime? date = null)
    {
        var veterinarian = db.Veterinarians.Find(id);
        if (veterinarian == null)
            return HttpNotFound();

        var selectedDate = date ?? DateTime.Today;
        var startDate = selectedDate.Date;
        var endDate = startDate.AddDays(1);

        var appointments = db.HealthCheckSchedules
            .Include(h => h.AssignedToUser)
            .Where(h => h.VeterinarianId == id &&
                       h.ScheduledDate >= startDate &&
                       h.ScheduledDate < endDate)
            .OrderBy(h => h.ScheduledDate)
            .ToList();

        // Get livestock count for each appointment
        var appointmentIds = appointments.Select(a => a.Id).ToList();
        var livestockCounts = db.HealthCheckLivestocks
            .Where(l => appointmentIds.Contains(l.HealthCheckScheduleId))
            .GroupBy(l => l.HealthCheckScheduleId)
            .ToDictionary(g => g.Key, g => g.Count());

        ViewBag.Veterinarian = veterinarian;
        ViewBag.SelectedDate = selectedDate;
        ViewBag.LivestockCounts = livestockCounts;

        return View(appointments);
    }

    // AJAX: Get veterinarian availability
    public JsonResult GetAvailability(int id, DateTime date)
    {
        var appointments = db.HealthCheckSchedules
            .Where(h => h.VeterinarianId == id &&
                       DbFunctions.TruncateTime(h.ScheduledDate) == date.Date &&
                       h.Status == "Pending")
            .Select(h => new {
                time = h.ScheduledDate.TimeOfDay,
                purpose = h.Purpose,
                checkType = h.CheckType
            })
            .ToList();

        return Json(new
        {
            isAvailable = !appointments.Any(),
            appointments = appointments
        }, JsonRequestBehavior.AllowGet);
    }

    // Performance and cost analysis
    public ActionResult Analytics()
    {
        var veterinarians = db.Veterinarians
            .Where(v => v.IsActive)
            .Select(v => new VeterinarianAnalyticsViewModel
            {
                Id = v.Id,
                Name = v.FullName,
                Specialization = v.Specialization,
                TotalAppointments = db.HealthCheckSchedules.Count(h => h.VeterinarianId == v.Id),
                CompletedAppointments = db.HealthCheckSchedules.Count(h => h.VeterinarianId == v.Id && h.Status == "Completed"),
                TotalRevenue = db.HealthCheckSchedules
                    .Where(h => h.VeterinarianId == v.Id && h.ActualCost.HasValue)
                    .Sum(h => h.ActualCost) ?? 0,
                AverageCost = db.HealthCheckSchedules
                    .Where(h => h.VeterinarianId == v.Id && h.ActualCost.HasValue)
                    .Average(h => h.ActualCost) ?? 0,
                LastAppointment = db.HealthCheckSchedules
                    .Where(h => h.VeterinarianId == v.Id)
                    .OrderByDescending(h => h.ScheduledDate)
                    .Select(h => h.ScheduledDate)
                    .FirstOrDefault()
            })
            .OrderByDescending(v => v.TotalAppointments)
            .ToList();

        // Monthly statistics
        var monthlyStats = db.HealthCheckSchedules
            .Where(h => h.IsOutsourced && h.ScheduledDate >= DbFunctions.AddMonths(DateTime.Now, -12))
            .GroupBy(h => new {
                Year = h.ScheduledDate.Year,
                Month = h.ScheduledDate.Month
            })
            .Select(g => new MonthlyStatsViewModel
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                AppointmentCount = g.Count(),
                TotalCost = g.Where(h => h.ActualCost.HasValue).Sum(h => h.ActualCost) ?? 0
            })
            .OrderBy(s => s.Year)
            .ThenBy(s => s.Month)
            .ToList();

        ViewBag.MonthlyStats = monthlyStats;

        return View(veterinarians);
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
