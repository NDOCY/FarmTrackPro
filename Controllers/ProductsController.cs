using FarmPro.Models;
using FarmTrack.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace FarmTrack.Controllers
{
    public class ProductsController : Controller
    {
        private FarmTrackContext db = new FarmTrackContext();

        // GET: Products
        public ActionResult Index()
        {
            var products = db.Products.Include(p => p.HarvestOutcome).Include(p => p.Inventory).Include(p => p.Livestock);
            return View(products.ToList());
        }

        

        // GET: Products/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);

            Product product = db.Products
                .Include(p => p.Reviews)
                .Include(p => p.Reviews.Select(r => r.User))
                .Include(p => p.HarvestOutcome)
                .Include(p => p.Inventory)
                .Include(p => p.Livestock)
                .FirstOrDefault(p => p.Id == id);

            if (product == null)
                return HttpNotFound();

            // Calculate rating summary
            var ratingSummary = CalculateRatingSummary(product.Id);
            ViewBag.RatingSummary = ratingSummary;

            // Get recent reviews
            var recentReviews = product.Reviews
                .Where(r => r.IsActive)
                .OrderByDescending(r => r.ReviewDate)
                .ToList();
            ViewBag.RecentReviews = recentReviews;

            // Check if current user can review
            bool canReview = false;
            if (Session["UserId"] != null)
            {
                int userId = (int)Session["UserId"];
                // User can review if they haven't reviewed before and have purchased the product
                canReview = !product.Reviews.Any(r => r.UserId == userId) && HasPurchasedProduct(userId, product.Id);
            }
            ViewBag.CanReview = canReview;

            return View(product);
        }

        // Helper method to calculate rating summary
        private ProductRatingViewModel CalculateRatingSummary(int productId)
        {
            var reviews = db.ProductReviews
                .Where(r => r.ProductId == productId && r.IsActive)
                .ToList();

            return new ProductRatingViewModel
            {
                ProductId = productId,
                AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0,
                TotalReviews = reviews.Count,
                FiveStar = reviews.Count(r => r.Rating == 5),
                FourStar = reviews.Count(r => r.Rating == 4),
                ThreeStar = reviews.Count(r => r.Rating == 3),
                TwoStar = reviews.Count(r => r.Rating == 2),
                OneStar = reviews.Count(r => r.Rating == 1)
            };
        }

        // Helper method to check if user has purchased the product
        private bool HasPurchasedProduct(int userId, int productId)
        {
            return db.Sales
                .Where(s => s.UserId == userId)
                .SelectMany(s => s.Items)
                .Any(i => i.ProductId == productId);
        }

        // POST: Submit review
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SubmitReview(int ProductId, int Rating, string ReviewText, bool IsVerifiedPurchase = false)
        {
            try
            {
                if (Session["UserId"] == null)
                {
                    return Json(new { success = false, message = "Please log in to submit a review." });
                }

                int userId = (int)Session["UserId"];

                // Check if user has already reviewed this product
                var existingReview = db.ProductReviews
                    .FirstOrDefault(r => r.ProductId == ProductId && r.UserId == userId);

                if (existingReview != null)
                {
                    return Json(new { success = false, message = "You have already reviewed this product." });
                }

                var review = new ProductReview
                {
                    ProductId = ProductId,
                    UserId = userId,
                    Rating = Rating,
                    ReviewText = ReviewText,
                    IsVerifiedPurchase = IsVerifiedPurchase,
                    ReviewDate = DateTime.Now,
                    IsActive = true
                };

                db.ProductReviews.Add(review);
                db.SaveChanges();

                // Log activity
                db.LogActivity(userId, $"Reviewed product #{ProductId} with {Rating} stars");

                return Json(new { success = true, message = "Review submitted successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error submitting review: " + ex.Message });
            }
        }

        // ADMIN: Order Management Detail View
        /*
         public ActionResult OrderManagement(int id)
         {
             // Check if user is admin
             if (Session["Role"]?.ToString() != "Admin" && Session["Role"]?.ToString() != "Owner")
             {
                 TempData["Error"] = "Access denied. Admin privileges required.";
                 return RedirectToAction("MyOrders");
             }

             var sale = db.Sales
                 .Include(s => s.User)
                 .Include(s => s.Items)
                 .Include(s => s.Items.Select(i => i.Product))
                 .Include(s => s.OrderStatusUpdates)
                 .Include(s => s.AssignedDriver)
                 .FirstOrDefault(s => s.SaleId == id);

             if (sale == null)
             {
                 TempData["Error"] = "Order not found.";
                 return RedirectToAction("SalesList");
             }

             // Get available drivers (admins and owners who are active)
             var availableDrivers = db.Users
                 .Where(u => (u.Role == "Admin" || u.Role == "Owner") && u.IsActive)
                 .Select(u => new
                 {
                     u.UserId,
                     u.FullName,
                     u.PhoneNumber,
                     u.VehicleType,
                     u.VehicleNumber
                 })
                 .ToList();

             ViewBag.AvailableDrivers = availableDrivers;

             return View(sale);
         }*/
        // Add these helper methods to your ProductsController class

        // CRITICAL FIX 1: Geocode customer address to get actual coordinates
        private (decimal? lat, decimal? lng) GeocodeAddress(string address)
        {
            if (string.IsNullOrEmpty(address)) return (null, null);

            try
            {
                // Using Google Geocoding API
                // You'll need to add your Google API key to Web.config
                string apiKey = System.Configuration.ConfigurationManager.AppSettings["GoogleMapsApiKey"];
                string url = $"https://maps.googleapis.com/maps/api/geocode/json?address={Uri.EscapeDataString(address)}&key={apiKey}";

                using (var client = new System.Net.WebClient())
                {
                    string response = client.DownloadString(url);
                    dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject(response);

                    if (json.status == "OK" && json.results.Count > 0)
                    {
                        decimal lat = json.results[0].geometry.location.lat;
                        decimal lng = json.results[0].geometry.location.lng;
                        return (lat, lng);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Geocoding error: {ex.Message}");
            }

            return (null, null);
        }

        // CRITICAL FIX 2: Update ProcessCheckout to geocode delivery address
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProcessCheckout(string CustomerName, string CustomerEmail, string CustomerPhone,
            string DeliveryAddress, string PaymentMethod, string DeliveryInstructions = "")
        {
            if (Session["UserId"] == null)
            {
                TempData["Error"] = "Please log in to complete your purchase.";
                return RedirectToAction("Login", "Account");
            }

            var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();

            if (!cart.Any())
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("Cart");
            }

            try
            {
                int userId = (int)Session["UserId"];

                // **FIX: Geocode the delivery address**
                var (destLat, destLng) = GeocodeAddress(DeliveryAddress);

                if (!destLat.HasValue || !destLng.HasValue)
                {
                    TempData["Error"] = "Could not locate the delivery address. Please provide a more specific address.";
                    return RedirectToAction("Checkout");
                }

                var trackingNumber = "FT" + DateTime.Now.ToString("yyyyMMddHHmmss");
                var estimatedDelivery = DateTime.Now.AddDays(new Random().Next(2, 4));

                var sale = new Sale
                {
                    SaleDate = DateTime.Now,
                    TotalAmount = (decimal)cart.Sum(item => item.Total),
                    UserId = userId,
                    Status = PaymentMethod == "Cash" ? "Confirmed" : "Pending",
                    TrackingNumber = trackingNumber,
                    EstimatedDelivery = estimatedDelivery,
                    CustomerName = CustomerName,
                    CustomerEmail = CustomerEmail,
                    CustomerPhone = CustomerPhone,
                    DeliveryAddress = DeliveryAddress,
                    PaymentMethod = PaymentMethod,
                    PaymentStatus = PaymentMethod == "Cash" ? "Pending" : "Simulated",

                    // **FIX: Store geocoded destination coordinates**
                    DestinationLatitude = destLat.Value,
                    DestinationLongitude = destLng.Value,

                    Items = new List<SaleItem>()
                };

                foreach (var cartItem in cart)
                {
                    var saleItem = new SaleItem
                    {
                        ProductId = cartItem.ProductId,
                        Quantity = cartItem.Quantity,
                        Price = (decimal)cartItem.PricePerUnit
                    };
                    sale.Items.Add(saleItem);

                    var product = db.Products.Find(cartItem.ProductId);
                    if (product != null)
                    {
                        product.Quantity = Math.Max(0, product.Quantity - cartItem.Quantity);
                    }
                }

                if (PaymentMethod != "Cash")
                {
                    System.Threading.Thread.Sleep(2000);
                    sale.PaymentStatus = "Completed";
                    sale.Status = "Confirmed";

                    db.OrderStatusUpdates.Add(new OrderStatusUpdate
                    {
                        SaleId = sale.SaleId,
                        Status = "Order Confirmed",
                        Notes = "Payment received. Preparing your order.",
                        UpdateTime = DateTime.Now
                    });
                }

                db.Sales.Add(sale);
                db.SaveChanges();

                Session["Cart"] = new List<CartItem>();

                TempData["Success"] = $"Order #{sale.SaleId} placed successfully! Tracking: {trackingNumber}";
                return RedirectToAction("OrderTracking", new { id = sale.SaleId });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Checkout error: {ex.Message}");
                TempData["Error"] = $"Checkout failed: {ex.Message}";
                return RedirectToAction("Checkout");
            }
        }

        // CRITICAL FIX 3: Fixed GetRealDeliveryData to use actual driver location
        public JsonResult GetRealDeliveryData(int saleId)
        {
            try
            {
                // Check user permission
                int? userId = Session["UserId"] as int?;
                if (!userId.HasValue)
                {
                    return Json(new { success = false, error = "Not logged in" }, JsonRequestBehavior.AllowGet);
                }

                var sale = db.Sales
                    .Include(s => s.AssignedDriver)
                    .FirstOrDefault(s => s.SaleId == saleId);

                if (sale == null)
                {
                    return Json(new { success = false, error = "Order not found" }, JsonRequestBehavior.AllowGet);
                }

                // Verify user can view this order (customer or assigned driver or admin)
                string userRole = Session["Role"]?.ToString();
                bool isCustomer = sale.UserId == userId.Value;
                bool isDriver = sale.AssignedDriverId == userId.Value;
                bool isAdmin = userRole == "Admin" || userRole == "Owner";

                if (!isCustomer && !isDriver && !isAdmin)
                {
                    return Json(new { success = false, error = "Access denied" }, JsonRequestBehavior.AllowGet);
                }

                // **FIX: Get REAL driver location from User table**
                decimal? currentLat = null;
                decimal? currentLng = null;

                if (sale.AssignedDriverId.HasValue)
                {
                    var driver = db.Users.Find(sale.AssignedDriverId.Value);
                    if (driver != null && driver.CurrentLatitude.HasValue && driver.CurrentLongitude.HasValue)
                    {
                        currentLat = driver.CurrentLatitude;
                        currentLng = driver.CurrentLongitude;

                        // Update sale's current location for tracking
                        sale.CurrentLatitude = currentLat;
                        sale.CurrentLongitude = currentLng;
                        sale.LastLocationUpdate = DateTime.Now;
                        db.SaveChanges();
                    }
                }

                // **FIX: Use stored destination coordinates from order**
                decimal destinationLat = sale.DestinationLatitude ?? -25.7479m; // Fallback
                decimal destinationLng = sale.DestinationLongitude ?? 28.2293m;

                // If driver hasn't started yet, show driver's starting position
                if (!currentLat.HasValue && sale.AssignedDriverId.HasValue)
                {
                    var driver = db.Users.Find(sale.AssignedDriverId.Value);
                    currentLat = driver?.CurrentLatitude ?? destinationLat;
                    currentLng = driver?.CurrentLongitude ?? destinationLng;
                }

                var deliveryData = new
                {
                    currentLat = currentLat ?? destinationLat,
                    currentLng = currentLng ?? destinationLng,
                    destinationLat = destinationLat,
                    destinationLng = destinationLng,
                    driverName = sale.DeliveryDriver ?? "Not assigned",
                    driverPhone = sale.DriverPhone ?? "",
                    vehicleType = sale.VehicleType ?? "Vehicle",
                    vehicleNumber = sale.VehicleNumber ?? "TBD",
                    status = sale.Status,
                    isActive = sale.IsActiveDelivery,
                    lastUpdate = sale.LastLocationUpdate?.ToString("g") ?? "No updates yet",
                    deliveryAddress = sale.DeliveryAddress,
                    hasDriver = sale.AssignedDriverId.HasValue,
                    driverIsOnline = sale.AssignedDriver?.IsOnlineAsDriver ?? false
                };

                return Json(new { success = true, deliveryData }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetRealDeliveryData error: {ex.Message}");
                return Json(new { success = false, error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // CRITICAL FIX 4: Update driver location properly
        [HttpPost]
        public JsonResult UpdateDriverLocation(decimal latitude, decimal longitude)
        {
            try
            {
                int userId = (int)Session["UserId"];
                var user = db.Users.Find(userId);

                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                // Update user's current location
                user.CurrentLatitude = latitude;
                user.CurrentLongitude = longitude;
                user.LastOnlineTime = DateTime.Now;
                user.IsOnlineAsDriver = true;

                // **FIX: Update ALL active deliveries assigned to this driver**
                var activeDeliveries = db.Sales
                    .Where(s => s.AssignedDriverId == userId && s.IsActiveDelivery)
                    .ToList();

                foreach (var delivery in activeDeliveries)
                {
                    delivery.CurrentLatitude = latitude;
                    delivery.CurrentLongitude = longitude;
                    delivery.LastLocationUpdate = DateTime.Now;

                    // Record in location history
                    db.DeliveryLocations.Add(new DeliveryLocation
                    {
                        SaleId = delivery.SaleId,
                        DriverUserId = userId,
                        Latitude = latitude,
                        Longitude = longitude,
                        Timestamp = DateTime.Now,
                        Sequence = db.DeliveryLocations.Count(dl => dl.SaleId == delivery.SaleId) + 1
                    });
                }

                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    updatedDeliveries = activeDeliveries.Count,
                    message = $"Location updated for {activeDeliveries.Count} deliveries"
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateDriverLocation error: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // CRITICAL FIX 5: Get driver's active deliveries correctly
        public JsonResult GetMyActiveDeliveries()
        {
            try
            {
                if (Session["UserId"] == null)
                {
                    return Json(new List<object>(), JsonRequestBehavior.AllowGet);
                }

                int userId = (int)Session["UserId"];

                var deliveries = db.Sales
                    .Where(s => s.AssignedDriverId == userId &&
                           (s.Status == "Assigned to Driver" || s.Status == "Out for Delivery"))
                    .Select(s => new
                    {
                        saleId = s.SaleId,
                        customerName = s.CustomerName,
                        deliveryAddress = s.DeliveryAddress,
                        status = s.Status,
                        isActive = s.IsActiveDelivery,
                        destinationLat = s.DestinationLatitude,
                        destinationLng = s.DestinationLongitude
                    })
                    .ToList();

                return Json(deliveries, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetMyActiveDeliveries error: {ex.Message}");
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }
        }

        // CRITICAL FIX 6: Start delivery properly
        [HttpPost]
        public JsonResult StartDelivery(int saleId)
        {
            try
            {
                int userId = (int)Session["UserId"];
                var sale = db.Sales.Find(saleId);

                if (sale == null)
                {
                    return Json(new { success = false, message = "Order not found" });
                }

                if (sale.AssignedDriverId != userId)
                {
                    return Json(new { success = false, message = "This delivery is not assigned to you" });
                }

                // Get driver's current location
                var driver = db.Users.Find(userId);
                if (driver.CurrentLatitude.HasValue && driver.CurrentLongitude.HasValue)
                {
                    sale.CurrentLatitude = driver.CurrentLatitude;
                    sale.CurrentLongitude = driver.CurrentLongitude;
                }

                sale.Status = "Out for Delivery";
                sale.IsActiveDelivery = true;
                sale.LastLocationUpdate = DateTime.Now;

                // Add status update
                db.OrderStatusUpdates.Add(new OrderStatusUpdate
                {
                    SaleId = saleId,
                    Status = "Out for Delivery",
                    Notes = $"Delivery started by {driver.FullName}",
                    UpdateTime = DateTime.Now
                });

                db.SaveChanges();

                // Log activity
                db.LogActivity(userId, $"Started delivery for order #{saleId}");

                return Json(new { success = true, message = "Delivery started successfully" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"StartDelivery error: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }



        public ActionResult OrderManagement(int id)
        {
            // Check if user is admin
            if (Session["Role"]?.ToString() != "Admin" && Session["Role"]?.ToString() != "Owner")
            {
                TempData["Error"] = "Access denied. Admin privileges required.";
                return RedirectToAction("MyOrders");
            }

            var sale = db.Sales
                .Include(s => s.User)
                .Include(s => s.Items)
                .Include(s => s.Items.Select(i => i.Product))
                .Include(s => s.OrderStatusUpdates)
                .Include(s => s.AssignedDriver)
                .FirstOrDefault(s => s.SaleId == id);

            if (sale == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction("SalesList");
            }

            // SIMPLE FIX: Get list of User objects directly (not anonymous objects)
            var availableDrivers = db.Users
                .Where(u => u.Role == "Admin" || u.Role == "Owner")
                .ToList();

            ViewBag.AvailableDrivers = availableDrivers;
            ViewBag.CurrentUserId = Session["UserId"];

            return View(sale);
        }

        // Assign driver to delivery
        [HttpPost]
        public JsonResult AssignDriverToDelivery(int saleId, int driverUserId, string driverPhone)
        {
            try
            {
                var sale = db.Sales.Find(saleId);
                var driver = db.Users.Find(driverUserId);

                if (sale == null || driver == null)
                {
                    return Json(new { success = false, message = "Sale or driver not found." });
                }

                // Assign driver
                sale.AssignedDriverId = driverUserId;
                sale.DeliveryDriver = driver.FullName;
                sale.DriverPhone = driver.PhoneNumber;
                sale.VehicleType = driver.VehicleType;
                sale.VehicleNumber = driver.VehicleNumber;
                sale.Status = "Assigned to Driver";

                // Add status update
                db.OrderStatusUpdates.Add(new OrderStatusUpdate
                {
                    SaleId = saleId,
                    Status = "Assigned to Driver",
                    Notes = $"Driver assigned: {driver.FullName}",
                    UpdateTime = DateTime.Now
                });

                db.SaveChanges();

                // Log the assignment
                db.LogActivity((int)Session["UserId"],
                    $"Assigned driver {driver.FullName} to order #{saleId}");

                return Json(new { success = true, message = $"Driver {driver.FullName} assigned successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error assigning driver: " + ex.Message });
            }
        }

        // Unassign driver from delivery
        [HttpPost]
        public JsonResult UnassignDriver(int saleId)
        {
            try
            {
                var sale = db.Sales.Find(saleId);

                if (sale == null)
                {
                    return Json(new { success = false, message = "Sale not found." });
                }

                var oldDriverName = sale.DeliveryDriver;

                // Unassign driver
                sale.AssignedDriverId = null;
                sale.DeliveryDriver = null;
                sale.DriverPhone = null;
                sale.VehicleType = null;
                sale.VehicleNumber = null;
                sale.IsActiveDelivery = false;
                sale.Status = "Confirmed"; // Revert to confirmed status

                // Add status update
                db.OrderStatusUpdates.Add(new OrderStatusUpdate
                {
                    SaleId = saleId,
                    Status = "Confirmed",
                    Notes = $"Driver {oldDriverName} unassigned from delivery",
                    UpdateTime = DateTime.Now
                });

                db.SaveChanges();

                db.LogActivity((int)Session["UserId"],
                    $"Unassigned driver from order #{saleId}");

                return Json(new { success = true, message = "Driver unassigned successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error unassigning driver: " + ex.Message });
            }
        }

        // GET: Products/Create
        public ActionResult Create()
        {
            ViewBag.HarvestOutcomeId = new SelectList(db.HarvestOutcomes, "Id", "QualityGrade");
            ViewBag.InventoryId = new SelectList(db.Inventories, "InventoryId", "ItemName");
            ViewBag.LivestockId = new SelectList(db.Livestocks, "LivestockId", "Type");
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Name,ProductType,Category,Unit,Quantity,PricePerUnit,HarvestOutcomeId,LivestockId,InventoryId,CreatedAt")] Product product)
        {
            if (ModelState.IsValid)
            {
                db.Products.Add(product);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.HarvestOutcomeId = new SelectList(db.HarvestOutcomes, "Id", "QualityGrade", product.HarvestOutcomeId);
            ViewBag.InventoryId = new SelectList(db.Inventories, "InventoryId", "ItemName", product.InventoryId);
            ViewBag.LivestockId = new SelectList(db.Livestocks, "LivestockId", "Type", product.LivestockId);
            return View(product);
        }

        // GET: Products/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);

            Product product = db.Products.Find(id);
            if (product == null)
                return HttpNotFound();

            ViewBag.HarvestOutcomeId = new SelectList(db.HarvestOutcomes, "Id", "QualityGrade", product.HarvestOutcomeId);
            ViewBag.InventoryId = new SelectList(db.Inventories, "InventoryId", "ItemName", product.InventoryId);
            ViewBag.LivestockId = new SelectList(db.Livestocks, "LivestockId", "Type", product.LivestockId);
            return View(product);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Name,ProductType,Category,Unit,Quantity,PricePerUnit,Description,ImageUrl,IsAvailable,IsFeatured,MinimumOrder,HarvestOutcomeId,LivestockId,InventoryId,CreatedAt")] Product product)
        {
            if (ModelState.IsValid)
            {
                // Update the LastUpdated timestamp
                product.LastUpdated = DateTime.Now;

                db.Entry(product).State = EntityState.Modified;
                db.SaveChanges();

                TempData["Success"] = "Product updated successfully!";
                return RedirectToAction("Index");
            }

            ViewBag.HarvestOutcomeId = new SelectList(db.HarvestOutcomes, "Id", "QualityGrade", product.HarvestOutcomeId);
            ViewBag.InventoryId = new SelectList(db.Inventories, "InventoryId", "ItemName", product.InventoryId);
            ViewBag.LivestockId = new SelectList(db.Livestocks, "LivestockId", "Type", product.LivestockId);

            TempData["Error"] = "Please correct the errors below.";
            return View(product);
        }

        // GET: Products/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);

            Product product = db.Products.Find(id);
            if (product == null)
                return HttpNotFound();

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Product product = db.Products.Find(id);
            db.Products.Remove(product);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // STORE FUNCTIONALITY
        public ActionResult Store(string typeFilter, string categoryFilter)
        {
            var products = db.Products.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(typeFilter))
                products = products.Where(p => p.ProductType == typeFilter);

            if (!string.IsNullOrEmpty(categoryFilter))
                products = products.Where(p => p.Category == categoryFilter);

            // Group by Category
            var grouped = products
                .GroupBy(p => p.Category)
                .Select(g => new StoreCategoryViewModel
                {
                    Category = g.Key,
                    Items = g.OrderBy(p => p.Name).ToList()
                })
                .ToList();

            var model = new StoreViewModel
            {
                TypeFilter = typeFilter,
                CategoryFilter = categoryFilter,
                Categories = grouped
            };
            return View(model);
        }

        // CART FUNCTIONALITY
        public ActionResult Cart()
        {
            var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();
            return View(cart);
        }

        [HttpPost]
        public ActionResult AddToCart(int productId, int quantity = 1)
        {
            var product = db.Products.Find(productId);
            if (product == null || product.PricePerUnit == null)
                return HttpNotFound();

            var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();

            var existingItem = cart.FirstOrDefault(c => c.ProductId == productId);
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    Name = product.Name,
                    Unit = product.Unit,
                    PricePerUnit = product.PricePerUnit ?? 0,
                    Quantity = quantity
                });
            }

            Session["Cart"] = cart;
            TempData["Success"] = "Item added to cart.";
            return RedirectToAction("Store");
        }

        public ActionResult RemoveFromCart(int productId)
        {
            var cart = Session["Cart"] as List<CartItem>;
            if (cart != null)
            {
                var item = cart.FirstOrDefault(c => c.ProductId == productId);
                if (item != null)
                    cart.Remove(item);
                Session["Cart"] = cart;
            }

            return RedirectToAction("Cart");
        }

        public ActionResult ClearCart()
        {
            Session["Cart"] = new List<CartItem>();
            return RedirectToAction("Cart");
        }

        
        // CHECKOUT FUNCTIONALITY - GET (pre-populate user data)
        public ActionResult Checkout()
        {
            // Check if user is logged in
            if (Session["UserId"] == null)
            {
                TempData["Error"] = "Please log in to checkout.";
                return RedirectToAction("Login", "Account");
            }

            var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();

            if (!cart.Any())
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("Cart");
            }

            // Get user data to pre-populate the form
            int userId = (int)Session["UserId"];
            var user = db.Users.Find(userId);

            if (user != null)
            {
                var checkoutModel = new CheckoutViewModel
                {
                    CartItems = cart,
                    CustomerName = user.FullName,
                    CustomerEmail = user.Email,
                    CustomerPhone = user.PhoneNumber,
                    DeliveryAddress = user.Address
                };

                return View(checkoutModel);
            }

            return View(new CheckoutViewModel { CartItems = cart });
        }

        // CHECKOUT FUNCTIONALITY - POST (process the actual checkout
        /*
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProcessCheckout(string CustomerName, string CustomerEmail, string CustomerPhone, string DeliveryAddress, string PaymentMethod, string DeliveryInstructions = "")
        {
            // Check if user is logged in
            if (Session["UserId"] == null)
            {
                TempData["Error"] = "Please log in to complete your purchase.";
                return RedirectToAction("Login", "Account");
            }

            var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();

            if (!cart.Any())
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("Cart");
            }

            try
            {
                int userId = (int)Session["UserId"];

                // Generate tracking number
                var trackingNumber = "FT" + DateTime.Now.ToString("yyyyMMddHHmmss");
                var estimatedDelivery = DateTime.Now.AddDays(new Random().Next(2, 4));

                // Create new Sale with UserId
                var sale = new Sale
                {
                    SaleDate = DateTime.Now,
                    TotalAmount = (decimal)cart.Sum(item => item.Total),
                    UserId = userId, // Link to logged-in user
                    Status = PaymentMethod == "Cash" ? "Confirmed" : "Pending",
                    TrackingNumber = trackingNumber,
                    EstimatedDelivery = estimatedDelivery,
                    CustomerName = CustomerName,
                    CustomerEmail = CustomerEmail,
                    CustomerPhone = CustomerPhone,
                    DeliveryAddress = DeliveryAddress,
                    PaymentMethod = PaymentMethod,
                    PaymentStatus = PaymentMethod == "Cash" ? "Pending" : "Simulated",
                    Items = new List<SaleItem>()
                };

                // Create SaleItems from cart and update stock
                foreach (var cartItem in cart)
                {
                    var saleItem = new SaleItem
                    {
                        ProductId = cartItem.ProductId,
                        Quantity = cartItem.Quantity,
                        Price = (decimal)cartItem.PricePerUnit
                    };
                    sale.Items.Add(saleItem);

                    // Update product stock
                    var product = db.Products.Find(cartItem.ProductId);
                    if (product != null)
                    {
                        product.Quantity = Math.Max(0, product.Quantity - cartItem.Quantity);
                    }
                }

                // Simulate payment processing for non-cash methods
                if (PaymentMethod != "Cash")
                {
                    System.Threading.Thread.Sleep(2000);
                    sale.PaymentStatus = "Completed";
                    sale.Status = "Confirmed";

                    // Create initial status update
                    db.OrderStatusUpdates.Add(new OrderStatusUpdate
                    {
                        SaleId = sale.SaleId,
                        Status = "Order Confirmed",
                        Notes = "Payment received. Preparing your order.",
                        UpdateTime = DateTime.Now
                    });
                }

                // Save to database
                db.Sales.Add(sale);
                db.SaveChanges();

                // Clear cart
                Session["Cart"] = new List<CartItem>();

                TempData["Success"] = $"Order #{sale.SaleId} placed successfully! Tracking: {trackingNumber}";
                return RedirectToAction("OrderTracking", new { id = sale.SaleId });
            }
            catch (Exception ex)
            {
                // Log the actual error for debugging
                System.Diagnostics.Debug.WriteLine($"Checkout error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                TempData["Error"] = $"Checkout failed. Please try again. Error: {ex.Message}";
                return RedirectToAction("Checkout");
            }
        }*/

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }


        // Simple orders list for customers
        // My Orders page for customers
        // My Orders page for customers - now filtered by logged-in user
        public ActionResult MyOrders(string statusFilter = "")
        {
            // Check if user is logged in
            if (Session["UserId"] == null)
            {
                TempData["Error"] = "Please log in to view your orders.";
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserId"];

            // Filter orders by the logged-in user's ID
            var ordersQuery = db.Sales
                .Where(s => s.UserId == userId) // Only show orders for this user
                .Include(s => s.Items)
                .Include(s => s.Items.Select(i => i.Product))
                .Include(s => s.OrderStatusUpdates)
                .OrderByDescending(s => s.SaleDate)
                .AsQueryable();

            // Apply status filter if provided
            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All")
            {
                ordersQuery = ordersQuery.Where(s => s.Status == statusFilter);
            }

            var orders = ordersQuery.ToList();

            var viewModel = new MyOrdersViewModel
            {
                Orders = orders.Select(s => new OrderSummary
                {
                    SaleId = s.SaleId,
                    SaleDate = s.SaleDate,
                    TotalAmount = s.TotalAmount,
                    Status = s.Status,
                    TrackingNumber = s.TrackingNumber,
                    EstimatedDelivery = s.EstimatedDelivery,
                    CustomerName = s.CustomerName,
                    ItemCount = s.Items.Sum(i => i.Quantity),
                    Items = s.Items.Select(i => new OrderItem
                    {
                        ProductName = i.Product?.Name ?? "Product",
                        Quantity = i.Quantity,
                        Price = i.Price
                    }).ToList()
                }).ToList(),
                FilterStatus = statusFilter
            };

            return View(viewModel);
        }

       

        public ActionResult OrderTracking(int id)
        {
            // Check if user is logged in
            if (Session["UserId"] == null)
            {
                TempData["Error"] = "Please log in to view order details.";
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserId"];

            var sale = db.Sales
                .Where(s => s.SaleId == id && s.UserId == userId) // Only allow access to user's own orders
                .Include(s => s.Items)
                .Include(s => s.Items.Select(i => i.Product))
                .FirstOrDefault();

            if (sale == null)
            {
                TempData["Error"] = "Order not found or you don't have permission to view this order.";
                return RedirectToAction("MyOrders");
            }

            return View(sale);
        }

        public ActionResult OrderTrackingLive(int id)
        {
            // Check if user is logged in
            if (Session["UserId"] == null)
            {
                return Json(new { error = "Please log in" }, JsonRequestBehavior.AllowGet);
            }

            int userId = (int)Session["UserId"];

            var sale = db.Sales
                .Where(s => s.SaleId == id && s.UserId == userId) // Only allow access to user's own orders
                .Include(s => s.Items)
                .Include(s => s.Items.Select(i => i.Product))
                .Include(s => s.OrderStatusUpdates)
                .FirstOrDefault();

            if (sale == null)
            {
                return Json(new { error = "Order not found" }, JsonRequestBehavior.AllowGet);
            }

            return View(sale);
        }

        // AJAX endpoint for live updates
        // AJAX endpoint for live updates - with user verification
        public JsonResult GetOrderStatus(int saleId)
        {
            // Check if user is logged in
            if (Session["UserId"] == null)
            {
                return Json(new { error = "Please log in" }, JsonRequestBehavior.AllowGet);
            }

            int userId = (int)Session["UserId"];

            var sale = db.Sales
                .Where(s => s.SaleId == saleId && s.UserId == userId) // Verify user ownership
                .Include(s => s.OrderStatusUpdates)
                .FirstOrDefault();

            if (sale == null)
            {
                return Json(new { error = "Order not found" }, JsonRequestBehavior.AllowGet);
            }

            var updates = sale.OrderStatusUpdates
                .OrderByDescending(u => u.UpdateTime)
                .Select(u => new
                {
                    status = u.Status,
                    notes = u.Notes,
                    time = u.UpdateTime.ToString("g"),
                    isCurrent = u.UpdateTime == sale.OrderStatusUpdates.Max(x => x.UpdateTime)
                });

            return Json(new
            {
                currentStatus = sale.Status,
                updates = updates,
                estimatedDelivery = sale.EstimatedDelivery?.ToString("ddd, MMM dd 'at' HH:mm"),
                trackingNumber = sale.TrackingNumber
            }, JsonRequestBehavior.AllowGet);
        }

        // AJAX method to get order count for the badge - now user-specific
        public JsonResult GetOrderCount()
        {
            try
            {
                // Check if user is logged in
                if (Session["UserId"] == null)
                {
                    return Json(new { count = 0 }, JsonRequestBehavior.AllowGet);
                }

                int userId = (int)Session["UserId"];

                // Count only orders for the logged-in user
                var count = db.Sales.Count(s => s.UserId == userId);

                return Json(new { count = count }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // Log error
                return Json(new { count = 0 }, JsonRequestBehavior.AllowGet);
            }
        }
        // ADMIN: Sales List with management
        public ActionResult SalesList(string status, string paymentStatus, DateTime? startDate, DateTime? endDate, string searchTerm, string driverStatus)
        {
            // Check if user is admin
            if (Session["Role"]?.ToString() != "Admin" && Session["Role"]?.ToString() != "Owner")
            {
                TempData["Error"] = "Access denied. Admin privileges required.";
                return RedirectToAction("MyOrders");
            }

            var salesQuery = db.Sales
                .Include(s => s.User)
                .Include(s => s.Items)
                .Include(s => s.OrderStatusUpdates)
                .Include(s => s.AssignedDriver)
                .OrderByDescending(s => s.SaleDate)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                salesQuery = salesQuery.Where(s => s.Status == status);
            }

            if (!string.IsNullOrEmpty(paymentStatus) && paymentStatus != "All")
            {
                salesQuery = salesQuery.Where(s => s.PaymentStatus == paymentStatus);
            }

            // Apply driver status filter
            if (!string.IsNullOrEmpty(driverStatus))
            {
                switch (driverStatus)
                {
                    case "assigned":
                        salesQuery = salesQuery.Where(s => s.AssignedDriverId != null);
                        break;
                    case "unassigned":
                        salesQuery = salesQuery.Where(s => s.AssignedDriverId == null);
                        break;
                    case "active":
                        salesQuery = salesQuery.Where(s => s.IsActiveDelivery);
                        break;
                }
            }

            if (startDate.HasValue)
            {
                salesQuery = salesQuery.Where(s => s.SaleDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                salesQuery = salesQuery.Where(s => s.SaleDate <= endDate.Value.AddDays(1));
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                salesQuery = salesQuery.Where(s =>
                    s.CustomerName.Contains(searchTerm) ||
                    s.CustomerEmail.Contains(searchTerm) ||
                    s.TrackingNumber.Contains(searchTerm) ||
                    s.SaleId.ToString().Contains(searchTerm));
            }

            var sales = salesQuery.ToList();

            // FIXED: Safe stats calculation with null handling
            var stats = new SalesStats
            {
                TotalOrders = db.Sales.Count(),
                TotalRevenue = db.Sales.Any() ? db.Sales.Sum(s => s.TotalAmount) : 0, // Handle empty database
                PendingOrders = db.Sales.Count(s => s.Status == "Pending"),
                DeliveredOrders = db.Sales.Count(s => s.Status == "Delivered")
            };

            var viewModel = new SalesListViewModel
            {
                Sales = sales.Select(s => new SaleSummary
                {
                    SaleId = s.SaleId,
                    SaleDate = s.SaleDate,
                    TotalAmount = s.TotalAmount,
                    Status = s.Status ?? "Unknown",
                    CustomerName = s.CustomerName ?? "Unknown Customer",
                    CustomerEmail = s.CustomerEmail ?? "No email",
                    CustomerPhone = s.CustomerPhone ?? "No phone",
                    PaymentMethod = s.PaymentMethod ?? "Unknown",
                    PaymentStatus = s.PaymentStatus ?? "Pending",
                    TrackingNumber = s.TrackingNumber ?? "No tracking",
                    EstimatedDelivery = s.EstimatedDelivery,
                    ItemCount = s.Items != null && s.Items.Any() ? s.Items.Sum(i => i.Quantity) : 0, // Safe item count
                    UserName = s.User?.FullName ?? "N/A",
                    UserId = s.UserId,

                    // Delivery tracking properties
                    DeliveryDriver = s.DeliveryDriver,
                    DriverPhone = s.DriverPhone,
                    VehicleType = s.VehicleType,
                    VehicleNumber = s.VehicleNumber,
                    LastLocationUpdate = s.LastLocationUpdate,
                    IsActiveDelivery = s.IsActiveDelivery,
                    AssignedDriverId = s.AssignedDriverId
                }).ToList(),
                Filter = new SalesFilter
                {
                    Status = status,
                    PaymentStatus = paymentStatus,
                    StartDate = startDate,
                    EndDate = endDate,
                    SearchTerm = searchTerm
                },
                Stats = stats
            };

            // Pass driverStatus to view
            ViewBag.DriverStatus = driverStatus;

            return View(viewModel);
        }
        /*
        // ADMIN: Order 
        public ActionResult OrderManagement(int id)
        {
            // Check if user is admin
            if (Session["Role"]?.ToString() != "Admin" && Session["Role"]?.ToString() != "Owner")
            {
                TempData["Error"] = "Access denied. Admin privileges required.";
                return RedirectToAction("MyOrders");
            }

            var sale = db.Sales
                .Include(s => s.User)
                .Include(s => s.Items)
                .Include(s => s.Items.Select(i => i.Product))
                .Include(s => s.OrderStatusUpdates)
                .FirstOrDefault(s => s.SaleId == id);

            if (sale == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction("SalesList");
            }

            return View(sale);
        }*/

        // ADMIN: Update Order Status
        [HttpPost]
        public ActionResult UpdateOrderStatus(int saleId, string status, string notes = "")
        {
            // Check if user is admin
            if (Session["Role"]?.ToString() != "Admin" && Session["Role"]?.ToString() != "Owner")
            {
                return Json(new { success = false, message = "Access denied" });
            }

            try
            {
                var sale = db.Sales.Find(saleId);
                if (sale == null)
                {
                    return Json(new { success = false, message = "Order not found" });
                }

                var oldStatus = sale.Status;
                sale.Status = status;

                // Add status update history
                db.OrderStatusUpdates.Add(new OrderStatusUpdate
                {
                    SaleId = saleId,
                    Status = status,
                    Notes = notes,
                    UpdateTime = DateTime.Now
                });

                db.SaveChanges();

                // Log the admin action
                db.LogActivity((int)Session["UserId"],
                    $"Admin updated order #{saleId} status from {oldStatus} to {status}");

                return Json(new { success = true, message = $"Status updated to {status}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating status" });
            }
        }

        // ADMIN: Bulk status update
        [HttpPost]
        public ActionResult BulkUpdateStatus(int[] saleIds, string status)
        {
            // Check if user is admin
            if (Session["Role"]?.ToString() != "Admin" && Session["Role"]?.ToString() != "Owner")
            {
                return Json(new { success = false, message = "Access denied" });
            }

            try
            {
                var sales = db.Sales.Where(s => saleIds.Contains(s.SaleId)).ToList();

                foreach (var sale in sales)
                {
                    sale.Status = status;

                    db.OrderStatusUpdates.Add(new OrderStatusUpdate
                    {
                        SaleId = sale.SaleId,
                        Status = status,
                        Notes = "Bulk status update by admin",
                        UpdateTime = DateTime.Now
                    });
                }

                db.SaveChanges();

                db.LogActivity((int)Session["UserId"],
                    $"Admin bulk updated {sales.Count} orders to {status}");

                return Json(new { success = true, message = $"Updated {sales.Count} orders to {status}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating orders" });
            }
        }

        // ADMIN: Export sales to CSV
        public ActionResult ExportSales(string status, DateTime? startDate, DateTime? endDate)
        {
            // Check if user is admin
            if (Session["Role"]?.ToString() != "Admin" && Session["Role"]?.ToString() != "Owner")
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("SalesList");
            }

            var salesQuery = db.Sales
                .Include(s => s.Items)
                .Include(s => s.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                salesQuery = salesQuery.Where(s => s.Status == status);
            }

            if (startDate.HasValue)
            {
                salesQuery = salesQuery.Where(s => s.SaleDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                salesQuery = salesQuery.Where(s => s.SaleDate <= endDate.Value.AddDays(1));
            }

            var sales = salesQuery.ToList();

            // Create CSV content
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("OrderID,Date,Customer,Email,Phone,Status,Payment Method,Total Amount,Items,User");

            foreach (var sale in sales)
            {
                csv.AppendLine($"\"{sale.SaleId}\",\"{sale.SaleDate:yyyy-MM-dd HH:mm}\",\"{sale.CustomerName}\",\"{sale.CustomerEmail}\",\"{sale.CustomerPhone}\",\"{sale.Status}\",\"{sale.PaymentMethod}\",\"{sale.TotalAmount}\",\"{sale.Items.Sum(i => i.Quantity)}\",\"{sale.User?.FullName ?? "N/A"}\"");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"sales_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        // Delivery Dashboard for Admins
        
        public ActionResult DeliveryDashboard()
        {
            var deliveries = db.Sales
                .Where(s => s.Status == "Confirmed" || s.Status == "Out for Delivery")
                .Include(s => s.AssignedDriver)
                .OrderBy(s => s.SaleDate)
                .ToList();

            return View(deliveries);
        }

        // Go online as delivery driver
        [HttpPost]
        public JsonResult GoOnlineAsDriver(decimal latitude, decimal longitude)
        {
            try
            {
                int userId = (int)Session["UserId"];
                var user = db.Users.Find(userId);

                // Update user's current location
                user.CurrentLatitude = latitude;
                user.CurrentLongitude = longitude;
                user.IsOnlineAsDriver = true;
                user.LastOnlineTime = DateTime.Now;

                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Assign delivery to current admin
        [HttpPost]
        public JsonResult AssignDeliveryToMe(int saleId)
        {
            try
            {
                int userId = (int)Session["UserId"];
                var sale = db.Sales.Find(saleId);
                var user = db.Users.Find(userId);

                sale.AssignedDriverId = userId;
                sale.DeliveryDriver = user.FullName;
                sale.DriverPhone = user.PhoneNumber;
                sale.VehicleType = "Car"; // Could be from user profile
                sale.VehicleNumber = "GP 123-456"; // Could be from user profile
                sale.Status = "Assigned to Driver";

                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

       /* // Start delivery - begin tracking
        [HttpPost]
        public JsonResult StartDelivery(int saleId)
        {
            try
            {
                int userId = (int)Session["UserId"];
                var sale = db.Sales.Find(saleId);

                if (sale.AssignedDriverId != userId)
                {
                    return Json(new { success = false, message = "Delivery not assigned to you" });
                }

                sale.Status = "Out for Delivery";
                sale.IsActiveDelivery = true;

                // Use admin's current location as starting point
                var driver = db.Users.Find(userId);
                if (driver.CurrentLatitude.HasValue)
                {
                    sale.CurrentLatitude = driver.CurrentLatitude;
                    sale.CurrentLongitude = driver.CurrentLongitude;
                    sale.LastLocationUpdate = DateTime.Now;
                }

                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }*/
        /*
        // Get REAL delivery data for tracking
        public JsonResult GetRealDeliveryData(int saleId)
        {
            try
            {
                var sale = db.Sales
                    .Include(s => s.AssignedDriver)
                    .FirstOrDefault(s => s.SaleId == saleId);

                if (sale == null) return Json(new { success = false });

                // Get the assigned admin/driver's current location
                decimal currentLat = sale.CurrentLatitude ?? -25.7479m;
                decimal currentLng = sale.CurrentLongitude ?? 28.2293m;

                if (sale.AssignedDriverId.HasValue && sale.IsActiveDelivery)
                {
                    var driver = db.Users.Find(sale.AssignedDriverId);
                    if (driver.CurrentLatitude.HasValue)
                    {
                        currentLat = driver.CurrentLatitude.Value;
                        currentLng = driver.CurrentLongitude.Value;
                    }
                }

                var deliveryData = new
                {
                    currentLat = currentLat,
                    currentLng = currentLng,
                    destinationLat = -25.8600m, // Would geocode the address
                    destinationLng = 28.1890m,
                    driverName = sale.DeliveryDriver,
                    driverPhone = sale.DriverPhone,
                    vehicleType = sale.VehicleType,
                    vehicleNumber = sale.VehicleNumber,
                    status = sale.Status,
                    isActive = sale.IsActiveDelivery,
                    lastUpdate = sale.LastLocationUpdate?.ToString("g")
                };

                return Json(new { success = true, deliveryData }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }*/

        // Go offline as driver
        [HttpPost]
        public JsonResult GoOfflineAsDriver()
        {
            try
            {
                int userId = (int)Session["UserId"];
                var user = db.Users.Find(userId);

                user.IsOnlineAsDriver = false;
                user.CurrentLatitude = null;
                user.CurrentLongitude = null;

                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Check if driver is online
        public JsonResult IsDriverOnline()
        {
            try
            {
                int userId = (int)Session["UserId"];
                var user = db.Users.Find(userId);

                return Json(user?.IsOnlineAsDriver ?? false, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }
        }
        /*
        // Get current user's active deliveries
        public JsonResult GetMyActiveDeliveries()
        {
            try
            {
                int userId = (int)Session["UserId"];

                var deliveries = db.Sales
                    .Where(s => s.AssignedDriverId == userId && s.IsActiveDelivery)
                    .Select(s => new
                    {
                        saleId = s.SaleId,
                        customerName = s.CustomerName,
                        deliveryAddress = s.DeliveryAddress,
                        status = s.Status
                    })
                    .ToList();

                return Json(deliveries, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }
        }*/

        // Complete delivery
        [HttpPost]
        public JsonResult CompleteDelivery(int saleId)
        {
            try
            {
                int userId = (int)Session["UserId"];
                var sale = db.Sales.Find(saleId);

                if (sale.AssignedDriverId != userId)
                {
                    return Json(new { success = false, message = "Delivery not assigned to you" });
                }

                sale.Status = "Delivered";
                sale.IsActiveDelivery = false;

                db.OrderStatusUpdates.Add(new OrderStatusUpdate
                {
                    SaleId = saleId,
                    Status = "Delivered",
                    Notes = "Delivery completed successfully",
                    UpdateTime = DateTime.Now
                });

                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Live Tracking Map View
        public ActionResult LiveTrackingMap(int id)
        {
            // Check if user is logged in
            if (Session["UserId"] == null)
            {
                TempData["Error"] = "Please log in to view tracking.";
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserId"];

            var sale = db.Sales
                .Include(s => s.AssignedDriver)
                .FirstOrDefault(s => s.SaleId == id && s.UserId == userId); // ONLY show user's own orders

            if (sale == null)
            {
                TempData["Error"] = "Order not found or you don't have permission to view this order.";
                return RedirectToAction("MyOrders");
            }

            // NO PERMISSION CHECKS NEEDED - customer can only see their own orders
            return View(sale);
        }

       /* [HttpPost]
        public JsonResult UpdateDriverLocation(decimal latitude, decimal longitude)
        {
            try
            {
                int userId = (int)Session["UserId"];
                var user = db.Users.Find(userId);

                user.CurrentLatitude = latitude;
                user.CurrentLongitude = longitude;
                user.LastOnlineTime = DateTime.Now;

                // Also update any active deliveries
                var activeDeliveries = db.Sales
                    .Where(s => s.AssignedDriverId == userId && s.IsActiveDelivery)
                    .ToList();

                foreach (var delivery in activeDeliveries)
                {
                    delivery.CurrentLatitude = latitude;
                    delivery.CurrentLongitude = longitude;
                    delivery.LastLocationUpdate = DateTime.Now;

                    // Record location history
                    db.DeliveryLocations.Add(new DeliveryLocation
                    {
                        SaleId = delivery.SaleId,
                        DriverUserId = userId,
                        Latitude = latitude,
                        Longitude = longitude,
                        Timestamp = DateTime.Now,
                        Sequence = db.DeliveryLocations.Count(dl => dl.SaleId == delivery.SaleId) + 1
                    });
                }

                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }*/
    }
}