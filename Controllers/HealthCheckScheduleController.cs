using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web.Mvc;
using FarmTrack.Models;

namespace FarmTrack.Controllers
{
    public class HealthCheckScheduleController : Controller
    {
        private FarmTrackContext db = new FarmTrackContext();

        [HttpGet]
        public ActionResult Create()
        {
            ViewBag.Users = new SelectList(db.Users.ToList(), "UserId", "FullName");
            ViewBag.Types = db.Livestocks.Select(l => l.Type).Distinct().ToList();
            ViewBag.Livestocks = db.Livestocks.ToList();
            ViewBag.Veterinarians = new SelectList(db.Veterinarians.Where(v => v.IsActive).ToList(), "Id", "FullName");

            ViewBag.HealthCheckTypes = new List<string>
            {
                "Vaccination",
                "General Checkup",
                "Deworming",
                "Pregnancy Check",
                "Weight Assessment",
                "Hoof Trimming",
                "Emergency Care",
                "Surgery",
                "Specialist Consultation"
            };

            return View(new HealthCheckSchedule());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(HealthCheckSchedule model, List<int> SelectedLivestockIds)
        {
            if (!ModelState.IsValid)
            {
                RebuildCreateViewBags(model.AssignedToUserId, model.VeterinarianId);
                return View(model);
            }

            if (SelectedLivestockIds == null || !SelectedLivestockIds.Any())
            {
                ModelState.AddModelError("", "Please select at least one animal.");
                RebuildCreateViewBags(model.AssignedToUserId, model.VeterinarianId);
                return View(model);
            }

            // Validation for outsourced appointments
            if (model.IsOutsourced)
            {
                if (!model.VeterinarianId.HasValue)
                {
                    ModelState.AddModelError("VeterinarianId", "Please select a veterinarian for outsourced appointments.");
                    RebuildCreateViewBags(model.AssignedToUserId, model.VeterinarianId);
                    return View(model);
                }

                if (string.IsNullOrWhiteSpace(model.Purpose))
                {
                    ModelState.AddModelError("Purpose", "Purpose is required for outsourced appointments.");
                    RebuildCreateViewBags(model.AssignedToUserId, model.VeterinarianId);
                    return View(model);
                }
            }

            // Check for conflicts
            var conflictLivestock = db.HealthCheckLivestocks
                .Include(hcl => hcl.HealthCheckSchedule)
                .Where(hcl => SelectedLivestockIds.Contains(hcl.LivestockId)
                              && hcl.HealthCheckSchedule.CheckType == model.CheckType
                              && hcl.HealthCheckSchedule.Status == "Pending"
                              && DbFunctions.TruncateTime(hcl.HealthCheckSchedule.ScheduledDate) == DbFunctions.TruncateTime(model.ScheduledDate))
                .Select(hcl => hcl.Livestock.TagNumber)
                .Distinct()
                .ToList();

            if (conflictLivestock.Any())
            {
                ModelState.AddModelError("", "The following animals already have a pending health check of this type on the same date: " +
                                              string.Join(", ", conflictLivestock));
                RebuildCreateViewBags(model.AssignedToUserId, model.VeterinarianId);
                return View(model);
            }

            // Check veterinarian availability for outsourced appointments
            if (model.IsOutsourced && model.VeterinarianId.HasValue)
            {
                var vetConflicts = db.HealthCheckSchedules
                    .Where(h => h.VeterinarianId == model.VeterinarianId
                               && h.Status == "Pending"
                               && DbFunctions.TruncateTime(h.ScheduledDate) == DbFunctions.TruncateTime(model.ScheduledDate))
                    .Count();

                if (vetConflicts > 0)
                {
                    ModelState.AddModelError("ScheduledDate", "The selected veterinarian already has an appointment scheduled for this date.");
                    RebuildCreateViewBags(model.AssignedToUserId, model.VeterinarianId);
                    return View(model);
                }
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

            // Send notifications
            await SendNotifications(model);

            TempData["Message"] = model.IsOutsourced ?
                "Veterinary appointment scheduled successfully." :
                "Health check schedule created successfully.";
            return RedirectToAction("Index");
        }

        // GET: Mark as completed - Load the form
        [HttpGet]
        public ActionResult MarkAsCompleted(int id)
        {
            var schedule = db.HealthCheckSchedules
                .Include(s => s.HealthCheckLivestocks.Select(l => l.Livestock))
                .FirstOrDefault(s => s.Id == id);

            if (schedule == null || schedule.Status == "Completed")
            {
                return HttpNotFound();
            }

            var vm = new CompleteHealthCheckViewModel
            {
                ScheduleId = schedule.Id,
                CheckType = schedule.CheckType,
                ScheduledDate = schedule.ScheduledDate,
                LivestockResults = schedule.HealthCheckLivestocks.Select(l => new LivestockHealthResult
                {
                    LivestockId = l.Livestock.LivestockId,
                    TagNumber = l.Livestock.TagNumber,
                    Type = l.Livestock.Type
                }).ToList()
            };

            // Pass info about whether this is outsourced to the view
            ViewBag.IsOutsourced = schedule.IsOutsourced;

            return View(vm);
        }

        // POST: Mark as completed - Process the form
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkAsCompleted(CompleteHealthCheckViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.IsOutsourced = db.HealthCheckSchedules
                    .Where(s => s.Id == model.ScheduleId)
                    .Select(s => s.IsOutsourced)
                    .FirstOrDefault();
                return View(model);
            }

            var schedule = db.HealthCheckSchedules
                .Include(s => s.AssignedToUser)
                .Include(s => s.Veterinarian)
                .FirstOrDefault(s => s.Id == model.ScheduleId);

            if (schedule == null)
            {
                return HttpNotFound();
            }

            // Create health records for each livestock
            foreach (var item in model.LivestockResults)
            {
                var notes = item.Notes;
                if (schedule.IsOutsourced && !string.IsNullOrWhiteSpace(model.VetInstructions))
                {
                    notes += $"\n\nVet Instructions: {model.VetInstructions}";
                }

                db.HealthRecords.Add(new HealthRecord
                {
                    LivestockId = item.LivestockId,
                    EventType = schedule.CheckType,
                    Notes = notes,
                    Date = DateTime.UtcNow,
                    RecordedBy = schedule.IsOutsourced && schedule.Veterinarian != null ?
                                 $"{schedule.Veterinarian.FullName} (Veterinarian)" :
                                 schedule.AssignedToUser?.FullName ?? "System"
                });

                // Update livestock status if marked as sick
                if (item.IsSick)
                {
                    var animal = db.Livestocks.Find(item.LivestockId);
                    if (animal != null)
                    {
                        animal.Status = "Sick";
                    }
                }
            }

            // Update schedule status and outsourced details
            schedule.Status = "Completed";
            if (schedule.IsOutsourced)
            {
                schedule.ActualCost = model.ActualCost;
                schedule.VetInstructions = model.VetInstructions;
                schedule.RequiresFollowUp = model.RequiresFollowUp;
                schedule.FollowUpDate = model.RequiresFollowUp ? model.FollowUpDate : null;
            }

            db.SaveChanges();

            // Create follow-up appointment if required
            if (schedule.RequiresFollowUp && schedule.FollowUpDate.HasValue)
            {
                var followUp = new HealthCheckSchedule
                {
                    CheckType = "Follow-up - " + schedule.CheckType,
                    ScheduledDate = schedule.FollowUpDate.Value,
                    AssignedToUserId = schedule.AssignedToUserId,
                    IsOutsourced = schedule.IsOutsourced,
                    VeterinarianId = schedule.VeterinarianId,
                    Purpose = "Follow-up from previous appointment",
                    Notes = $"Follow-up for appointment completed on {DateTime.UtcNow:yyyy-MM-dd}",
                    Status = "Pending"
                };

                db.HealthCheckSchedules.Add(followUp);
                db.SaveChanges();

                // Link the same livestock to follow-up
                foreach (var item in model.LivestockResults)
                {
                    db.HealthCheckLivestocks.Add(new HealthCheckLivestock
                    {
                        HealthCheckScheduleId = followUp.Id,
                        LivestockId = item.LivestockId
                    });
                }

                db.SaveChanges();
            }

            TempData["Message"] = "Schedule marked as completed and health records logged.";
            return RedirectToAction("Index");
        }

        public ActionResult Details(int id)
        {
            var schedule = db.HealthCheckSchedules
                .Include(h => h.AssignedToUser)
                .Include(h => h.Veterinarian)
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

        public ActionResult Index(string filter = "all", DateTime? date = null)
        {
            var query = db.HealthCheckSchedules
                .Include(h => h.AssignedToUser)
                .Include(h => h.Veterinarian)
                .AsQueryable();

            // Apply filters
            switch (filter.ToLower())
            {
                case "pending":
                    query = query.Where(h => h.Status == "Pending");
                    break;
                case "completed":
                    query = query.Where(h => h.Status == "Completed");
                    break;
                case "outsourced":
                    query = query.Where(h => h.IsOutsourced);
                    break;
                case "inhouse":
                    query = query.Where(h => !h.IsOutsourced);
                    break;
                case "today":
                    var today = DateTime.Today;
                    query = query.Where(h => DbFunctions.TruncateTime(h.ScheduledDate) == today);
                    break;
                case "upcoming":
                    query = query.Where(h => h.ScheduledDate >= DateTime.Today && h.Status == "Pending");
                    break;
            }

            if (date.HasValue)
            {
                query = query.Where(h => DbFunctions.TruncateTime(h.ScheduledDate) == DbFunctions.TruncateTime(date.Value));
            }

            var schedules = query.OrderBy(h => h.ScheduledDate).ToList();

            // Get livestock counts
            var livestockCounts = db.HealthCheckLivestocks
                .GroupBy(l => l.HealthCheckScheduleId)
                .ToDictionary(g => g.Key, g => g.Count());

            ViewBag.LivestockCounts = livestockCounts;
            ViewBag.CurrentFilter = filter;
            ViewBag.CurrentDate = date;

            return View(schedules);
        }

        // Calendar data for AJAX calls
        public JsonResult GetCalendarData(DateTime start, DateTime end)
        {
            var events = db.HealthCheckSchedules
                .Include(h => h.AssignedToUser)
                .Include(h => h.Veterinarian)
                .Where(h => h.ScheduledDate >= start && h.ScheduledDate <= end)
                .Select(h => new
                {
                    id = h.Id,
                    title = h.CheckType + (h.IsOutsourced ? " (Vet)" : ""),
                    start = h.ScheduledDate,
                    color = h.Status == "Completed" ? "#28a745" :
                           (h.IsOutsourced ? "#007bff" : "#6c757d"),
                    extendedProps = new
                    {
                        isOutsourced = h.IsOutsourced,
                        status = h.Status,
                        assignedTo = h.AssignedToUser.FullName,
                        veterinarian = h.Veterinarian != null ? h.Veterinarian.FullName : null
                    }
                })
                .ToList();

            return Json(events, JsonRequestBehavior.AllowGet);
        }

        private async Task SendNotifications(HealthCheckSchedule model)
        {
            // Notify assigned user
            var assignedUser = db.Users.Find(model.AssignedToUserId);
            if (assignedUser != null && !string.IsNullOrWhiteSpace(assignedUser.Email))
            {
                var subject = model.IsOutsourced ? "Veterinary Appointment Scheduled" : "New Health Check Scheduled";
                var body = model.IsOutsourced ?
                    $"A veterinary appointment has been scheduled:\n" +
                    $"Type: {model.CheckType}\n" +
                    $"Date: {model.ScheduledDate:yyyy-MM-dd HH:mm}\n" +
                    $"Veterinarian: {model.Veterinarian?.FullName}\n" +
                    $"Purpose: {model.Purpose}\n" +
                    $"Notes: {model.Notes}" :
                    $"You have been assigned a health check for {model.CheckType} on {model.ScheduledDate:yyyy-MM-dd}.\nNotes: {model.Notes}";

                await SendEmailAsync(assignedUser.Email, subject, body);
            }

            // Notify veterinarian for outsourced appointments
            if (model.IsOutsourced && model.Veterinarian != null && !string.IsNullOrWhiteSpace(model.Veterinarian.Email))
            {
                var animalCount = db.HealthCheckLivestocks.Count(h => h.HealthCheckScheduleId == model.Id);
                var subject = "New Appointment Scheduled - FarmTrack";
                var body = $"A new appointment has been scheduled with you:\n\n" +
                          $"Date: {model.ScheduledDate:yyyy-MM-dd HH:mm}\n" +
                          $"Type: {model.CheckType}\n" +
                          $"Purpose: {model.Purpose}\n" +
                          $"Number of Animals: {animalCount}\n" +
                          $"Estimated Cost: {(model.EstimatedCost.HasValue ? model.EstimatedCost.Value.ToString("C") : "Not specified")}\n" +
                          $"Notes: {model.Notes}\n\n" +
                          $"Contact: {assignedUser?.FullName} - {assignedUser?.Email}";

                await SendEmailAsync(model.Veterinarian.Email, subject, body);
            }
        }

        private void RebuildCreateViewBags(int? selectedUserId, int? selectedVetId = null)
        {
            ViewBag.Users = new SelectList(db.Users.ToList(), "UserId", "FullName", selectedUserId);
            ViewBag.Types = db.Livestocks.Select(l => l.Type).Distinct().ToList();
            ViewBag.Livestocks = db.Livestocks.ToList();
            ViewBag.Veterinarians = new SelectList(db.Veterinarians.Where(v => v.IsActive).ToList(), "Id", "FullName", selectedVetId);

            ViewBag.HealthCheckTypes = new List<string>
            {
                "Vaccination",
                "General Checkup",
                "Deworming",
                "Pregnancy Check",
                "Weight Assessment",
                "Hoof Trimming",
                "Emergency Care",
                "Surgery",
                "Specialist Consultation"
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
                smtp.Credentials = new NetworkCredential("as.nkab01@gmail.com", "vmbqzurtzekhxjcv");
                smtp.EnableSsl = true;
                await smtp.SendMailAsync(message);
            }
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