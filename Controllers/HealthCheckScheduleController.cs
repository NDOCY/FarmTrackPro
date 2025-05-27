using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web.Mvc;
using FarmTrack.Models;

public class HealthCheckScheduleController : Controller
{
    private FarmTrackContext db = new FarmTrackContext();

    [HttpGet]
    public ActionResult Create()
    {
        ViewBag.Users = new SelectList(db.Users.ToList(), "UserId", "FullName");
        ViewBag.Types = db.Livestocks.Select(l => l.Type).Distinct().ToList();
        ViewBag.Livestocks = db.Livestocks.ToList();

        ViewBag.HealthCheckTypes = new List<string>
    {
        "Vaccination",
        "General Checkup",
        "Deworming",
        "Pregnancy Check",
        "Weight Assessment",
        "Hoof Trimming"
    };

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Create(HealthCheckSchedule model, List<int> SelectedLivestockIds)
    {
        if (!ModelState.IsValid)
        {
            RebuildCreateViewBags(model.AssignedToUserId);
            return View(model);
        }

        if (SelectedLivestockIds == null || !SelectedLivestockIds.Any())
        {
            ModelState.AddModelError("", "Please select at least one animal.");
            RebuildCreateViewBags(model.AssignedToUserId);
            return View(model);
        }

        // 🔍 Check for conflicts
        var conflictLivestock = db.HealthCheckLivestocks
            .Include(hcl => hcl.HealthCheckSchedule)
            .Where(hcl => SelectedLivestockIds.Contains(hcl.LivestockId)
                          && hcl.HealthCheckSchedule.CheckType == model.CheckType
                          && hcl.HealthCheckSchedule.Status == "Pending")
            .Select(hcl => hcl.Livestock.TagNumber)
            .Distinct()
            .ToList();

        if (conflictLivestock.Any())
        {
            ModelState.AddModelError("", "The following animals already have a pending health check of this type: " +
                                          string.Join(", ", conflictLivestock));
            RebuildCreateViewBags(model.AssignedToUserId);
            return View(model);
        }

        model.Status = "Pending";
        db.HealthCheckSchedules.Add(model);
        db.SaveChanges();

        foreach (var id in SelectedLivestockIds)
        {
            db.HealthCheckLivestocks.Add(new HealthCheckLivestock
            {
                HealthCheckScheduleId = model.Id,
                LivestockId = id
            });
        }

        db.SaveChanges();

        var assignedUser = db.Users.Find(model.AssignedToUserId);
        if (assignedUser != null && !string.IsNullOrWhiteSpace(assignedUser.Email))
        {
            await SendEmailAsync(assignedUser.Email, "New Health Check Scheduled",
                $"You have been assigned a health check for {model.CheckType} on {model.ScheduledDate:yyyy-MM-dd}.\nNotes: {model.Notes}");
        }

        TempData["Message"] = "Health check schedule created successfully.";
        return RedirectToAction("Index");
    }

    private void RebuildCreateViewBags(int? selectedUserId)
    {
        ViewBag.Users = new SelectList(db.Users.ToList(), "UserId", "FullName", selectedUserId);
        ViewBag.Types = db.Livestocks.Select(l => l.Type).Distinct().ToList();
        ViewBag.Livestocks = db.Livestocks.ToList();

        ViewBag.HealthCheckTypes = new List<string>
    {
        "Vaccination",
        "General Checkup",
        "Deworming",
        "Pregnancy Check",
        "Weight Assessment",
        "Hoof Trimming"
    };
    }


    private async Task SendEmailAsync(string to, string subject, string body)
    {
        var message = new MailMessage();
        message.To.Add(to);
        message.Subject = subject;
        message.Body = body;
        message.IsBodyHtml = false;
        message.From = new MailAddress("as.nkab01@gmail.com");

        using (var smtp = new SmtpClient("smtp.gmail.com", 587))
        {
            smtp.Credentials = new NetworkCredential("as.nkab01@gmail.com", "vmbqzurtzekhxjcv"); // Use app password
            smtp.EnableSsl = true;
            await smtp.SendMailAsync(message);
        }
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult MarkAsCompleted(int id)
    {
        var schedule = db.HealthCheckSchedules
                         .Include(h => h.AssignedToUser)
                         .FirstOrDefault(h => h.Id == id);

        if (schedule == null)
        {
            return HttpNotFound("Health check schedule not found.");
        }

        if (schedule.Status == "Completed")
        {
            TempData["Warning"] = "This schedule has already been marked as completed.";
            return RedirectToAction("Index");
        }

        var livestockLinks = db.HealthCheckLivestocks
                                .Include(l => l.Livestock)
                                .Where(l => l.HealthCheckScheduleId == id)
                                .ToList();

        if (!livestockLinks.Any())
        {
            TempData["Error"] = "No livestock associated with this schedule. Cannot mark as completed.";
            return RedirectToAction("Index");
        }

        foreach (var link in livestockLinks)
        {
            // Prevent duplicate health records for same livestock and date
            bool recordExists = db.HealthRecords.Any(hr =>
                hr.LivestockId == link.LivestockId &&
                DbFunctions.TruncateTime(hr.Date) == DbFunctions.TruncateTime(schedule.ScheduledDate) &&
                hr.EventType == schedule.CheckType);

            if (!recordExists)
            {
                db.HealthRecords.Add(new HealthRecord
                {
                    LivestockId = link.LivestockId,
                    EventType = schedule.CheckType,
                    Notes = schedule.Notes,
                    Date = schedule.ScheduledDate = DateTime.UtcNow,
                    RecordedBy = schedule.AssignedToUser?.FullName ?? "System"
                });
            }
        }

        schedule.Status = "Completed";
        db.SaveChanges();

        TempData["Message"] = "Schedule marked as completed. Health records successfully logged.";
        return RedirectToAction("Index");
    }

    public ActionResult Details(int id)
    {
        var schedule = db.HealthCheckSchedules
            .Include(h => h.AssignedToUser)
            .FirstOrDefault(h => h.Id == id);

        if (schedule == null)
            return HttpNotFound();

        var livestock = db.HealthCheckLivestocks
            .Include(h => h.Livestock)
            .Where(h => h.HealthCheckScheduleId == id)
            .Select(h => h.Livestock)
            .ToList();

        ViewBag.Livestock = livestock;
        return View(schedule);
    }



    public ActionResult Index()
    {
        var schedules = db.HealthCheckSchedules
            .Include(h => h.AssignedToUser)
            .ToList();

        // Get livestock counts from the join table
        var livestockCounts = db.HealthCheckLivestocks
            .GroupBy(l => l.HealthCheckScheduleId)
            .ToDictionary(g => g.Key, g => g.Count());

        ViewBag.LivestockCounts = livestockCounts;

        return View(schedules);
    }

}
