//using farm.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using FarmTrack.Models;
using QRCoder;
using System.Drawing;
using FarmTrack.Services;
using System.Configuration;
using System.Threading.Tasks;
using System.Net.Mail;
using System.Net;

namespace FarmTrack.Controllers
{
    public class InventoryController : Controller
    {
        private FarmTrackContext db = new FarmTrackContext();

        // GET: Inventory/Create
        public ActionResult Create()
        {
            string currentUserRole = Session["Role"]?.ToString();
            if (currentUserRole != "Owner")
            {
                return RedirectToAction("AdminDashboard", "Dashboard");
            }
            ViewBag.Suppliers = db.Suppliers.ToList();


            return View(new Inventory()); // 💥 Fix: provide an initialized model
        }


        // POST: Inventory/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(Inventory model)
        {
            if (ModelState.IsValid)
            {
                if (Session["UserId"] != null)
                {
                    model.UserId = Convert.ToInt32(Session["UserId"]);
                    model.DateAdded = DateTime.Now;

                    db.Inventories.Add(model);
                    db.SaveChanges(); // Save first to get Inventory.Id

                    // === QR Code Generation ===
                    string detailsUrl = Url.Action("Details", "Inventory", new { id = model.InventoryId }, Request.Url.Scheme);
                    var qrBitmap = GenerateQrBitmap(detailsUrl);

                    // Upload QR code to Azure Blob Storage
                    var blobService = new BlobService(ConfigurationManager.AppSettings["AzureBlobConnection"]);
                    string fileName = $"inventory-qr-{model.InventoryId}.png";
                    string qrUrl = await blobService.UploadQrCodeAsync(qrBitmap, fileName);

                    // Save QR path
                    model.QrCodePath = qrUrl;
                    db.Entry(model).State = EntityState.Modified;
                    db.SaveChanges();

                    // Log activity
                    int userId = Convert.ToInt32(Session["UserId"]);
                    db.LogActivity(userId, $"Created a new item: {model.ItemName}");

                    TempData["Message"] = "Inventory item created with QR code.";
                    return RedirectToAction("InventoryList");
                }
                else
                {
                    ModelState.AddModelError("", "You must be logged in to add an inventory item.");
                }
            }
            ViewBag.Suppliers = db.Suppliers.ToList();

            return View(model);
        }

        public Bitmap GenerateQrBitmap(string tag)
        {
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(tag, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new QRCode(qrCodeData);
            return qrCode.GetGraphic(20); // adjust graphic density if needed
        }


        // GET: Inventory/List
        public ActionResult InventoryList(string search)
        {
            string currentUserRole = Session["Role"]?.ToString();
            if (currentUserRole == "User")
            {
                return RedirectToAction("UserDashboard", "Dashboard");
            }
            var inventoryList = db.Inventories.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                inventoryList = inventoryList.Where(l =>
                    l.ItemName.Contains(search) ||
                    l.Category.Contains(search) ||
                    l.Notes.Contains(search));
            }

            // Pass the search term back to the view for display in the search box
            ViewBag.SearchQuery = search;

            return View(inventoryList.ToList()); // ✅ Return filtered results
        }

