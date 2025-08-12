using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using FarmTrack.Models;

public class EmergencyController : Controller
{
    private FarmTrackContext db = new FarmTrackContext();

    // Emergency Dashboard
    public ActionResult Index()
    {
        var viewModel = new EmergencyDashboardViewModel
        {
            EmergencyContacts = db.EmergencyContacts
                .OrderBy(c => c.ContactType)
                .ThenBy(c => c.IsPrimary ? 0 : 1)
                .ToList(),

            CareGuidelines = db.CareGuidelines
                .OrderBy(g => g.AnimalType)
                .ThenBy(g => g.EmergencyLevel == "Critical" ? 0 :
                           g.EmergencyLevel == "Urgent" ? 1 : 2)
                .ToList(),

            RecentEmergencies = db.HealthCheckSchedules
                .Include(h => h.AssignedToUser)
                .Include(h => h.Veterinarian)
                .Where(h => h.CheckType.Contains("Emergency") &&
                           h.ScheduledDate >= DbFunctions.AddDays(DateTime.Now, -30))
                .OrderByDescending(h => h.ScheduledDate)
                .Take(10)
                .ToList()
        };

        return View(viewModel);
    }

    // Emergency Contacts Management
    public ActionResult Contacts()
    {
        var contacts = db.EmergencyContacts
            .OrderBy(c => c.ContactType)
            .ThenBy(c => c.IsPrimary ? 0 : 1)
            .ToList();

        return View(contacts);
    }

