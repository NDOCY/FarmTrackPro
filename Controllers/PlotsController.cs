using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using FarmPro.Models;
using FarmTrack.Models;

namespace FarmTrack.Controllers
{
    public class PlotsController : Controller
    {
        private FarmTrackContext db = new FarmTrackContext();

        // GET: Plots
        public ActionResult Index(string status)
        {
            var plots = db.Plots.Include(p => p.Crop);
            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                plots = plots.Where(p => p.Status.ToString() == status);
            }

            ViewBag.Statuses = new SelectList(db.Plots.Select(p => p.Status).Distinct());
            return View(plots.ToList());
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


        // GET: Plots/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var plot = db.Plots
                .Include(p => p.PlotCrops)
                .FirstOrDefault(p => p.Id == id);

            if (plot == null)
                return HttpNotFound();

            return View(plot);
        }


        // GET: Plots/Create
        public ActionResult Create()
        {
            // FIX 1: Use correct ViewBag property name that matches the view
            ViewBag.CropList = new SelectList(db.Crops, "Id", "Name");
            return View();
        }

        // POST: Plots/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Plot plot)
        {
            try
            {
                // Debug output
                System.Diagnostics.Debug.WriteLine($"Plot Name: {plot.Name}");
                System.Diagnostics.Debug.WriteLine($"Coordinates: {plot.Coordinates}");
                System.Diagnostics.Debug.WriteLine($"Status: {plot.Status}");
                System.Diagnostics.Debug.WriteLine($"SoilType: {plot.SoilType}");
                System.Diagnostics.Debug.WriteLine($"CropId: {plot.CropId}");

                // FIX 2: Additional validation for required fields
                if (string.IsNullOrEmpty(plot.Coordinates))
                {
                    ModelState.AddModelError("Coordinates", "Plot boundary is required. Please draw the plot on the map.");
                }

                if (string.IsNullOrEmpty(plot.SoilType))
                {
                    ModelState.AddModelError("SoilType", "Soil type is required.");
                }

                if (ModelState.IsValid)
                {
                    // FIX 3: Set default status if not provided
                    if (string.IsNullOrWhiteSpace(plot.Status))
                    {
                        plot.Status = "Idle"; // Default if none selected
                    }


                    db.Plots.Add(plot);
                    db.SaveChanges();

                    int userId = Convert.ToInt32(Session["UserId"] ?? 1); // Default to 1 if null
                    db.LogActivity(userId, $"Added new plot: {plot.Name}");

                    TempData["SuccessMessage"] = "Plot created successfully!";
                    return RedirectToAction("Index");
                }
                else
                {
                    // Log model state errors for debugging
                    var errors = ModelState.Values.SelectMany(v => v.Errors);
                    foreach (var error in errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"ModelState Error: {error.ErrorMessage}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                ModelState.AddModelError("", "An error occurred while saving. Please try again.");
            }

            // FIX 4: Ensure ViewBag is set again on return
            ViewBag.CropList = new SelectList(db.Crops, "Id", "Name", plot.CropId);
            return View(plot);
        }

        // GET: Plots/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Plot plot = db.Plots.Find(id);
            if (plot == null)
            {
                return HttpNotFound();
            }
            ViewBag.CropId = new SelectList(db.Crops, "Id", "Name", plot.CropId);
            return View(plot);
        }

        // POST: Plots/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Name,CropId,SoilType,IrrigationMethod,FertilizerType,SizeInHectares,PlantingDate,MaturityDate,Status,Coordinates,LastInspectionDate,IrrigationFrequency,Notes")] Plot plot)
        {
            if (ModelState.IsValid)
            {
                db.Entry(plot).State = EntityState.Modified;
                db.SaveChanges();
                int userId = Convert.ToInt32(Session["UserId"]);
                db.LogActivity(userId, $"Updated plot: {plot.Name} ; plot: {plot.Id}");
                TempData["SuccessMessage"] = "Plot updated successfully!";
                return RedirectToAction("Index");
            }
            ViewBag.CropId = new SelectList(db.Crops, "Id", "Name", plot.CropId);
            return View(plot);
        }

        // GET: Plots/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Plot plot = db.Plots.Find(id);
            if (plot == null)
            {
                return HttpNotFound();
            }
            return View(plot);
        }