        // GET: Inventory/Details/5
        public ActionResult Details(int id)
        {
            var inventoryItem = db.Inventories.Find(id);
            if (inventoryItem == null)
                return HttpNotFound();

            return View(inventoryItem);
        }


        
        // GET: Create Restock Request
        public ActionResult RestockCreate(int? id = null)
        {
            var model = new InventoryRestock();

            // If an ID is provided, we're coming from a specific item page
            if (id.HasValue)
            {
                var item = db.Inventories.Include(i => i.Supplier).FirstOrDefault(i => i.InventoryId == id.Value);
                if (item != null)
                {
                    model.InventoryId = item.InventoryId;
                    model.SupplierId = item.SupplierId ?? 0; // Pre-select supplier if available
                    ViewBag.SelectedItem = item; // Pass the item for display
                    ViewBag.IsFromItemPage = true; // Flag to show/hide dropdown
                }
                else
                {
                    TempData["Error"] = "Item not found.";
                    return RedirectToAction("RestockList");
                }
            }
            else
            {
                ViewBag.IsFromItemPage = false; // Show dropdown for manual selection
            }

            // Populate inventory dropdown (for manual creation)
            ViewBag.InventoryList = new SelectList(
                db.Inventories
                  .Select(i => new { i.InventoryId, i.ItemName })
                  .ToList(),
                "InventoryId", "ItemName", model.InventoryId);

            // Populate supplier dropdown
            ViewBag.SupplierList = new SelectList(
                db.Suppliers
                  .Select(s => new { s.SupplierId, s.Name })
                  .ToList(),
                "SupplierId", "Name", model.SupplierId);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RestockCreate(InventoryRestock model, int? id = null)
        {
            if (ModelState.IsValid)
            {
                // If id is provided, use it; otherwise use the dropdown selection
                int inventoryId = id ?? model.InventoryId;

                var inventoryItem = db.Inventories
                                      .FirstOrDefault(i => i.InventoryId == inventoryId);

                if (inventoryItem == null)
                {
                    TempData["Error"] = "Invalid inventory item selected.";
                    return RedirectToAction("RestockList");
                }

                var supplier = db.Suppliers.FirstOrDefault(s => s.SupplierId == model.SupplierId);
                if (supplier == null)
                {
                    TempData["Error"] = "Invalid supplier selected.";

                    // Repopulate ViewBag data for redisplay
                    if (id.HasValue)
                    {
                        ViewBag.SelectedItem = inventoryItem;
                        ViewBag.IsFromItemPage = true;
                    }
                    else
                    {
                        ViewBag.IsFromItemPage = false;
                    }

                    ViewBag.InventoryList = new SelectList(
                        db.Inventories.Select(i => new { i.InventoryId, i.ItemName }).ToList(),
                        "InventoryId", "ItemName", inventoryId);

                    ViewBag.SupplierList = new SelectList(
                        db.Suppliers.Select(s => new { s.SupplierId, s.Name }).ToList(),
                        "SupplierId", "Name", model.SupplierId);

                    return View(model);
                }

                var restock = new InventoryRestock
                {
                    InventoryId = inventoryId, // Use the resolved inventory ID
                    Quantity = model.Quantity,
                    SupplierId = model.SupplierId,
                    RequestedOn = DateTime.Now,
                    RequestedById = Convert.ToInt32(Session["UserId"]),
                    SupplierNotified = false,
                    IsCompleted = false,
                    Failed = false
                };

                try
                {
                    db.InventoryRestocks.Add(restock);
                    db.SaveChanges();

                    if (!string.IsNullOrWhiteSpace(supplier.Email))
                    {
                        string subject = $"Restock Request for {inventoryItem.ItemName}";
                        string body = $"Dear {supplier.Name},\n\n" +
                                      $"We would like to request a restock of the following item:\n\n" +
                                      $"Item: {inventoryItem.ItemName}\n" +
                                      $"Quantity Requested: {model.Quantity}\n" +
                                      $"Batches Requested: {inventoryItem.Notes}\n" +
                                      $"Current Stock Level: {inventoryItem.Quantity}\n" +
                                      $"Requested on: {DateTime.Now:yyyy-MM-dd HH:mm}\n" +
                                      $"Requested by: {Session["FullName"]}\n\n" +
                                      $"Please confirm receipt of this request.\n\n" +
                                      $"Best regards,\nFarmTrack";

                        await SendEmailAsync(supplier.Email, subject, body);

                        restock.SupplierNotified = true;
                        db.Entry(restock).State = EntityState.Modified;
                        db.SaveChanges();
                    }

                    TempData["Message"] = $"Restock request created successfully for {inventoryItem.ItemName}. " +
                                          (supplier.Email != null ? "Supplier has been notified." : "Please contact supplier manually.");

                    return RedirectToAction("RestockList");
                }
                catch (Exception )
                {
                    TempData["Error"] = "Failed to create restock request. Please try again.";
                    // Log the exception for debugging
                    // Logger.LogError(ex, "Failed to create restock request");
                }
            }

            // Redisplay form if validation failed
            int displayInventoryId = id ?? model.InventoryId;

            if (id.HasValue)
            {
                var item = db.Inventories.FirstOrDefault(i => i.InventoryId == id.Value);
                ViewBag.SelectedItem = item;
                ViewBag.IsFromItemPage = true;
            }
            else
            {
                ViewBag.IsFromItemPage = false;
            }

            ViewBag.InventoryList = new SelectList(
                db.Inventories.Select(i => new { i.InventoryId, i.ItemName }).ToList(),
                "InventoryId", "ItemName", displayInventoryId);

            ViewBag.SupplierList = new SelectList(
                db.Suppliers.Select(s => new { s.SupplierId, s.Name }).ToList(),
                "SupplierId", "Name", model.SupplierId);

            return View(model);
        }


        // AJAX method to get default supplier for an inventory item
        public JsonResult GetItemSupplier(int inventoryId)
        {
            var userId = Convert.ToInt32(Session["UserId"]);
            var item = db.Inventories.FirstOrDefault(i => i.InventoryId == inventoryId &&
                                                          i.UserId == userId);

            if (item != null)
            {
                return Json(new { supplierId = item.SupplierId }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { supplierId = (int?)null }, JsonRequestBehavior.AllowGet);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CompleteRestock(int restockId)
        {
            var restock = db.InventoryRestocks.Include(r => r.Inventory).FirstOrDefault(r => r.InventoryRestockId == restockId);
            if (restock == null || restock.IsCompleted) return HttpNotFound();

            restock.Inventory.Quantity += restock.Quantity;
            restock.IsCompleted = true;
            restock.CompletedOn = DateTime.Now;

            db.SaveChanges();

            TempData["Message"] = "Restock marked as complete and inventory updated.";
            return RedirectToAction("InventoryList");
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

        // GET: Inventory/Use/5
        public ActionResult Use(int id)
        {
            var inventoryItem = db.Inventories.Find(id);
            if (inventoryItem == null)
            {
                return HttpNotFound();
            }
            return View(inventoryItem);
        }

        // POST: Inventory/Use/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Use(int id, int quantity)
        {
            var inventoryItem = db.Inventories.Find(id);
            if (inventoryItem != null && inventoryItem.Quantity >= quantity)
            {
                inventoryItem.Quantity -= quantity;
                db.SaveChanges();

                int userId = Convert.ToInt32(Session["UserId"]);
                db.LogActivity(userId, $"Used {quantity} of {inventoryItem.ItemName}");

                // Check for low stock and auto restock
                if (inventoryItem.Quantity <= inventoryItem.LowStockThreshold)
                {
                    bool alreadyRequested = db.InventoryRestocks.Any(r =>
                        r.InventoryId == inventoryItem.InventoryId &&
                        !r.IsCompleted &&
                        !r.Failed);

                    if (!alreadyRequested)
                    {
                        var supplier = db.Suppliers.FirstOrDefault(s => s.SupplierId == inventoryItem.SupplierId);
                        if (supplier != null)
                        {
                            var restock = new InventoryRestock
                            {
                                InventoryId = inventoryItem.InventoryId,
                                Quantity = inventoryItem.RestockThreshold > 0 ? inventoryItem.RestockThreshold : 10, // or any default
                                SupplierId = supplier.SupplierId,
                                RequestedOn = DateTime.Now,
                                RequestedById = userId,
                                SupplierNotified = false,
                                IsCompleted = false,
                                Failed = false
                            };

                            db.InventoryRestocks.Add(restock);
                            db.SaveChanges();

                            if (!string.IsNullOrWhiteSpace(supplier.Email))
                            {
                                string subject = $"Auto Restock Request for {inventoryItem.ItemName}";
                                string body = $"Dear {supplier.Name},\n\n" +
                                              $"This is an automatic restock request for the item:\n" +
                                              $"{inventoryItem.ItemName}\n" +
                                              $"Quantity: {restock.Quantity}\n" +
                                              $"Current stock: {inventoryItem.Quantity}\n" +
                                              $"Requested on: {DateTime.Now:yyyy-MM-dd HH:mm}\n\n" +
                                              "Please process this request at your earliest convenience.\n\nFarmTrack";

                                await SendEmailAsync(supplier.Email, subject, body);

                                restock.SupplierNotified = true;
                                db.Entry(restock).State = EntityState.Modified;
                                db.SaveChanges();
                            }

                            TempData["Message"] = $"Auto restock request created for {inventoryItem.ItemName}. Supplier notified.";
                        }
                        else
                        {
                            TempData["Message"] = $"Auto restock triggered but no supplier found for {inventoryItem.ItemName}.";
                        }
                    }
                    else
                    {
                        TempData["Message"] = $"Restock request for {inventoryItem.ItemName} is already pending.";
                    }
                }
            }
            else
            {
                ModelState.AddModelError("", "Not enough stock available.");
            }

            return RedirectToAction("RestockList");
        }


        // GET: Inventory/Edit/{id}
        public ActionResult Edit(int id)
        {
            string currentUserRole = Session["Role"]?.ToString();
            if (currentUserRole != "Owner")
            {
                return RedirectToAction("AdminDashboard", "Dashboard");
            }
            var inventoryItem = db.Inventories.Find(id);
            if (inventoryItem == null)
            {
                return HttpNotFound();
            }
            ViewBag.Suppliers = db.Suppliers.ToList();

            return View(inventoryItem);
        }

        // POST: Inventory/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Inventory model)
        {
            if (ModelState.IsValid)
            {
                var existingItem = db.Inventories.Find(model.InventoryId);
                if (existingItem == null)
                {
                    return HttpNotFound();
                }

                existingItem.ItemName = model.ItemName;
                existingItem.Quantity = model.Quantity;
                existingItem.Notes = model.Notes;
                existingItem.Category = model.Category;
                existingItem.DateAdded = DateTime.Now;
                existingItem.LowStockThreshold = model.LowStockThreshold;
                existingItem.RestockThreshold = model.RestockThreshold;
                existingItem.SupplierId = model.SupplierId;
                existingItem.NotifySupplier = model.NotifySupplier;

                db.SaveChanges();

                int userId = Convert.ToInt32(Session["UserId"]);
                db.LogActivity(userId, $"Edited inventory item: {model.ItemName}");

                return RedirectToAction("InventoryList");
            }
            ViewBag.Suppliers = db.Suppliers.ToList();

            return View(model);
        }

        // GET: Inventory/Delete/{id}
        public ActionResult Delete(int id)
        {
            string currentUserRole = Session["Role"]?.ToString();
            if (currentUserRole != "Owner")
            {
                return RedirectToAction("AdminDashboard", "Dashboard");
            }
            var inventoryItem = db.Inventories.Find(id);
            if (inventoryItem == null)
            {
                return HttpNotFound();
            }
            return View(inventoryItem);
        }

        // POST: Inventory/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var inventoryItem = db.Inventories.Find(id);
            if (inventoryItem == null)
            {
                return HttpNotFound();
            }

            db.Inventories.Remove(inventoryItem);
            db.SaveChanges();

            int userId = Convert.ToInt32(Session["UserId"]);
            db.LogActivity(userId, $"Deleted inventory item: {inventoryItem.ItemName}");

            return RedirectToAction("InventoryList");
        }

        public ActionResult FindByBarcode(string barcode)
        {
            if (string.IsNullOrEmpty(barcode))
                return RedirectToAction("Index");

            var item = db.Inventories.FirstOrDefault(i => i.Barcode == barcode);

            if (item == null)
            {
                TempData["Error"] = "Item not found for barcode: " + barcode;
                return RedirectToAction("Index");
            }

            return RedirectToAction("Details", new { id = item.InventoryId });
        }

        public ActionResult Scan()
        {
            return View();
        }
        // GET: Inventory/RestockList
        public ActionResult RestockList()
        {
            var restocks = db.InventoryRestocks.Include(r => r.Inventory).Include(r => r.RequestedBy).ToList();

            return View(restocks);
        }

        // GET: Inventory/RestockDetails/5
        public ActionResult RestockDetails(int id)
        {
            var restock = db.InventoryRestocks.Include(r => r.Inventory).Include(r => r.RequestedBy).FirstOrDefault(r => r.InventoryRestockId == id);
            if (restock == null) return HttpNotFound();
            return View(restock);
        }

        // POST: Inventory/MarkRestockComplete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkRestockComplete(int id)
        {
            var restock = db.InventoryRestocks.Include(r => r.Inventory).FirstOrDefault(r => r.InventoryRestockId == id);
            if (restock == null || restock.IsCompleted) return HttpNotFound();

            restock.IsCompleted = true;
            restock.CompletedOn = DateTime.Now;
            restock.Inventory.Quantity += restock.Quantity;

            db.SaveChanges();

            TempData["Message"] = "Restock marked as complete.";
            return RedirectToAction("RestockDetails", new { id });
        }

        // POST: Inventory/FailRestock/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult FailRestock(int id)
        {
            var restock = db.InventoryRestocks.FirstOrDefault(r => r.InventoryRestockId == id);
            if (restock == null || restock.IsCompleted) return HttpNotFound();

            restock.Failed = true;
            restock.CompletedOn = DateTime.Now;
            db.SaveChanges();

            TempData["Message"] = "Restock marked as failed.";
            return RedirectToAction("RestockDetails", new { id });
        }


    }

}