    [HttpGet]
    public ActionResult CreateContact()
    {
        ViewBag.ContactTypes = new List<string>
        {
            "Primary Veterinarian",
            "Emergency Veterinarian",
            "Farm Manager",
            "Farm Owner",
            "Animal Specialist",
            "Local Authority",
            "Emergency Services",
            "Insurance Provider"
        };

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult CreateContact(EmergencyContact contact)
    {
        if (ModelState.IsValid)
        {
            // Ensure only one primary contact per type
            if (contact.IsPrimary)
            {
                var existingPrimary = db.EmergencyContacts
                    .Where(c => c.ContactType == contact.ContactType && c.IsPrimary)
                    .ToList();

                foreach (var existing in existingPrimary)
                {
                    existing.IsPrimary = false;
                }
            }

            db.EmergencyContacts.Add(contact);
            db.SaveChanges();

            TempData["Message"] = "Emergency contact added successfully.";
            return RedirectToAction("Contacts");
        }

        ViewBag.ContactTypes = new List<string>
        {
            "Primary Veterinarian",
            "Emergency Veterinarian",
            "Farm Manager",
            "Farm Owner",
            "Animal Specialist",
            "Local Authority",
            "Emergency Services",
            "Insurance Provider"
        };

        return View(contact);
    }

    [HttpGet]
    public ActionResult EditContact(int id)
    {
        var contact = db.EmergencyContacts.Find(id);
        if (contact == null)
            return HttpNotFound();

        ViewBag.ContactTypes = new List<string>
        {
            "Primary Veterinarian",
            "Emergency Veterinarian",
            "Farm Manager",
            "Farm Owner",
            "Animal Specialist",
            "Local Authority",
            "Emergency Services",
            "Insurance Provider"
        };

        return View(contact);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult EditContact(EmergencyContact contact)
    {
        if (ModelState.IsValid)
        {
            // Ensure only one primary contact per type
            if (contact.IsPrimary)
            {
                var existingPrimary = db.EmergencyContacts
                    .Where(c => c.ContactType == contact.ContactType &&
                               c.IsPrimary && c.Id != contact.Id)
                    .ToList();

                foreach (var existing in existingPrimary)
                {
                    existing.IsPrimary = false;
                }
            }

            db.Entry(contact).State = EntityState.Modified;
            db.SaveChanges();

            TempData["Message"] = "Emergency contact updated successfully.";
            return RedirectToAction("Contacts");
        }

        ViewBag.ContactTypes = new List<string>
        {
            "Primary Veterinarian",
            "Emergency Veterinarian",
            "Farm Manager",
            "Farm Owner",
            "Animal Specialist",
            "Local Authority",
            "Emergency Services",
            "Insurance Provider"
        };

        return View(contact);
    }

    // Care Guidelines Management
    public ActionResult Guidelines(string animalType = "", string issueType = "")
    {
        var query = db.CareGuidelines.AsQueryable();

        if (!string.IsNullOrEmpty(animalType))
            query = query.Where(g => g.AnimalType == animalType);

        if (!string.IsNullOrEmpty(issueType))
            query = query.Where(g => g.IssueType.Contains(issueType));

        var guidelines = query
            .OrderBy(g => g.AnimalType)
            .ThenBy(g => g.EmergencyLevel == "Critical" ? 0 :
                        g.EmergencyLevel == "Urgent" ? 1 : 2)
            .ThenBy(g => g.IssueType)
            .ToList();

        ViewBag.AnimalTypes = db.CareGuidelines
            .Select(g => g.AnimalType)
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        ViewBag.IssueTypes = db.CareGuidelines
            .Select(g => g.IssueType)
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        ViewBag.SelectedAnimalType = animalType;
        ViewBag.SelectedIssueType = issueType;

        return View(guidelines);
    }

    [HttpGet]
    public ActionResult CreateGuideline()
    {
        ViewBag.AnimalTypes = db.Livestocks.Select(l => l.Type).Distinct().ToList();
        ViewBag.EmergencyLevels = new List<string> { "Critical", "Urgent", "Moderate" };
        ViewBag.CommonIssues = new List<string>
        {
            "Burns", "Cuts/Wounds", "Bone Fracture", "Choking", "Poisoning",
            "Respiratory Distress", "Bloat", "Prolapse", "Birthing Complications",
            "Heat Stroke", "Hypothermia", "Eye Injury", "Severe Bleeding",
            "Seizures", "Loss of Consciousness", "Allergic Reaction"
        };

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult CreateGuideline(CareGuideline guideline)
    {
        if (ModelState.IsValid)
        {
            guideline.CreatedDate = DateTime.UtcNow;
            guideline.LastUpdated = DateTime.UtcNow;

            db.CareGuidelines.Add(guideline);
            db.SaveChanges();

            TempData["Message"] = "Care guideline created successfully.";
            return RedirectToAction("Guidelines");
        }

        ViewBag.AnimalTypes = db.Livestocks.Select(l => l.Type).Distinct().ToList();
        ViewBag.EmergencyLevels = new List<string> { "Critical", "Urgent", "Moderate" };
        ViewBag.CommonIssues = new List<string>
        {
            "Burns", "Cuts/Wounds", "Bone Fracture", "Choking", "Poisoning",
            "Respiratory Distress", "Bloat", "Prolapse", "Birthing Complications",
            "Heat Stroke", "Hypothermia", "Eye Injury", "Severe Bleeding",
            "Seizures", "Loss of Consciousness", "Allergic Reaction"
        };

        return View(guideline);
    }

    [HttpGet]
    public ActionResult EditGuideline(int id)
    {
        var guideline = db.CareGuidelines.Find(id);
        if (guideline == null)
            return HttpNotFound();

        ViewBag.AnimalTypes = db.Livestocks.Select(l => l.Type).Distinct().ToList();
        ViewBag.EmergencyLevels = new List<string> { "Critical", "Urgent", "Moderate" };
        ViewBag.CommonIssues = new List<string>
        {
            "Burns", "Cuts/Wounds", "Bone Fracture", "Choking", "Poisoning",
            "Respiratory Distress", "Bloat", "Prolapse", "Birthing Complications",
            "Heat Stroke", "Hypothermia", "Eye Injury", "Severe Bleeding",
            "Seizures", "Loss of Consciousness", "Allergic Reaction"
        };

        return View(guideline);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult EditGuideline(CareGuideline guideline)
    {
        if (ModelState.IsValid)
        {
            guideline.LastUpdated = DateTime.UtcNow;
            db.Entry(guideline).State = EntityState.Modified;
            db.SaveChanges();

            TempData["Message"] = "Care guideline updated successfully.";
            return RedirectToAction("Guidelines");
        }

        ViewBag.AnimalTypes = db.Livestocks.Select(l => l.Type).Distinct().ToList();
        ViewBag.EmergencyLevels = new List<string> { "Critical", "Urgent", "Moderate" };
        ViewBag.CommonIssues = new List<string>
        {
            "Burns", "Cuts/Wounds", "Bone Fracture", "Choking", "Poisoning",
            "Respiratory Distress", "Bloat", "Prolapse", "Birthing Complications",
            "Heat Stroke", "Hypothermia", "Eye Injury", "Severe Bleeding",
            "Seizures", "Loss of Consciousness", "Allergic Reaction"
        };

        return View(guideline);
    }

    // Quick Reference for Mobile/Emergency Use
    public ActionResult QuickReference()
    {
        var criticalGuidelines = db.CareGuidelines
            .Where(g => g.EmergencyLevel == "Critical")
            .OrderBy(g => g.AnimalType)
            .ThenBy(g => g.IssueType)
            .ToList();

        var emergencyContacts = db.EmergencyContacts
            .Where(c => c.IsPrimary || c.ContactType.Contains("Emergency") || c.ContactType.Contains("Veterinarian"))
            .OrderBy(c => c.ContactType)
            .ToList();

        ViewBag.EmergencyContacts = emergencyContacts;

        return View(criticalGuidelines);
    }

    // Search functionality for quick access during emergencies
    public ActionResult Search(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return RedirectToAction("Index");
        }

        var guidelines = db.CareGuidelines
            .Where(g => g.IssueType.Contains(searchTerm) ||
                       g.AnimalType.Contains(searchTerm) ||
                       g.ImmediateActions.Contains(searchTerm))
            .OrderBy(g => g.EmergencyLevel == "Critical" ? 0 :
                        g.EmergencyLevel == "Urgent" ? 1 : 2)
            .ToList();

        var contacts = db.EmergencyContacts
            .Where(c => c.Name.Contains(searchTerm) ||
                       c.ContactType.Contains(searchTerm) ||
                       c.Phone.Contains(searchTerm))
            .ToList();

        ViewBag.SearchTerm = searchTerm;
        ViewBag.EmergencyContacts = contacts;

        return View("SearchResults", guidelines);
    }

    // Emergency appointment creation (quick access)
    [HttpPost]
    public ActionResult CreateEmergencyAppointment(int livestockId, string issueType, bool requiresVet = true)
    {
        try
        {
            var livestock = db.Livestocks.Find(livestockId);
            if (livestock == null)
            {
                return Json(new { success = false, message = "Animal not found." });
            }

            var schedule = new HealthCheckSchedule
            {
                CheckType = "Emergency Care",
                Purpose = issueType,
                ScheduledDate = DateTime.Now,
                AssignedToUserId = 1, // Default to first user or current user
                IsOutsourced = requiresVet,
                Status = "Pending",
                Notes = $"Emergency case: {issueType}. Created at {DateTime.Now}",
                VeterinarianId = requiresVet ? GetEmergencyVeterinarian()?.Id : null
            };

            db.HealthCheckSchedules.Add(schedule);
            db.SaveChanges();

            db.HealthCheckLivestocks.Add(new HealthCheckLivestock
            {
                HealthCheckScheduleId = schedule.Id,
                LivestockId = livestockId
            });
            db.SaveChanges();

            return Json(new
            {
                success = true,
                message = "Emergency appointment created successfully.",
                appointmentId = schedule.Id
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error creating appointment: " + ex.Message });
        }
    }

    private Veterinarian GetEmergencyVeterinarian()
    {
        // Try to find emergency veterinarian first
        return db.Veterinarians
            .Where(v => v.IsActive && v.Specialization.Contains("Emergency"))
            .FirstOrDefault() ??
               db.Veterinarians
            .Where(v => v.IsActive)
            .FirstOrDefault();
    }

    // Get care guideline by issue type (AJAX)
    public JsonResult GetGuideline(string animalType, string issueType)
    {
        var guideline = db.CareGuidelines
            .Where(g => g.AnimalType == animalType && g.IssueType == issueType)
            .FirstOrDefault();

        if (guideline == null)
        {
            // Try to find a general guideline for the issue type
            guideline = db.CareGuidelines
                .Where(g => g.IssueType == issueType)
                .OrderBy(g => g.EmergencyLevel == "Critical" ? 0 : 1)
                .FirstOrDefault();
        }

        if (guideline != null)
        {
            return Json(new
            {
                success = true,
                guideline = new
                {
                    emergencyLevel = guideline.EmergencyLevel,
                    immediateActions = guideline.ImmediateActions,
                    whatNotToDo = guideline.WhatNotToDo,
                    whenToCallVet = guideline.WhenToCallVet,
                    additionalNotes = guideline.AdditionalNotes
                }
            }, JsonRequestBehavior.AllowGet);
        }

        return Json(new { success = false, message = "No guideline found for this issue." }, JsonRequestBehavior.AllowGet);
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