        // POST: Plots/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Plot plot = db.Plots.Find(id);
            db.Plots.Remove(plot);
            db.SaveChanges();
            int userId = Convert.ToInt32(Session["UserId"]);
            db.LogActivity(userId, $"Deleted plot: {plot.Name} ; plot: {plot.Id}");
            TempData["SuccessMessage"] = "Plot deleted successfully!";
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

        // Rest of your methods remain the same...
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
                    Description = "Setup irrigation system",
                    TaskPhase = "Preparation",
                    AssignedTo = 2,
                    AssignedDepartment = "Farming",
                    DueDate = DateTime.Now.AddDays(2),
                    Status = "Pending"
                }
            };

            db.Tasks.AddRange(defaultTasks);
        }
        /*
        public ActionResult AssignCrop(int id, int? cropId)
        {
            var plot = db.Plots.Find(id);
            if (plot == null) return HttpNotFound();

            if (cropId == null)
            {
                // Show crop selection page instead of assigning directly
                var crops = db.Crops.ToList();
                ViewBag.CropList = new SelectList(crops, "Id", "Name");
                ViewBag.PlotId = id;
                return View("SelectCrop");
            }

            // Continue with assignment
            var plotCrop = new PlotCrop
            {
                PlotId = id,
                CropId = cropId.Value,
                Status = "Preparation",
                ExpectedYield = db.CropRequirement
                    .FirstOrDefault(c => c.CropId == cropId.Value)?.ExpectedYieldKgPerHectare ?? 0
            };

            db.PlotCrops.Add(plotCrop);
            GeneratePreparationTasks(plotCrop.Id);
            db.SaveChanges();

            return RedirectToAction("Dashboard", "PlotCrops", new { id = plotCrop.Id });
        }*/

        // GET: Plots/AssignCrop/5
        public ActionResult AssignCrop(int id)
        {
            var plot = db.Plots.Find(id);
            if (plot == null) return HttpNotFound();

            // Get all crops with their requirements
            var allCrops = db.Crops.Include(c => c.CropRequirement).ToList();

            var recommendedCrops = allCrops
                .Where(c => c.CropRequirement != null &&
                            !string.IsNullOrEmpty(c.CropRequirement.PreferredSoil) &&
                            c.CropRequirement.PreferredSoil
                                .ToLower()
                                .Contains(plot.SoilType.ToLower()))
                .ToList();

            var otherCrops = allCrops.Except(recommendedCrops).ToList();

            ViewBag.RecommendedCrops = new SelectList(recommendedCrops, "Id", "Name");
            ViewBag.OtherCrops = new SelectList(otherCrops, "Id", "Name");
            ViewBag.PlotName = plot.Name;
            ViewBag.SoilType = plot.SoilType;

            return View(plot); // This uses your assigncrop.cshtml
        }

        // POST: Plots/AssignCrop
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AssignCrop(int id, int cropId)
        {
            var plot = db.Plots.Find(id);
            if (plot == null) return HttpNotFound();

            // Check if there is already an active crop assignment for this plot
            //bool cropAlreadyAssigned = db.PlotCrops
            //  .Any(pc => pc.PlotId == id && pc.Status != "Harvested");
            bool cropAlreadyAssigned = db.PlotCrops
                .Any(pc => pc.PlotId == id &&
                (pc.Status == "Preparation" || pc.Status == "Planting" || pc.Status == "Growing"));


            if (cropAlreadyAssigned)
            {
                TempData["ErrorMessage"] = "This plot already has an assigned crop.";
                return RedirectToAction("Details", new { id });
            }

            var plotCrop = new PlotCrop
            {
                PlotId = id,
                CropId = cropId,
                Status = "Preparation",
                ExpectedYield = db.CropRequirement
                    .FirstOrDefault(c => c.CropId == cropId)?
                    .ExpectedYieldKgPerHectare ?? 0
            };

            db.PlotCrops.Add(plotCrop);
            db.SaveChanges();

            // ✅ NEW: Update plot status when crop is assigned
            UpdatePlotStatus(plotCrop.PlotId, "Assigned");

            GeneratePreparationTasks(plotCrop.Id);
            db.SaveChanges();

            return RedirectToAction("Dashboard", "PlotCrops", new { id = plotCrop.Id });
        }



    }
}