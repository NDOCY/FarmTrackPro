using FarmTrack.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FarmTrack.Controllers
{
    public class PlotCropsController : Controller
    {
        private FarmTrackContext db = new FarmTrackContext();

        public ActionResult Dashboard(int id)
        {
            var plotCrop = db.PlotCrops
                .Include("Plot") // Use string-based Include for navigation properties
                .Include("Crop")
                .Include("Tasks")
                .FirstOrDefault(pc => pc.Id == id);

            if (plotCrop == null) return HttpNotFound();

            return View(plotCrop);
        }

        private void UpdatePlotStatus(int plotId, string newStatus)
        {
            var plot = db.Plots.FirstOrDefault(p => p.Id == plotId);
            if (plot != null)
            {
                plot.Status = newStatus;
                db.SaveChanges();
            }
        }


        // Controllers/TasksController.cs
        public ActionResult TaskList(int plotCropId)
        {
            var tasks = db.Tasks
                .Where(t => t.PlotCropId == plotCropId)
                .OrderBy(t => t.DueDate)
                .ToList();

            return PartialView("_TaskList", tasks);
        }

        [HttpPost]
        public ActionResult StartPlanting(int id)
        {
            var plotCrop = db.PlotCrops
                .Include("Tasks")
                .FirstOrDefault(pc => pc.Id == id);

            if (plotCrop == null) return HttpNotFound();

            // Check if all preparation tasks are completed
            var incompleteTasks = plotCrop.Tasks
                .Any(t => t.TaskPhase == "Preparation" && t.Status != "Completed");

            if (incompleteTasks)
            {
                TempData["Alert"] = "Cannot start planting - complete all preparation tasks first";
                return RedirectToAction("Dashboard", new { id });
            }

            /*// Update to planting phase
            plotCrop.Status = "Planting";
            plotCrop.DatePlanted = DateTime.Now;

            // Set maturity date
            var cropReq = db.CropRequirement.FirstOrDefault(c => c.CropId == plotCrop.CropId);
            if (cropReq?.GrowthDurationDays != null)
            {
                plotCrop.ExpectedMaturityDate = plotCrop.DatePlanted.Value.AddDays(cropReq.GrowthDurationDays.Value);
            }*/

            db.SaveChanges();

            // Redirect to planting task setup
            return RedirectToAction("SetupPlantingTasks", new { id });
        }
        
        public ActionResult SetupPlantingTasks(int id )
        {
            var plotCrop = db.PlotCrops
                .Include("Crop")
                .FirstOrDefault(pc => pc.Id == id);

            if (plotCrop == null) return HttpNotFound();

            ViewBag.PlotCropId = id;
            ViewBag.CropName = plotCrop.Crop.Name;

            // Pre-populate with default planting tasks
            var model = new PlantingTaskViewModel
            {
                PlotCropId = id,
                Tasks = new List<PlantingTaskInput>
        {
            new PlantingTaskInput { Name = "Initial Planting", DaysAfterPlanting = 0 },
            new PlantingTaskInput { Name = "First Fertilization", DaysAfterPlanting = 7 }
        }
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SetupPlantingTasks(PlantingTaskViewModel model)
        {
            // ✅ Extra guard: Ensure ID exists
            var plotCrop = db.PlotCrops.FirstOrDefault(pc => pc.Id == model.PlotCropId);
            if (plotCrop == null)
            {
                ModelState.AddModelError("", "Invalid crop assignment. Please start planting again.");
                return View(model);
            }

            if (ModelState.IsValid)
            {
                // Remove existing planting tasks for this crop to avoid duplicates
                var existingTasks = db.Tasks
                    .Where(t => t.PlotCropId == model.PlotCropId && t.TaskPhase == "Planting")
                    .ToList();
                if (existingTasks.Any())
                    db.Tasks.RemoveRange(existingTasks);

                // Add the new tasks
                foreach (var taskInput in model.Tasks)
                {
                    if (string.IsNullOrWhiteSpace(taskInput.Name))
                        continue;

                    var task = new FarmTask
                    {
                        PlotCropId = model.PlotCropId,
                        Title = taskInput.Name,
                        TaskPhase = "Planting",
                        DueDate = DateTime.Now.AddDays(taskInput.DaysAfterPlanting),
                        Status = "Pending",
                        AssignedTo = 2, // placeholder
                        AssignedDepartment = "Farming",
                        IsRecurring = taskInput.IsRecurring,
                        RecurrenceType = taskInput.IsRecurring ? taskInput.RecurrenceType : null,
                        LastGeneratedDate = null
                    };

                    db.Tasks.Add(task);
                }

                // Update to planting phase
                plotCrop.Status = "Planting";
                plotCrop.DatePlanted = DateTime.Now;
                // ✅ Update the plot to show it's now planted
                UpdatePlotStatus(plotCrop.PlotId, "Planted");

                // Set maturity date
                var cropReq = db.CropRequirement.FirstOrDefault(c => c.CropId == plotCrop.CropId);
                if (cropReq?.GrowthDurationDays != null)
                {
                    plotCrop.ExpectedMaturityDate = plotCrop.DatePlanted.Value.AddDays(cropReq.GrowthDurationDays.Value);
                }

                db.SaveChanges();

                int userId = Convert.ToInt32(Session["UserId"] ?? 1);
                db.LogActivity(userId, $"Set up planting tasks for PlotCrop ID {model.PlotCropId}");

                return RedirectToAction("Dashboard", "PlotCrops", new { id = model.PlotCropId });
            }

            // If model state invalid, reload crop name for view
            ViewBag.CropName = plotCrop?.Crop?.Name ?? "Unknown Crop";
            return View(model);
        }



        // Recurring task generation (call this daily from a scheduled job)
        public void GenerateRecurringCropTasks()
        {
            var today = DateTime.UtcNow.Date;
            var activePlotCrops = db.PlotCrops
                .Where(pc => pc.Status == "Planting") // Only for active crops
                .Select(pc => pc.Id)
                .ToList();

            var recurringTasks = db.Tasks
                .Where(t => t.IsRecurring && activePlotCrops.Contains(t.PlotCropId.Value))
                .ToList();

            foreach (var task in recurringTasks)
            {
                if (ShouldGenerateTask(task.LastGeneratedDate, task.RecurrenceType, today))
                {
                    var newTask = new FarmTask
                    {
                        PlotCropId = task.PlotCropId,
                        Title = task.Title,
                        Description = task.Description,
                        TaskPhase = task.TaskPhase,
                        DueDate = CalculateNextDueDate(task, today),
                        Status = "Pending",
                        AssignedTo = 2,
                        AssignedDepartment = "Farming",
                        IsRecurring = true,
                        RecurrenceType = task.RecurrenceType,
                        LastGeneratedDate = today
                    };

                    db.Tasks.Add(newTask);
                    task.LastGeneratedDate = today;
                }
            }

            db.SaveChanges();
        }

        private bool ShouldGenerateTask(DateTime? lastDate, string recurrenceType, DateTime today)
        {
            if (!lastDate.HasValue) return true;

            switch (recurrenceType)
            {
                case "Daily":
                    return lastDate.Value.AddDays(1) <= today;
                case "Weekly":
                    return lastDate.Value.AddDays(7) <= today;
                case "Monthly":
                    return lastDate.Value.AddMonths(1) <= today;
                default:
                    return false;
            }
        }

        private DateTime CalculateNextDueDate(FarmTask task, DateTime today)
        {
            var baseDate = task.LastGeneratedDate ?? task.DueDate;

            switch (task.RecurrenceType)
            {
                case "Daily":
                    return baseDate.AddDays(1);
                case "Weekly":
                    return baseDate.AddDays(7);
                case "Monthly":
                    return baseDate.AddMonths(1);
                default:
                    return today;
            }
        }

        public ActionResult PlotCropsList()
        {
            var plotCrops = db.PlotCrops
                .Include("Plot")
                .Include("Crop")
                .OrderByDescending(pc => pc.DateAssigned)
                .ToList();

            return View(plotCrops);
        }

        [HttpPost]
        public ActionResult StartHarvest(int id)
        {
            var plotCrop = db.PlotCrops.Find(id);
            if (plotCrop == null) return HttpNotFound();
                

            plotCrop.Status = "Harvested";
            plotCrop.HarvestDate = DateTime.Now;
            // ✅ Update plot to harvested
            UpdatePlotStatus(plotCrop.PlotId, "Harvested");

            db.SaveChanges();
            // Redirect to log harvest details (Use Case 13)
            return RedirectToAction("LogHarvestOutcome", new { plotCropId = plotCrop.Id });
        }

        public ActionResult LogHarvestOutcome(int plotCropId)
        {
            var plotCrop = db.PlotCrops
                .Include("Crop")
                .Include("Plot")
                .FirstOrDefault(pc => pc.Id == plotCropId);

            if (plotCrop == null) return HttpNotFound();

            var model = new HarvestOutcomeViewModel
            {
                PlotCropId = plotCropId,
                CropName = plotCrop.Crop.Name,
                PlotName = plotCrop.Plot.Name,
                HarvestDate = plotCrop.HarvestDate ?? DateTime.Now
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogHarvestOutcome(HarvestOutcomeViewModel model)
        {
            if (ModelState.IsValid)
            {
                var harvest = new HarvestOutcome
                {
                    PlotCropId = model.PlotCropId,
                    HarvestDate = model.HarvestDate,
                    ActualYieldKg = model.ActualYieldKg,
                    QualityGrade = model.QualityGrade,
                    LossesKg = model.LossesKg,
                    Notes = model.Notes
                };

                db.HarvestOutcomes.Add(harvest);
                db.SaveChanges();

                // Optional: Log the activity
                int userId = Convert.ToInt32(Session["UserId"] ?? 1);
                db.LogActivity(userId, $"Logged harvest outcome for PlotCrop ID {model.PlotCropId}");

                return RedirectToAction("Dashboard", "PlotCrops", new { id = model.PlotCropId });
            }

            return View(model);
        }
        /*
        public ActionResult HarvestAnalytics(int plotCropId)
        {
            var plotCrop = db.PlotCrops.Include("HarvestOutcomes")
                            .FirstOrDefault(pc => pc.Id == plotCropId);
            if (plotCrop == null) return HttpNotFound();

            var outcome = plotCrop.HarvestOutcomes.FirstOrDefault();
            if (outcome == null) return PartialView("_HarvestAnalytics", null);

            var expectedYield = plotCrop.ExpectedYield;
            var actualYield = outcome.ActualYieldKg;
            var lossKg = outcome.LossesKg ?? 0;

            var model = new HarvestAnalyticsViewModel
            {
                ExpectedYield = expectedYield,
                ActualYield = actualYield,
                YieldDifference = actualYield - expectedYield,
                LossKg = lossKg,
                LossPercentage = expectedYield > 0 ? (lossKg / expectedYield) * 100 : 0,
                DaysFromPlantingToHarvest = (outcome.HarvestDate - plotCrop.DatePlanted.Value).Days,
                LossReason = outcome.Notes
            };

            return PartialView("_HarvestAnalytics", model);
        }

        public ActionResult HarvestAnalytics(int id)
        {
            var plotCrop = db.PlotCrops
                .Include("Plot")
                .Include("Crop")
                .FirstOrDefault(pc => pc.Id == id);

            if (plotCrop == null)
                return HttpNotFound();

            // Find the latest harvest outcome for this plot crop
            var outcome = db.HarvestOutcomes
                .Where(h => h.PlotCropId == id)
                .OrderByDescending(h => h.HarvestDate)
                .FirstOrDefault();

            if (outcome == null)
            {
                ViewBag.NoData = true;
                return PartialView("_HarvestAnalytics", null);
            }

            // Calculate values
            double expectedYield = plotCrop.ExpectedYield; // from PlotCrop
            double actualYield = outcome.ActualYieldKg;
            double losses = outcome.LossesKg ?? 0;
            double yieldDiff = actualYield - expectedYield;

            var model = new HarvestAnalyticsViewModel
            {
                PlotCropId = id,
                CropName = plotCrop.Crop.Name,
                PlotName = plotCrop.Plot.Name,
                ExpectedYield = expectedYield,
                ActualYield = actualYield,
                YieldDifference = yieldDiff,
                LossKg = losses,
                LossPercentage = expectedYield > 0 ? Math.Round((losses / expectedYield) * 100, 2) : 0,
                DaysFromPlantingToHarvest = plotCrop.DatePlanted.HasValue
                    ? (outcome.HarvestDate - plotCrop.DatePlanted.Value).Days
                    : 0,
                LossReason = string.IsNullOrEmpty(outcome.Notes) ? "Not specified" : outcome.Notes
            };

            return PartialView("_HarvestAnalytics", model);
        }*/
        public ActionResult HarvestAnalytics(int id)
        {
            var plotCrop = db.PlotCrops
                .Include("Plot")
                .Include("Crop")
                .FirstOrDefault(pc => pc.Id == id);

            if (plotCrop == null)
                return HttpNotFound();

            var outcome = db.HarvestOutcomes
                .Where(h => h.PlotCropId == id)
                .OrderByDescending(h => h.HarvestDate)
                .FirstOrDefault();

            if (outcome == null)
            {
                ViewBag.NoData = true;
                return PartialView("_HarvestAnalytics", null);
            }

            double expectedYield = plotCrop.ExpectedYield;
            double actualYield = outcome.ActualYieldKg;
            double losses = outcome.LossesKg ?? 0;
            double yieldDiff = actualYield - expectedYield;

            var model = new HarvestAnalyticsViewModel
            {
                PlotCropId = id,
                CropName = plotCrop.Crop.Name,
                PlotName = plotCrop.Plot.Name,
                ExpectedYield = expectedYield,
                ActualYield = actualYield,
                YieldDifference = yieldDiff,
                LossKg = losses,
                LossPercentage = expectedYield > 0 ? Math.Round((losses / expectedYield) * 100, 2) : 0,
                DaysFromPlantingToHarvest = plotCrop.DatePlanted.HasValue
                    ? (outcome.HarvestDate - plotCrop.DatePlanted.Value).Days
                    : 0,
                LossReason = string.IsNullOrEmpty(outcome.Notes) ? "Not specified" : outcome.Notes
            };

            return PartialView("_HarvestAnalytics", model);
        }




        private void GeneratePreparationTasks(int plotCropId)
        {
            var defaultTasks = new List<FarmTask>
        {
            new FarmTask {
                PlotCropId = plotCropId,
                Title = "Soil Preparation",
                Description = "Prepare soil for planting",
                TaskPhase = "Preparation",
                AssignedTo = 2,
                AssignedDepartment = "Farming",
                DueDate = DateTime.Now.AddDays(2),
                Status = "Pending"
            },
            new FarmTask {
                PlotCropId = plotCropId,
                Title = "Fertilization",
                Description = "Apply base fertilizers",
                TaskPhase = "Preparation",
                AssignedTo = 2,
                AssignedDepartment = "Farming",
                DueDate = DateTime.Now.AddDays(2),
                Status = "Pending"
            },
            new FarmTask {
                PlotCropId = plotCropId,
                Title = "Irrigation Setup",
                Description = "Prepare irrigation system",
                TaskPhase = "Preparation",
                AssignedTo = 2,
                AssignedDepartment = "Farming",
                DueDate = DateTime.Now.AddDays(2),
                Status = "Pending"
            }
        };

            db.Tasks.AddRange(defaultTasks);
        }

        public ActionResult ActivityList(int plotCropId)
        {
            var activities = db.Activities
                .Where(a => a.PlotCropId == plotCropId)
                .OrderByDescending(a => a.ActivityDate)
                .ToList();

            return PartialView("_ActivityList", activities);
        }

        public ActionResult ActivityStats(int plotCropId)
        {
            var stats = new ActivityStatsViewModel
            {
                TotalActivities = db.Activities.Count(a => a.PlotCropId == plotCropId),
                LastFertilization = db.Activities
                    .Where(a => a.PlotCropId == plotCropId && a.ActivityType == "Fertilization")
                    .OrderByDescending(a => a.ActivityDate)
                    .FirstOrDefault(),
                PestCount = db.Activities.Count(a => a.PlotCropId == plotCropId && a.ActivityType == "Pest"),
                DiseaseCount = db.Activities.Count(a => a.PlotCropId == plotCropId && a.ActivityType == "Disease")
            };

            return PartialView("_ActivityStats", stats);
        }

        public ActionResult ActivityForm(int plotCropId)
        {
            var model = new ActivityViewModel
            {
                PlotCropId = plotCropId,
                ActivityDate = DateTime.Now,

                AvailableTags = new MultiSelectList(db.Tags.ToList(), "Id", "Name")
            };

            ViewBag.ActivityTypes = new SelectList(new[] {
        "Fertilization", "Pest", "Disease", "Weeding",
        "GrowthRecording", "Irrigation", "Pruning", "Other"
    });

            ViewBag.SeverityLevels = new SelectList(new[] {
        "Low", "Medium", "High", "Critical"
    });

            ViewBag.GrowthStages = new SelectList(new[] {
        "Seedling", "Vegetative", "Flowering", "Fruiting", "Mature"
    });

            return PartialView("_ActivityForm", model);
        }

        public ActionResult SeverityTrends(int plotCropId)
        {
            var activities = db.Activities
                .Where(a => a.PlotCropId == plotCropId &&
                           (a.ActivityType == "Pest" || a.ActivityType == "Disease"))
                .ToList();

            return PartialView("_SeverityTrends", activities);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddActivity(ActivityViewModel model)
        {
            // ✅ Ensure PlotCrop exists
            var plotCropExists = db.PlotCrops.Any(pc => pc.Id == model.PlotCropId);
            if (!plotCropExists)
            {
                ModelState.AddModelError("", "Invalid plot crop ID.");
                return PartialView("_ActivityForm", model);
            }

            if (ModelState.IsValid)
            {
                // Handle optional image upload
                string imagePath = null;
                if (model.ImageFile != null && model.ImageFile.ContentLength > 0)
                {
                    // Create upload folder if it doesn't exist
                    var uploadsDir = Server.MapPath("~/Uploads/Activities");
                    if (!Directory.Exists(uploadsDir))
                        Directory.CreateDirectory(uploadsDir);

                    // Save file with unique name
                    string fileName = Guid.NewGuid() + Path.GetExtension(model.ImageFile.FileName);
                    string savePath = Path.Combine(uploadsDir, fileName);
                    model.ImageFile.SaveAs(savePath);

                    // Store relative path for DB
                    imagePath = Url.Content("~/Uploads/Activities/" + fileName);
                }

                var activity = new Activity
                {
                    PlotCropId = model.PlotCropId,
                    ActivityType = model.ActivityType,
                    ActivityDate = model.ActivityDate,
                    Description = model.Description,
                    Notes = model.Notes,
                    AmountUsed = model.AmountUsed,
                    Unit = model.Unit,
                    ProductName = model.ProductName,
                    Severity = model.Severity,
                    GrowthStage = model.GrowthStage,
                    MeasurementValue = model.MeasurementValue,
                    ImagePath = imagePath
                };

                // ✅ Assign tags if selected
                if (model.SelectedTagIds != null && model.SelectedTagIds.Any())
                {
                    activity.Tags = db.Tags
                        .Where(t => model.SelectedTagIds.Contains(t.Id))
                        .ToList();
                }

                db.Activities.Add(activity);

                // ✅ Also log in GrowthRecords if it's a growth activity
                if (model.ActivityType == "GrowthRecording")
                {
                    var growthRecord = new GrowthRecord
                    {
                        PlotCropId = model.PlotCropId,
                        DateRecorded = model.ActivityDate,
                        Stage = model.GrowthStage,
                        Notes = model.Description
                    };
                    db.GrowthRecords.Add(growthRecord);
                }

                db.SaveChanges();

                // Optional: log activity
                int userId = Convert.ToInt32(Session["UserId"] ?? 1);
                db.LogActivity(userId, $"Added new activity '{model.ActivityType}' for PlotCrop {model.PlotCropId}");

                // Return success for AJAX
                return Json(new { success = true });
            }

            // If we got here, validation failed → reload form
            model.AvailableTags = new MultiSelectList(db.Tags.ToList(), "Id", "Name", model.SelectedTagIds);

            ViewBag.ActivityTypes = new SelectList(new[] {
        "Fertilization", "Pest", "Disease", "Weeding",
        "GrowthRecording", "Irrigation", "Pruning", "Other"
    }, model.ActivityType);

            ViewBag.SeverityLevels = new SelectList(new[] {
        "Low", "Medium", "High", "Critical"
    }, model.Severity);

            ViewBag.GrowthStages = new SelectList(new[] {
        "Seedling", "Vegetative", "Flowering", "Fruiting", "Mature"
    }, model.GrowthStage);

            return PartialView("_ActivityForm", model);
        }


        public ActionResult GrowthRecords(int id)
        {
            var records = db.GrowthRecords
                .Where(r => r.PlotCropId == id)
                .OrderByDescending(r => r.DateRecorded)
                .ToList();

            ViewBag.PlotCropId = id;
            return PartialView("_GrowthRecords", records);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddGrowthRecord(int plotCropId, string stage, string notes)
        {
            var record = new GrowthRecord
            {
                PlotCropId = plotCropId,
                DateRecorded = DateTime.Now,
                Stage = stage,
                Notes = notes
            };

            db.GrowthRecords.Add(record);
            db.SaveChanges();

            return RedirectToAction("GrowthRecords", new { id = plotCropId });
        }


    }
}
