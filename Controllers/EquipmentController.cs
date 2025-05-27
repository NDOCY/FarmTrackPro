using FarmTrack.Models;
using FarmTrack.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Mail;
using System.Data.Entity;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace FarmPro.Controllers
{
    public class EquipmentController : Controller
    {
        private FarmTrackContext db = new FarmTrackContext();
        // GET: Equipment
        [HttpGet]
        public ActionResult Create()
        {
            var categories = db.Equipments
                .Select(e => e.Category)
                .Distinct()
                .ToList();

            ViewBag.CategoryList = new SelectList(categories);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(Equipment equipment, HttpPostedFileBase ImageUpload, string SelectedCategory, string NewCategory)
        {
            if (ModelState.IsValid)
            {
                // Use new category if provided, otherwise selected
                equipment.Category = !string.IsNullOrWhiteSpace(NewCategory) ? NewCategory : SelectedCategory;

                if (ImageUpload != null && ImageUpload.ContentLength > 0)
                {
                    var blobService = new BlobService(ConfigurationManager.AppSettings["AzureBlobConnection"]);
                    string imageUrl = await blobService.UploadFileAsync(ImageUpload);
                    equipment.ImagePath = imageUrl;
                }

                db.Equipments.Add(equipment);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            var categories = db.Equipments.Select(e => e.Category).Distinct().ToList();
            ViewBag.CategoryList = new SelectList(categories);
            return View(equipment);
        }



        public ActionResult Index(string category)
        {
            var categories = db.Equipments
                               .Select(e => e.Category)
                               .Distinct()
                               .ToList();

            ViewBag.CategoryFilter = new SelectList(categories);

            var equipmentList = db.Equipments.AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                equipmentList = equipmentList.Where(e => e.Category == category);
            }

            return View(equipmentList.ToList());
        }


        public ActionResult Details(int id)
        {
            var item = db.Equipments.Find(id);
            var equipment = db.Equipments
                            .Include("EquipmentRepairs.InHouseUser")
                            .FirstOrDefault(e => e.EquipmentId == id);

            if (item == null) return HttpNotFound();
            return View(item);
        }

        // Updated GET method for ScheduleRepair
        // Updated GET method for ScheduleRepair
        public ActionResult ScheduleRepair(int equipmentId)
        {
            var equipment = db.Equipments.Find(equipmentId);
            if (equipment == null) return HttpNotFound();

            var maintenanceUsers = db.Users
                .Where(u => u.Department == "Maintenance")
                .Select(u => new SelectListItem
                {
                    Value = u.UserId.ToString(),
                    Text = u.FullName + " (" + u.Email + ")"
                }).ToList();

            // Get all active repairs for this equipment (both in-house and outsourced)
            var activeRepairs = db.EquipmentRepairs
                .Where(r => r.EquipmentId == equipmentId && r.Status != "Completed")
                .OrderBy(r => r.RepairDate)
                .ToList();

            ViewBag.MaintenanceUsers = maintenanceUsers;
            ViewBag.EquipmentName = equipment.Name;
            ViewBag.EquipmentId = equipmentId;
            ViewBag.ActiveRepairs = activeRepairs; // Pass all active repairs to view

            var model = new EquipmentRepair
            {
                EquipmentId = equipmentId,
                RepairDate = DateTime.Today
            };

            return View(model);
        }

        // Updated POST method for ScheduleRepair
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ScheduleRepair(EquipmentRepair repair)
        {
            if (ModelState.IsValid)
            {
                // Check for duplicate In-house repair schedule
                if (repair.TechnicianType == "In-house")
                {
                    var existingInHouse = db.EquipmentRepairs.Any(r =>
                        r.EquipmentId == repair.EquipmentId &&
                        r.TechnicianType == "In-house" &&
                        r.Status != "Completed");

                    if (existingInHouse)
                    {
                        ModelState.AddModelError("", "An in-house repair for this equipment is already scheduled and not yet completed. Please complete the existing repair before scheduling a new one.");
                    }
                }

                if (ModelState.IsValid)
                {
                    if (repair.TechnicianType == "In-house")
                    {
                        repair.Cost = 0;
                        repair.Status = "Scheduled"; // Ensure status is set

                        var user = db.Users.Find(repair.InHouseUserId);
                        if (user != null)
                        {
                            try
                            {
                                SendRepairEmail(user.Email, user.FullName, repair, isOutsourced: false);
                            }
                            catch (Exception ex)
                            {
                                TempData["Error"] = "Repair scheduled, but email failed: " + ex.Message;
                            }
                        }
                    }
                    else if (repair.TechnicianType == "Outsourced")
                    {
                        repair.Status = "Scheduled"; // Ensure status is set

                        try
                        {
                            SendRepairEmail(repair.OutsourcedEmail, repair.OutsourcedTechnicianName, repair, isOutsourced: true);
                        }
                        catch (Exception ex)
                        {
                            TempData["Error"] = "Repair scheduled, but email failed: " + ex.Message;
                        }
                    }

                    // Update equipment status to 'Scheduled Maintenance'
                    var equipment = db.Equipments.Find(repair.EquipmentId);
                    if (equipment != null)
                    {
                        equipment.Status = "Scheduled Maintenance";
                    }

                    db.EquipmentRepairs.Add(repair);
                    db.SaveChanges();
                    TempData["Message"] = "Repair successfully scheduled.";
                    return RedirectToAction("Details", new { id = repair.EquipmentId });
                }
            }

            // If we got this far, something failed, redisplay form with active repairs
            var maintenanceUsers = db.Users
                .Where(u => u.Department == "Maintenance")
                .Select(u => new SelectListItem
                {
                    Value = u.UserId.ToString(),
                    Text = u.FullName + " (" + u.Email + ")"
                }).ToList();

            var activeRepairs = db.EquipmentRepairs
                .Where(r => r.EquipmentId == repair.EquipmentId && r.Status != "Completed")
                .OrderBy(r => r.RepairDate)
                .ToList();

            ViewBag.MaintenanceUsers = maintenanceUsers;
            ViewBag.ActiveRepairs = activeRepairs;

            return View(repair);
        }


        private void SendRepairEmail(string toEmail, string name, EquipmentRepair repair, bool isOutsourced)
        {
            var equipment = db.Equipments.Find(repair.EquipmentId);
            if (equipment == null) return;

            var subject = isOutsourced
                ? $"Repair Request - {equipment.Name}"
                : $"Assigned Repair Task - {equipment.Name}";

            var body = $@"
        Dear {name},

        {(isOutsourced ? "You have been requested" : "You have been assigned")} to perform a repair on the following equipment:

        🛠 Equipment: {equipment.Name}
        📝 Description: {repair.Description}
        📅 Scheduled Date: {repair.RepairDate.ToString("dddd, dd MMMM yyyy")}

        Please ensure the repair is carried out on time.

        {(isOutsourced ? "\nKindly reply to this email if you have any questions or concerns." : "\nYou can view your task under your assigned maintenance tasks.")}

        Thank you,
        FarmTrack Equipment Team
    ";

            var message = new MailMessage();
            message.To.Add(toEmail);
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = false;
            message.From = new MailAddress("as.nkab01@gmail.com");

            using (var smtp = new SmtpClient("smtp.gmail.com", 587))
            {
                smtp.Credentials = new NetworkCredential("as.nkab01@gmail.com", "vmbqzurtzekhxjcv");
                smtp.EnableSsl = true;
                smtp.Send(message);
            }
        }

        public ActionResult CompleteRepair(int id)
        {
            var schedule = db.EquipmentRepairs
                .Include("Equipment")
                .Include("InHouseUser")
                .FirstOrDefault(r => r.RepairId == id);

            if (schedule == null || schedule.Status == "Completed")
                return HttpNotFound();

            return View(schedule);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CompleteRepair(int id, string logNotes)
        {

            var schedule = db.EquipmentRepairs
                .Include("Equipment")
                .Include("InHouseUser")
                .FirstOrDefault(r => r.RepairId == id);
            if (schedule == null)
                return HttpNotFound();

            schedule.Status = "Completed";
            db.EquipmentRepairLogs.Add(new EquipmentRepairLog
            {
                EquipmentId = schedule.EquipmentId,
                RepairDate = DateTime.Now,
                RepairDetails = logNotes,
                RepairedBy = schedule.TechnicianType == "In-house"
                    ? (schedule.InHouseUser?.FullName ?? "Unknown In-house Technician")
                    : (schedule.OutsourcedTechnicianName ?? "Unknown Outsourced Technician")
            });


            db.SaveChanges();

            return RedirectToAction("Details", new { id = schedule.EquipmentId });
        }
        public ActionResult RepairLogs(int id)
        {
            var equipment = db.Equipments
                              .Include(e => e.RepairLogs)
                              .FirstOrDefault(e => e.EquipmentId == id);

            if (equipment == null)
            {
                return HttpNotFound();
            }

            return View(equipment.RepairLogs); // ✅ This is the fix
        }






    }
}