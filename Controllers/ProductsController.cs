using FarmTrack.Models;
using FarmTrack.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
// Add these using directives at the top of your ProductsController.cs
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Web.Mvc;

namespace FarmTrack.Controllers
{
    public class ProductsController : Controller
    {
        private FarmTrackContext db = new FarmTrackContext();

        private BlobStorageService _blobService;

        public ProductsController()
        {
            var connectionString = ConfigurationManager.AppSettings["AzureBlobStorageConnectionString"];
            _blobService = new BlobStorageService(connectionString);
        }

        // GET: Products
        public ActionResult Index()
        {
            var products = db.Products.Include(p => p.HarvestOutcome).Include(p => p.Inventory).Include(p => p.Livestock);
            return View(products.ToList());
        }



        // GET: Products/Details/5
        // Add these new methods to your ProductsController.cs

        // UPDATED: Check if user can review (allows multiple reviews if multiple purchases)
        private bool CanUserReview(int userId, int productId)
        {
            // Count how many times user purchased this product (delivered orders only)
            var purchaseCount = db.Sales
                .Where(s => s.UserId == userId && s.Status == "Delivered")
                .SelectMany(s => s.Items)
                .Count(i => i.ProductId == productId);

            // Count how many reviews user has submitted
            var reviewCount = db.ProductReviews
                .Count(r => r.UserId == userId && r.ProductId == productId);

            // User can review if they've purchased more times than they've reviewed
            return purchaseCount > reviewCount;
        }

        // UPDATED: Submit Review - allows multiple reviews
        [HttpPost]
        public JsonResult SubmitReview(int ProductId, int Rating, string ReviewText)
        {
            try
            {
                // Skip anti-forgery for testing
                System.Diagnostics.Debug.WriteLine($"SubmitReview called: ProductId={ProductId}, Rating={Rating}");

                if (Session["UserId"] == null)
                {
                    return Json(new { success = false, message = "Please log in to submit a review." });
                }

                int userId = (int)Session["UserId"];

                var review = new ProductReview
                {
                    ProductId = ProductId,
                    UserId = userId,
                    Rating = Rating,
                    ReviewText = ReviewText ?? "",
                    IsVerifiedPurchase = true,
                    ReviewDate = DateTime.Now,
                    IsActive = true
                };

                db.ProductReviews.Add(review);
                db.SaveChanges();

                return Json(new { success = true, message = "Review submitted successfully!" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // NEW: Submit Admin Reply to Review
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SubmitReviewReply(int reviewId, string replyText)
        {
            try
            {
                if (Session["UserId"] == null)
                {
                    return Json(new { success = false, message = "Please log in." });
                }

                int userId = (int)Session["UserId"];
                string userRole = Session["Role"]?.ToString();

                // Check if user is admin or owner
                if (userRole != "Admin" && userRole != "Owner")
                {
                    return Json(new { success = false, message = "Only administrators can reply to reviews." });
                }

                if (string.IsNullOrWhiteSpace(replyText))
                {
                    return Json(new { success = false, message = "Reply text cannot be empty." });
                }

                var review = db.ProductReviews.Find(reviewId);
                if (review == null)
                {
                    return Json(new { success = false, message = "Review not found." });
                }

                // Update the review with admin reply
                review.AdminReply = replyText.Trim();
                review.AdminReplyDate = DateTime.Now;
                review.AdminReplyUserId = userId;

                db.SaveChanges();

                // Log activity
                try
                {
                    db.LogActivity(userId, $"Replied to review #{reviewId} for product #{review.ProductId}");
                }
                catch (Exception logEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: Failed to log activity: {logEx.Message}");
                }

                return Json(new
                {
                    success = true,
                    message = "Reply posted successfully!",
                    replyText = review.AdminReply,
                    replyDate = review.AdminReplyDate.Value.ToString("MMM dd, yyyy")
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SubmitReviewReply ERROR: {ex.Message}");
                return Json(new { success = false, message = "Error posting reply: " + ex.Message });
            }
        }

        // UPDATED: Check Review Eligibility - supports multiple reviews
        public JsonResult CheckReviewEligibility(int productId)
        {
            try
            {
                if (Session["UserId"] == null)
                {
                    return Json(new
                    {
                        canReview = false,
                        message = "Please log in to review products",
                        badgeText = "Login Required",
                        badgeClass = "bg-secondary",
                        purchaseCount = 0,
                        reviewCount = 0
                    }, JsonRequestBehavior.AllowGet);
                }

                int userId = (int)Session["UserId"];

                // Count delivered purchases
                var purchaseCount = db.Sales
                    .Where(s => s.UserId == userId && s.Status == "Delivered")
                    .SelectMany(s => s.Items)
                    .Count(i => i.ProductId == productId);

                // Count existing reviews
                var reviewCount = db.ProductReviews
                    .Count(r => r.UserId == userId && r.ProductId == productId);

                bool canReview = purchaseCount > reviewCount;

                string message;
                if (purchaseCount == 0)
                {
                    message = "Purchase this product and receive delivery to leave a review";
                }
                else if (canReview)
                {
                    if (reviewCount == 0)
                    {
                        message = $"You purchased this item. Share your experience!";
                    }
                    else
                    {
                        message = $"You've purchased this {purchaseCount} time(s) and reviewed {reviewCount} time(s). Review again!";
                    }
                }
                else
                {
                    message = $"You've already reviewed all {purchaseCount} purchase(s) of this product";
                }

                return Json(new
                {
                    canReview = canReview,
                    message = message,
                    badgeText = canReview ? "Verified Purchase" : (purchaseCount > 0 ? "All Purchases Reviewed" : "Purchase Required"),
                    badgeClass = canReview ? "bg-success" : (purchaseCount > 0 ? "bg-info" : "bg-warning"),
                    purchaseCount = purchaseCount,
                    reviewCount = reviewCount
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CheckReviewEligibility ERROR: {ex.Message}");
                return Json(new
                {
                    canReview = false,
                    message = "Error checking eligibility",
                    badgeText = "Error",
                    badgeClass = "bg-danger",
                    purchaseCount = 0,
                    reviewCount = 0
                }, JsonRequestBehavior.AllowGet);
            }
        }

        // UPDATED: Details method - pass admin status to view
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

            // Get recent reviews (only verified purchases)
            var recentReviews = product.Reviews
                .Where(r => r.IsActive && r.IsVerifiedPurchase)
                .OrderByDescending(r => r.ReviewDate)
                .ToList();
            ViewBag.RecentReviews = recentReviews;

            // Check if current user can review
            bool canReview = false;
            int purchaseCount = 0;
            int reviewCount = 0;

            if (Session["UserId"] != null)
            {
                int userId = (int)Session["UserId"];

                purchaseCount = db.Sales
                    .Where(s => s.UserId == userId && s.Status == "Delivered")
                    .SelectMany(s => s.Items)
                    .Count(i => i.ProductId == product.Id);

                reviewCount = db.ProductReviews
                    .Count(r => r.UserId == userId && r.ProductId == product.Id);

                canReview = purchaseCount > reviewCount;
            }

            ViewBag.CanReview = canReview;
            ViewBag.PurchaseCount = purchaseCount;
            ViewBag.ReviewCount = reviewCount;

            // Check if user is admin
            string userRole = Session["Role"]?.ToString();
            ViewBag.IsAdmin = userRole == "Admin" || userRole == "Owner";

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
                .Where(s => s.UserId == userId && s.Status == "Delivered") // Only delivered orders count
                .SelectMany(s => s.Items)
                .Any(i => i.ProductId == productId);
        }

        // REPLACE YOUR SubmitReview METHOD with this:

        /*// STRICT VERIFIED PURCHASE SYSTEM - Only delivered orders can review
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SubmitReview(int ProductId, int Rating, string ReviewText)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"SubmitReview called: ProductId={ProductId}, Rating={Rating}");

                if (Session["UserId"] == null)
                {
                    System.Diagnostics.Debug.WriteLine("User not logged in");
                    return Json(new { success = false, message = "Please log in to submit a review." });
                }

                int userId = (int)Session["UserId"];
                System.Diagnostics.Debug.WriteLine($"User ID: {userId}");

                // STRICT VERIFICATION: Check if user has purchased AND order was delivered
                var hasPurchasedAndReceived = db.Sales
                    .Where(s => s.UserId == userId && s.Status == "Delivered")
                    .SelectMany(s => s.Items)
                    .Any(i => i.ProductId == ProductId);

                if (!hasPurchasedAndReceived)
                {
                    System.Diagnostics.Debug.WriteLine("User has not purchased or order not delivered");
                    return Json(new
                    {
                        success = false,
                        message = "Only customers who have purchased and received this product can leave reviews. Purchase the product and wait for delivery to submit a review."
                    });
                }

                // Check if user has already reviewed this product
                var existingReview = db.ProductReviews
                    .FirstOrDefault(r => r.ProductId == ProductId && r.UserId == userId);

                if (existingReview != null)
                {
                    System.Diagnostics.Debug.WriteLine("User already reviewed this product");
                    return Json(new { success = false, message = "You have already reviewed this product." });
                }

                var review = new ProductReview
                {
                    ProductId = ProductId,
                    UserId = userId,
                    Rating = Rating,
                    ReviewText = ReviewText ?? "",
                    IsVerifiedPurchase = true, // Always true - enforced by check above
                    ReviewDate = DateTime.Now,
                    IsActive = true
                };

                db.ProductReviews.Add(review);
                db.SaveChanges();

                System.Diagnostics.Debug.WriteLine($"✅ Review saved successfully! ID: {review.ProductReviewId}");

                // Log activity
                try
                {
                    db.LogActivity(userId, $"Submitted verified review for product #{ProductId}");
                }
                catch (Exception logEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: Failed to log activity: {logEx.Message}");
                }

                return Json(new { success = true, message = "Review submitted successfully! Thank you for your feedback." });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ SubmitReview ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = "Error submitting review: " + ex.Message });
            }
        }*/

        /*// Check if user can review a product
        public JsonResult CheckReviewEligibility(int productId)
        {
            try
            {
                if (Session["UserId"] == null)
                {
                    return Json(new
                    {
                        canReview = false,
                        message = "Please log in to review products",
                        badgeText = "Login Required",
                        badgeClass = "bg-secondary"
                    }, JsonRequestBehavior.AllowGet);
                }

                int userId = (int)Session["UserId"];

                // Check if user has any delivered orders containing this product
                var deliveredOrder = db.Sales
                    .Where(s => s.UserId == userId && s.Status == "Delivered")
                    .SelectMany(s => s.Items)
                    .Where(i => i.ProductId == productId)
                    .Select(i => new { i.Sale.SaleDate, i.Sale.TrackingNumber })
                    .FirstOrDefault();

                bool canReview = deliveredOrder != null;

                return Json(new
                {
                    canReview = canReview,
                    message = canReview ?
                        $"You purchased this item on {deliveredOrder.SaleDate.ToString("MMM dd, yyyy")}" :
                        "Purchase this product and receive delivery to leave a review",
                    badgeText = canReview ? "Verified Purchase" : "Purchase Required",
                    badgeClass = canReview ? "bg-success" : "bg-warning"
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CheckReviewEligibility ERROR: {ex.Message}");
                return Json(new
                {
                    canReview = false,
                    message = "Error checking eligibility",
                    badgeText = "Error",
                    badgeClass = "bg-danger"
                }, JsonRequestBehavior.AllowGet);
            }
        }*/


        // Calculate shipping fee based on distance and cart weight
        private decimal CalculateShippingFee(decimal? destinationLat, decimal? destinationLng, List<CartItem> cart)
        {
            try
            {
                // Base shipping fee
                decimal baseShippingFee = 50m; // R50 base fee

                // Calculate distance if coordinates available
                if (destinationLat.HasValue && destinationLng.HasValue)
                {
                    // Farm/warehouse location (update to your actual farm coordinates)
                    decimal farmLat = -29.8587m; // Durban example
                    decimal farmLng = 31.0218m;

                    double distance = CalculateDistance(farmLat, farmLng, destinationLat.Value, destinationLng.Value);

                    // Distance-based fee: R2 per km beyond 10km
                    if (distance > 10)
                    {
                        decimal distanceFee = (decimal)(distance - 10) * 2m;
                        baseShippingFee += distanceFee;
                    }

                    System.Diagnostics.Debug.WriteLine($"Distance: {distance:F2}km, Distance Fee: R{(decimal)(Math.Max(0, distance - 10) * 2):F2}");
                }

                // Weight-based fee (based on quantity)
                int totalItems = cart.Sum(c => c.Quantity);

                // Add R5 per item beyond 5 items
                if (totalItems > 5)
                {
                    decimal weightFee = (totalItems - 5) * 5m;
                    baseShippingFee += weightFee;
                }

                // Free shipping for orders over R500
                decimal cartTotal = (decimal)cart.Sum(c => c.Total);
                if (cartTotal >= 500m)
                {
                    baseShippingFee = 0m; // Free shipping!
                }

                // Cap maximum shipping fee at R200
                baseShippingFee = Math.Min(baseShippingFee, 200m);

                System.Diagnostics.Debug.WriteLine($"Final Shipping Fee: R{baseShippingFee:F2}");

                return Math.Round(baseShippingFee, 2);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Shipping calculation error: {ex.Message}");
                return 50m; // Default to R50 if calculation fails
            }
        }

        // Calculate distance between two points (Haversine formula)
        private double CalculateDistance(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
        {
            const double R = 6371; // Earth's radius in kilometers

            double dLat = ToRadians((double)(lat2 - lat1));
            double dLon = ToRadians((double)(lon2 - lon1));

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRadians((double)lat1)) * Math.Cos(ToRadians((double)lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double distance = R * c;

            return distance;
        }

        private double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        // NEW: AJAX endpoint to calculate shipping fee
        [HttpPost]
        public JsonResult CalculateShipping(string deliveryAddress)
        {
            try
            {
                var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();

                if (!cart.Any())
                {
                    return Json(new { success = false, message = "Cart is empty" });
                }

                // Geocode the address
                var (destLat, destLng) = GeocodeAddress(deliveryAddress);

                if (!destLat.HasValue || !destLng.HasValue)
                {
                    // Default shipping if geocoding fails
                    return Json(new
                    {
                        success = true,
                        shippingFee = 50.00m,
                        distance = 0,
                        freeShippingThreshold = 500m,
                        message = "Standard shipping rate applied"
                    });
                }

                // Calculate shipping
                decimal shippingFee = CalculateShippingFee(destLat, destLng, cart);

                // Calculate distance for display
                decimal farmLat = -29.8587m;
                decimal farmLng = 31.0218m;
                double distance = CalculateDistance(farmLat, farmLng, destLat.Value, destLng.Value);

                decimal cartTotal = (decimal)cart.Sum(c => c.Total);
                bool isFreeShipping = cartTotal >= 500m;

                return Json(new
                {
                    success = true,
                    shippingFee = shippingFee,
                    distance = Math.Round(distance, 2),
                    freeShippingThreshold = 500m,
                    currentTotal = cartTotal,
                    isFreeShipping = isFreeShipping,
                    message = isFreeShipping ? "🎉 Free shipping applied!" : $"Delivery distance: {distance:F1}km"
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Calculate shipping error: {ex.Message}");
                return Json(new { success = false, message = "Error calculating shipping" });
            }
        }

        // ========================================
        // UPDATE YOUR ProcessCheckout METHOD
        // ========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProcessCheckout(string CustomerName, string CustomerEmail, string CustomerPhone,
            string DeliveryAddress, string PaymentMethod, string voucherCode = null, string DeliveryInstructions = "")
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

                // Geocode the delivery address
                var (destLat, destLng) = GeocodeAddress(DeliveryAddress);

                if (!destLat.HasValue || !destLng.HasValue)
                {
                    TempData["Error"] = "Could not locate the delivery address. Please provide a more specific address.";
                    return RedirectToAction("Checkout");
                }

                // Calculate subtotal
                decimal subtotal = (decimal)cart.Sum(item => item.Total);

                // ✅ Calculate shipping fee
                decimal shippingFee = CalculateShippingFee(destLat, destLng, cart);

                decimal discountAmount = 0;
                DiscountVoucher appliedVoucher = null;

                // Apply voucher if provided
                if (!string.IsNullOrEmpty(voucherCode))
                {
                    appliedVoucher = db.DiscountVouchers
                        .FirstOrDefault(v => v.Code.ToUpper() == voucherCode.ToUpper() && v.IsActive);

                    if (appliedVoucher != null && appliedVoucher.IsValid)
                    {
                        if (!IsVoucherApplicableToCart(appliedVoucher, cart))
                        {
                            TempData["Error"] = "Voucher is not applicable to items in your cart.";
                            return RedirectToAction("Checkout");
                        }

                        if (appliedVoucher.MinimumOrderAmount.HasValue && subtotal < appliedVoucher.MinimumOrderAmount.Value)
                        {
                            TempData["Error"] = $"Voucher requires minimum order of R{appliedVoucher.MinimumOrderAmount.Value:0.##}";
                            return RedirectToAction("Checkout");
                        }

                        discountAmount = CalculateDiscount(appliedVoucher, subtotal);
                        appliedVoucher.UsedCount++;
                    }
                    else
                    {
                        TempData["Error"] = "Invalid or expired voucher code.";
                        return RedirectToAction("Checkout");
                    }
                }

                // ✅ Calculate total with shipping
                decimal totalAmount = subtotal - discountAmount + shippingFee;

                var trackingNumber = "FT" + DateTime.Now.ToString("yyyyMMddHHmmss");

                // Calculate delivery estimate based on distance
                double distance = CalculateDistance(-29.8587m, 31.0218m, destLat.Value, destLng.Value);
                int deliveryDays = distance < 20 ? 1 : (distance < 50 ? 2 : 3);
                var estimatedDelivery = DateTime.Now.AddDays(deliveryDays);

                var sale = new Sale
                {
                    SaleDate = DateTime.Now,
                    Subtotal = subtotal,
                    DiscountAmount = discountAmount,
                    ShippingFee = shippingFee, // ✅ NEW
                    TotalAmount = totalAmount,
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
                    DestinationLatitude = destLat.Value,
                    DestinationLongitude = destLng.Value,
                    AppliedVoucherId = appliedVoucher?.VoucherId,
                    AppliedVoucherCode = appliedVoucher?.Code,
                    Items = new List<SaleItem>()
                };

                // Create sale items and update inventory
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

                // Record voucher usage
                if (appliedVoucher != null)
                {
                    var voucherUsage = new VoucherUsage
                    {
                        VoucherId = appliedVoucher.VoucherId,
                        SaleId = sale.SaleId,
                        UserId = userId,
                        DiscountAmount = discountAmount,
                        OrderTotalBeforeDiscount = subtotal,
                        OrderTotalAfterDiscount = totalAmount,
                        UsedAt = DateTime.Now
                    };
                    db.VoucherUsages.Add(voucherUsage);
                }

                // Payment processing
                string statusNote = $"Payment received. Preparing your order. Shipping: R{shippingFee:F2}";
                if (shippingFee == 0)
                {
                    statusNote += " (Free shipping applied!)";
                }
                if (appliedVoucher != null)
                {
                    statusNote += $" Voucher '{appliedVoucher.Code}' applied.";
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
                        Notes = statusNote,
                        UpdateTime = DateTime.Now
                    });
                }
                else
                {
                    db.OrderStatusUpdates.Add(new OrderStatusUpdate
                    {
                        SaleId = sale.SaleId,
                        Status = "Order Confirmed",
                        Notes = "Order confirmed. " + statusNote,
                        UpdateTime = DateTime.Now
                    });
                }

                db.Sales.Add(sale);
                db.SaveChanges();

                Session["Cart"] = new List<CartItem>();

                string successMessage = $"Order #{sale.SaleId} placed successfully! ";
                if (discountAmount > 0)
                {
                    successMessage += $"You saved R{discountAmount:F2}! ";
                }
                if (shippingFee == 0)
                {
                    successMessage += "Free shipping! ";
                }
                successMessage += $"Tracking: {trackingNumber}";

                TempData["Success"] = successMessage;
                return RedirectToAction("OrderTracking", new { id = sale.SaleId });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Checkout error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                TempData["Error"] = $"Checkout failed: {ex.Message}";
                return RedirectToAction("Checkout");
            }
        }

        // Helper method to check voucher applicability to cart
        private bool IsVoucherApplicableToCart(DiscountVoucher voucher, List<CartItem> cart)
        {
            switch (voucher.Applicability)
            {
                case VoucherApplicability.AllProducts:
                    return true;
                case VoucherApplicability.SpecificCategory:
                    return cart.Any(item => item.Category == voucher.ApplicableCategory);
                case VoucherApplicability.SpecificProduct:
                    return cart.Any(item => item.ProductId == voucher.ApplicableProductId);
                case VoucherApplicability.MinimumOrderOnly:
                    return true;
                default:
                    return true;
            }
        }

        // Helper method to calculate discount
        private decimal CalculateDiscount(DiscountVoucher voucher, decimal cartTotal)
        {
            decimal discount = 0;

            if (voucher.VoucherType == VoucherType.FixedAmount)
            {
                discount = Math.Min(voucher.DiscountValue, cartTotal);
            }
            else if (voucher.VoucherType == VoucherType.Percentage)
            {
                discount = cartTotal * (voucher.DiscountValue / 100);

                // Apply maximum discount limit
                if (voucher.MaximumDiscount.HasValue)
                {
                    discount = Math.Min(discount, voucher.MaximumDiscount.Value);
                }
            }

            return Math.Round(discount, 2);
        }

        // ===== PASTE THIS INTO YOUR ProductsController.cs - REPLACE THE EXISTING METHODS =====

        // STEP 1: IMPROVED GEOCODING with better error handling
        // IMPROVED GEOCODING with better accuracy
        private (decimal? lat, decimal? lng) GeocodeAddress(string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                System.Diagnostics.Debug.WriteLine("GeocodeAddress: Empty address provided");
                return (-29.8587m, 31.0218m); // Default Durban coordinates
            }

            try
            {
                string apiKey = ConfigurationManager.AppSettings["GoogleMapsApiKey"];

                if (string.IsNullOrEmpty(apiKey))
                {
                    System.Diagnostics.Debug.WriteLine("GeocodeAddress: No Google Maps API key found");
                    return (-29.8587m, 31.0218m);
                }

                // Clean and improve the address for better geocoding
                string cleanedAddress = CleanAddressForGeocoding(address);

                string url = $"https://maps.googleapis.com/maps/api/geocode/json?address={Uri.EscapeDataString(cleanedAddress)}&region=za&key={apiKey}";

                using (var client = new System.Net.WebClient())
                {
                    string response = client.DownloadString(url);
                    dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject(response);

                    System.Diagnostics.Debug.WriteLine($"Geocoding response status: {json.status}");

                    if (json.status == "OK" && json.results.Count > 0)
                    {
                        // Get the most precise result
                        var result = json.results[0];
                        decimal lat = result.geometry.location.lat;
                        decimal lng = result.geometry.location.lng;

                        // Check location type for accuracy
                        string locationType = result.geometry.location_type;
                        decimal accuracy = GetAccuracyScore(locationType);

                        System.Diagnostics.Debug.WriteLine($"Geocoded '{address}' to: {lat}, {lng} (Accuracy: {locationType})");

                        return (lat, lng);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Geocoding failed. Status: {json.status}");
                        return (-29.8587m, 31.0218m);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Geocoding error: {ex.Message}");
                return (-29.8587m, 31.0218m);
            }
        }

        // Helper method to clean addresses for better geocoding
        private string CleanAddressForGeocoding(string address)
        {
            if (string.IsNullOrEmpty(address)) return address;

            // Add South Africa to improve accuracy
            if (!address.ToLower().Contains("south africa") && !address.ToLower().Contains("sa"))
            {
                address += ", South Africa";
            }

            // Remove common issues
            address = address.Replace("P.O. Box", "").Replace("P.O Box", "").Replace("PO Box", "");

            return address.Trim();
        }

        // Helper method to score geocoding accuracy
        private decimal GetAccuracyScore(string locationType)
        {
            switch (locationType)
            {
                case "ROOFTOP": return 1.0m;      // Most accurate
                case "RANGE_INTERPOLATED": return 0.8m;
                case "GEOMETRIC_CENTER": return 0.6m;
                case "APPROXIMATE": return 0.4m;  // Least accurate
                default: return 0.5m;
            }
        }/*
        private (decimal? lat, decimal? lng) GeocodeAddress(string address)
        {
            if (string.IsNullOrEmpty(address)) return (null, null);

            try
            {
                string apiKey = System.Configuration.ConfigurationManager.AppSettings["GoogleMapsApiKey"];

                // Add region bias for South Africa
                string url = $"https://maps.googleapis.com/maps/api/geocode/json?address={Uri.EscapeDataString(address)}&region=za&key={apiKey}";

                using (var client = new System.Net.WebClient())
                {
                    string response = client.DownloadString(url);
                    dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject(response);

                    System.Diagnostics.Debug.WriteLine($"Geocoding response: {response}");

                    if (json.status == "OK" && json.results.Count > 0)
                    {
                        decimal lat = json.results[0].geometry.location.lat;
                        decimal lng = json.results[0].geometry.location.lng;

                        System.Diagnostics.Debug.WriteLine($"Geocoded: {address} -> {lat}, {lng}");

                        return (lat, lng);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Geocoding failed: {json.status}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Geocoding error: {ex.Message}");
            }

            return (null, null);
        }*/

        // STEP 2: FIXED GetRealDeliveryData - ONE SET OF FALLBACK COORDINATES
        // FIXED: GetRealDeliveryData - Uses real-time driver GPS when available
        public JsonResult GetRealDeliveryData(int saleId)
        {
            try
            {
                int? userId = Session["UserId"] as int?;
                if (!userId.HasValue)
                {
                    return Json(new { success = false, error = "Not logged in" }, JsonRequestBehavior.AllowGet);
                }

                string userRole = Session["Role"]?.ToString();
                bool isAdmin = userRole == "Admin" || userRole == "Owner";

                var sale = db.Sales
                    .Include(s => s.AssignedDriver)
                    .FirstOrDefault(s => s.SaleId == saleId);

                if (sale == null)
                {
                    return Json(new { success = false, error = "Order not found" }, JsonRequestBehavior.AllowGet);
                }

                // ✅ FIX: Verify permissions - allow admin, customer, or driver
                bool isCustomer = sale.UserId == userId.Value;
                bool isDriver = sale.AssignedDriverId == userId.Value;

                if (!isAdmin && !isCustomer && !isDriver)
                {
                    return Json(new { success = false, error = "Access denied" }, JsonRequestBehavior.AllowGet);
                }

                // **Get REAL-TIME driver location from User table**
                decimal? currentLat = null;
                decimal? currentLng = null;
                bool hasRealLocation = false;

                if (sale.AssignedDriverId.HasValue)
                {
                    var driver = db.Users.Find(sale.AssignedDriverId.Value);

                    // Check if driver is online AND has recent location (within 2 minutes)
                    if (driver != null && driver.IsOnlineAsDriver &&
                        driver.CurrentLatitude.HasValue && driver.CurrentLongitude.HasValue &&
                        driver.LastOnlineTime.HasValue &&
                        (DateTime.Now - driver.LastOnlineTime.Value).TotalMinutes < 2)
                    {
                        currentLat = driver.CurrentLatitude;
                        currentLng = driver.CurrentLongitude;
                        hasRealLocation = true;

                        System.Diagnostics.Debug.WriteLine($"USING REAL-TIME DRIVER LOCATION: {currentLat}, {currentLng}");

                        // Update sale with current real-time location
                        sale.CurrentLatitude = currentLat;
                        sale.CurrentLongitude = currentLng;
                        sale.LastLocationUpdate = DateTime.Now;
                        db.SaveChanges();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Driver offline or location stale. Online: {driver?.IsOnlineAsDriver}, LastUpdate: {driver?.LastOnlineTime}");
                    }
                }

                // Get destination coordinates
                decimal destinationLat = sale.DestinationLatitude ?? -29.8587m;
                decimal destinationLng = sale.DestinationLongitude ?? 31.0218m;

                // Only use fallback if no real location available
                if (!hasRealLocation)
                {
                    if (sale.AssignedDriverId.HasValue && sale.CurrentLatitude.HasValue && sale.CurrentLongitude.HasValue)
                    {
                        currentLat = sale.CurrentLatitude;
                        currentLng = sale.CurrentLongitude;
                        System.Diagnostics.Debug.WriteLine($"USING LAST KNOWN LOCATION: {currentLat}, {currentLng}");
                    }
                    else
                    {
                        currentLat = destinationLat;
                        currentLng = destinationLng;
                        System.Diagnostics.Debug.WriteLine($"USING DESTINATION AS PLACEHOLDER: {currentLat}, {currentLng}");
                    }
                }

                var deliveryData = new
                {
                    currentLat = currentLat.Value,
                    currentLng = currentLng.Value,
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
                    driverIsOnline = sale.AssignedDriver?.IsOnlineAsDriver ?? false,
                    hasRealLocation = hasRealLocation,
                    locationSource = hasRealLocation ? "real-time-gps" : (sale.AssignedDriverId.HasValue ? "last-known" : "destination"),

                    // ✅ NEW: Pass user role info to frontend
                    viewerRole = isAdmin ? "admin" : (isDriver ? "driver" : "customer")
                };

                System.Diagnostics.Debug.WriteLine($"Delivery Data - Source: {deliveryData.locationSource}, RealLocation: {hasRealLocation}, Viewer: {deliveryData.viewerRole}");

                return Json(new { success = true, deliveryData }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetRealDeliveryData ERROR: {ex.Message}");
                return Json(new { success = false, error = "Server error loading delivery data" }, JsonRequestBehavior.AllowGet);
            }
        }

        // STEP 3: BETTER UpdateDriverLocation with debugging
        [HttpPost]
        public JsonResult UpdateDriverLocation(decimal latitude, decimal longitude)
        {
            try
            {
                if (Session["UserId"] == null)
                {
                    return Json(new { success = false, message = "Not logged in" });
                }

                int userId = (int)Session["UserId"];
                var user = db.Users.Find(userId);

                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                System.Diagnostics.Debug.WriteLine($"UPDATING DRIVER LOCATION: User={userId}, Lat={latitude}, Lng={longitude}");

                // Update user's location
                user.CurrentLatitude = latitude;
                user.CurrentLongitude = longitude;
                user.LastOnlineTime = DateTime.Now;
                user.IsOnlineAsDriver = true;

                // Update ALL active deliveries
                var activeDeliveries = db.Sales
                    .Where(s => s.AssignedDriverId == userId &&
                           (s.Status == "Assigned to Driver" || s.Status == "Out for Delivery"))
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"Found {activeDeliveries.Count} active deliveries");

                foreach (var delivery in activeDeliveries)
                {
                    delivery.CurrentLatitude = latitude;
                    delivery.CurrentLongitude = longitude;
                    delivery.LastLocationUpdate = DateTime.Now;
                    delivery.IsActiveDelivery = true;

                    // Record in history
                    db.DeliveryLocations.Add(new DeliveryLocation
                    {
                        SaleId = delivery.SaleId,
                        DriverUserId = userId,
                        Latitude = latitude,
                        Longitude = longitude,
                        Timestamp = DateTime.Now,
                        Sequence = db.DeliveryLocations.Count(dl => dl.SaleId == delivery.SaleId) + 1
                    });

                    System.Diagnostics.Debug.WriteLine($"Updated delivery #{delivery.SaleId} to {latitude},{longitude}");
                }

                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    updatedDeliveries = activeDeliveries.Count,
                    message = $"Location updated: {latitude:F6}, {longitude:F6}",
                    latitude = latitude,
                    longitude = longitude
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateDriverLocation ERROR: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // STEP 4: BETTER GoOnlineAsDriver
        [HttpPost]
        public JsonResult GoOnlineAsDriver(decimal latitude, decimal longitude)
        {
            try
            {
                if (Session["UserId"] == null)
                {
                    return Json(new { success = false, message = "Not logged in" });
                }

                int userId = (int)Session["UserId"];
                var user = db.Users.Find(userId);

                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                System.Diagnostics.Debug.WriteLine($"GO ONLINE: User={userId}, Lat={latitude}, Lng={longitude}");

                user.CurrentLatitude = latitude;
                user.CurrentLongitude = longitude;
                user.IsOnlineAsDriver = true;
                user.LastOnlineTime = DateTime.Now;

                db.SaveChanges();

                System.Diagnostics.Debug.WriteLine($"Driver #{userId} now ONLINE at {latitude},{longitude}");

                return Json(new
                {
                    success = true,
                    message = "You are now online",
                    latitude = latitude,
                    longitude = longitude
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GoOnlineAsDriver ERROR: {ex.Message}");
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

        // POST: Products/Create - UPDATED FOR BLOB STORAGE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "Id,Name,ProductType,Category,Unit,Quantity,PricePerUnit,Description,ImageUrl,IsAvailable,IsFeatured,MinimumOrder,HarvestOutcomeId,LivestockId,InventoryId")] Product product, HttpPostedFileBase imageFile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Handle image upload
                    if (imageFile != null && imageFile.ContentLength > 0)
                    {
                        var imageUrl = await _blobService.UploadImageAsync(imageFile, product.Name);
                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            product.ImageUrl = imageUrl;
                        }
                        else
                        {
                            TempData["Error"] = "Failed to upload image. Please try again.";
                            ViewBag.HarvestOutcomeId = new SelectList(db.HarvestOutcomes, "Id", "QualityGrade", product.HarvestOutcomeId);
                            ViewBag.InventoryId = new SelectList(db.Inventories, "InventoryId", "ItemName", product.InventoryId);
                            ViewBag.LivestockId = new SelectList(db.Livestocks, "LivestockId", "Type", product.LivestockId);
                            return View(product);
                        }
                    }

                    // Set timestamps
                    product.CreatedAt = DateTime.Now;
                    product.LastUpdated = DateTime.Now;

                    // Set default values if not provided
                    if (product.MinimumOrder <= 0) product.MinimumOrder = 1;
                    if (string.IsNullOrEmpty(product.Description)) product.Description = $"High quality {product.Name}";

                    db.Products.Add(product);
                    db.SaveChanges();

                    TempData["Success"] = "Product created successfully!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Error creating product: {ex.Message}";
                }
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

        // POST: Products/Edit/5 - UPDATED FOR BLOB STORAGE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Id,Name,ProductType,Category,Unit,Quantity,PricePerUnit,Description,ImageUrl,IsAvailable,IsFeatured,MinimumOrder,HarvestOutcomeId,LivestockId,InventoryId,CreatedAt")] Product product, HttpPostedFileBase imageFile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Handle image upload
                    if (imageFile != null && imageFile.ContentLength > 0)
                    {
                        // Upload to blob storage and get URL
                        var imageUrl = await _blobService.UploadImageAsync(imageFile, product.Name);

                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            // Delete old image if it exists and is from blob storage
                            if (!string.IsNullOrEmpty(product.ImageUrl) && product.ImageUrl.Contains("blob.core.windows.net"))
                            {
                                await _blobService.DeleteImageAsync(product.ImageUrl);
                            }
                            product.ImageUrl = imageUrl;
                        }
                        else
                        {
                            TempData["Error"] = "Failed to upload image. Please try again.";
                            return View(product);
                        }
                    }

                    // Update the LastUpdated timestamp
                    product.LastUpdated = DateTime.Now;

                    db.Entry(product).State = EntityState.Modified;
                    db.SaveChanges();

                    TempData["Success"] = "Product updated successfully!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Error updating product: {ex.Message}";
                }
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
                    Quantity = quantity,
                    Category = product.Category,
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }

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
            string userRole = Session["Role"]?.ToString();

            // ✅ FIX: Check if user is admin/owner
            bool isAdmin = userRole == "Admin" || userRole == "Owner";

            // ✅ FIX: Get sale without user restriction if admin
            var sale = isAdmin
                ? db.Sales
                    .Where(s => s.SaleId == id)
                    .Include(s => s.Items)
                    .Include(s => s.Items.Select(i => i.Product))
                    .FirstOrDefault()
                : db.Sales
                    .Where(s => s.SaleId == id && s.UserId == userId)
                    .Include(s => s.Items)
                    .Include(s => s.Items.Select(i => i.Product))
                    .FirstOrDefault();

            if (sale == null)
            {
                TempData["Error"] = "Order not found or you don't have permission to view this order.";
                return RedirectToAction(isAdmin ? "SalesList" : "MyOrders");
            }

            ViewBag.IsAdmin = isAdmin;

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
            string userRole = Session["Role"]?.ToString();

            // ✅ FIX: Check if user is admin/owner
            bool isAdmin = userRole == "Admin" || userRole == "Owner";

            // ✅ FIX: Get sale without user restriction if admin
            var sale = isAdmin
                ? db.Sales
                    .Where(s => s.SaleId == id)
                    .Include(s => s.Items)
                    .Include(s => s.Items.Select(i => i.Product))
                    .Include(s => s.OrderStatusUpdates)
                    .FirstOrDefault()
                : db.Sales
                    .Where(s => s.SaleId == id && s.UserId == userId)
                    .Include(s => s.Items)
                    .Include(s => s.Items.Select(i => i.Product))
                    .Include(s => s.OrderStatusUpdates)
                    .FirstOrDefault();

            if (sale == null)
            {
                return Json(new { error = "Order not found" }, JsonRequestBehavior.AllowGet);
            }

            // ✅ FIX: Permission check - allow admin, customer, or driver
            bool isCustomer = sale.UserId == userId;
            bool isAssignedDriver = sale.AssignedDriverId == userId;

            if (!isAdmin && !isCustomer && !isAssignedDriver)
            {
                return Json(new { error = "Access denied" }, JsonRequestBehavior.AllowGet);
            }

            ViewBag.IsAdmin = isAdmin;
            ViewBag.IsDriver = isAssignedDriver;
            ViewBag.IsCustomer = isCustomer;

            return View(sale);
        }


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

        // REPLACE your existing DeliveryDashboard method with this fixed version:

        public ActionResult DeliveryDashboard()
        {
            // Check if user is logged in
            if (Session["UserId"] == null)
            {
                TempData["Error"] = "Please log in to access the delivery dashboard.";
                return RedirectToAction("Login", "Account");
            }

            int currentUserId = (int)Session["UserId"];

            // Get all deliveries that need driver assignment or are in progress
            var allDeliveries = db.Sales
                .Where(s => s.Status == "Confirmed" ||
                           s.Status == "Assigned to Driver" ||
                           s.Status == "Out for Delivery")
                .Include(s => s.AssignedDriver)
                .Include(s => s.Items)
                .OrderBy(s => s.SaleDate)
                .ToList();

            // Filter: My assigned deliveries (includes "Assigned to Driver" and "Out for Delivery")
            var myAssignedDeliveries = allDeliveries
                .Where(s => s.AssignedDriverId == currentUserId)
                .ToList();

            // Filter: Available deliveries (not assigned to anyone)
            var availableDeliveries = allDeliveries
                .Where(s => !s.AssignedDriverId.HasValue)
                .ToList();

            // Store both lists for the view
            ViewBag.MyAssignedDeliveries = myAssignedDeliveries;
            ViewBag.AvailableDeliveries = availableDeliveries;
            ViewBag.CurrentUserId = currentUserId;

            return View(allDeliveries);
        }
        /*
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
        }*/

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

        // Complete delivery
        // Complete delivery with verification option
        [HttpPost]
        public JsonResult CompleteDelivery(int saleId, bool useVerification = false)
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
                    return Json(new { success = false, message = "Delivery not assigned to you" });
                }

                // If verification is required, generate code instead of completing
                if (useVerification)
                {
                    return GenerateDeliveryCode(saleId);
                }

                // Traditional completion without verification
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

                return Json(new { success = true, message = "Delivery completed successfully!" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CompleteDelivery ERROR: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        

        // Driver completes delivery by entering customer's code
        [HttpPost]
        public JsonResult CompleteDeliveryWithCode(int saleId, string customerCode)
        {
            try
            {
                if (Session["UserId"] == null)
                {
                    return Json(new { success = false, message = "Not logged in" });
                }

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

                // Verify the code matches
                if (string.IsNullOrEmpty(sale.DeliveryVerificationCode) || sale.DeliveryVerificationCode != customerCode)
                {
                    // Log failed attempt
                    db.DeliveryVerifications.Add(new DeliveryVerification
                    {
                        SaleId = saleId,
                        DriverUserId = userId,
                        VerificationCode = customerCode,
                        VerifiedAt = DateTime.Now,
                        VerificationMethod = "Code",
                        IsSuccessful = false,
                        Notes = "Invalid code provided"
                    });

                    db.SaveChanges();

                    return Json(new
                    {
                        success = false,
                        message = "Invalid verification code. Please ask the customer for the correct code."
                    });
                }

                // Check if code is expired (10 minutes)
                if (sale.DeliveryCodeGeneratedAt.HasValue &&
                    (DateTime.Now - sale.DeliveryCodeGeneratedAt.Value).TotalMinutes > 10)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Verification code has expired. Please ask customer to generate a new code."
                    });
                }

                // SUCCESS: Complete delivery
                sale.Status = "Delivered";
                sale.IsActiveDelivery = false;
                sale.DeliveryVerifiedAt = DateTime.Now;
                sale.DeliveryVerifiedByDriverId = userId;
                sale.DeliveryCodeStatus = "Used";

                // Log successful verification
                db.DeliveryVerifications.Add(new DeliveryVerification
                {
                    SaleId = saleId,
                    DriverUserId = userId,
                    CustomerUserId = sale.UserId,
                    VerificationCode = customerCode,
                    VerifiedAt = DateTime.Now,
                    VerificationMethod = "Code",
                    IsSuccessful = true,
                    Notes = "Delivery verified with customer code"
                });

                db.OrderStatusUpdates.Add(new OrderStatusUpdate
                {
                    SaleId = saleId,
                    Status = "Delivered",
                    Notes = "Delivery completed and verified with customer code",
                    UpdateTime = DateTime.Now
                });

                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Delivery completed successfully! Verification code accepted."
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CompleteDeliveryWithCode ERROR: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // REPLACE these two methods in your ProductsController.cs

        // Customer can view their verification code
        public JsonResult GetCustomerVerificationCode(int saleId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"GetCustomerVerificationCode called for saleId: {saleId}");

                if (Session["UserId"] == null)
                {
                    System.Diagnostics.Debug.WriteLine("User not logged in");
                    return Json(new { success = false, message = "Please log in" }, JsonRequestBehavior.AllowGet);
                }

                int userId = (int)Session["UserId"];
                System.Diagnostics.Debug.WriteLine($"User ID: {userId}");

                var sale = db.Sales.FirstOrDefault(s => s.SaleId == saleId && s.UserId == userId);

                if (sale == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Order not found for saleId: {saleId}, userId: {userId}");
                    return Json(new { success = false, message = "Order not found" }, JsonRequestBehavior.AllowGet);
                }

                System.Diagnostics.Debug.WriteLine($"Order found. Status: {sale.Status}");

                if (sale.Status != "Out for Delivery")
                {
                    System.Diagnostics.Debug.WriteLine($"Order status is '{sale.Status}', not 'Out for Delivery'");
                    return Json(new
                    {
                        success = false,
                        message = $"Delivery not in progress. Current status: {sale.Status}"
                    }, JsonRequestBehavior.AllowGet);
                }

                System.Diagnostics.Debug.WriteLine($"Existing code: '{sale.DeliveryVerificationCode}', Generated at: {sale.DeliveryCodeGeneratedAt}");

                // Check if code exists and is not expired
                if (!string.IsNullOrEmpty(sale.DeliveryVerificationCode) &&
                    sale.DeliveryCodeGeneratedAt.HasValue &&
                    (DateTime.Now - sale.DeliveryCodeGeneratedAt.Value).TotalMinutes <= 10)
                {
                    System.Diagnostics.Debug.WriteLine("Valid code found - returning it");

                    return Json(new
                    {
                        success = true,
                        hasCode = true,
                        code = sale.DeliveryVerificationCode,
                        generatedAt = sale.DeliveryCodeGeneratedAt.Value.ToString("HH:mm"),
                        expiresAt = sale.DeliveryCodeGeneratedAt.Value.AddMinutes(10).ToString("HH:mm"),
                        message = "Show this code to your driver when they arrive"
                    }, JsonRequestBehavior.AllowGet);
                }

                System.Diagnostics.Debug.WriteLine("No valid code found");

                return Json(new
                {
                    success = true,
                    hasCode = false,
                    message = "Generate a verification code for your driver"
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetCustomerVerificationCode ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new
                {
                    success = false,
                    message = "Error: " + ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        // Generate delivery verification code
        [HttpPost]
        public JsonResult GenerateDeliveryCode(int saleId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"GenerateDeliveryCode called for saleId: {saleId}");

                if (Session["UserId"] == null)
                {
                    System.Diagnostics.Debug.WriteLine("User not logged in");
                    return Json(new { success = false, message = "Please log in" });
                }

                int userId = (int)Session["UserId"];
                System.Diagnostics.Debug.WriteLine($"User ID: {userId}");

                var sale = db.Sales.FirstOrDefault(s => s.SaleId == saleId && s.UserId == userId);

                if (sale == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Order not found for saleId: {saleId}, userId: {userId}");
                    return Json(new { success = false, message = "Order not found" });
                }

                System.Diagnostics.Debug.WriteLine($"Order found. Status: {sale.Status}");

                // Generate a simple 4-digit code for customer
                var random = new Random();
                string code = random.Next(1000, 9999).ToString(); // 4-digit code

                System.Diagnostics.Debug.WriteLine($"Generated code: {code}");

                // Update the sale with the new code
                sale.DeliveryVerificationCode = code;
                sale.DeliveryCodeGeneratedAt = DateTime.Now;
                sale.DeliveryCodeStatus = "Generated";

                try
                {
                    db.SaveChanges();
                    System.Diagnostics.Debug.WriteLine("✅ Code saved to database successfully");
                }
                catch (Exception saveEx)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error saving to database: {saveEx.Message}");
                    return Json(new { success = false, message = "Error saving code: " + saveEx.Message });
                }

                // Log the code generation in verification history
                try
                {
                    db.DeliveryVerifications.Add(new DeliveryVerification
                    {
                        SaleId = saleId,
                        CustomerUserId = sale.UserId,
                        VerificationCode = code,
                        VerifiedAt = DateTime.Now,
                        VerificationMethod = "CodeGenerated",
                        IsSuccessful = true,
                        Notes = "Verification code generated for customer"
                    });

                    db.SaveChanges();
                    System.Diagnostics.Debug.WriteLine("✅ Verification history logged");
                }
                catch (Exception logEx)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Warning: Failed to log verification history: {logEx.Message}");
                    // Don't fail the whole operation if logging fails
                }

                System.Diagnostics.Debug.WriteLine($"✅ Successfully generated code {code} for order {saleId}");

                return Json(new
                {
                    success = true,
                    verificationCode = code,
                    message = $"Your verification code is: {code}",
                    generatedAt = DateTime.Now.ToString("HH:mm")
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ GenerateDeliveryCode ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new
                {
                    success = false,
                    message = "Error generating code: " + ex.Message
                });
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
            string userRole = Session["Role"]?.ToString();

            // ✅ FIX: Check if user is admin/owner OR driver OR customer
            bool isAdmin = userRole == "Admin" || userRole == "Owner";

            var sale = db.Sales
                .Include(s => s.AssignedDriver)
                .FirstOrDefault(s => s.SaleId == id);

            if (sale == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction(isAdmin ? "SalesList" : "MyOrders");
            }

            // ✅ FIX: Permission check - allow admin, customer, or assigned driver
            bool isCustomer = sale.UserId == userId;
            bool isAssignedDriver = sale.AssignedDriverId == userId;

            if (!isAdmin && !isCustomer && !isAssignedDriver)
            {
                TempData["Error"] = "You don't have permission to view this order.";
                return RedirectToAction("MyOrders");
            }

            // Pass user role to view for conditional display
            ViewBag.IsAdmin = isAdmin;
            ViewBag.IsDriver = isAssignedDriver;
            ViewBag.IsCustomer = isCustomer;

            return View(sale);
        }

        // Add this method to your ProductsController.cs

        public ActionResult SalesAnalytics(string period = "30days")
        {
            // Check if user is admin
            if (Session["Role"]?.ToString() != "Admin" && Session["Role"]?.ToString() != "Owner")
            {
                TempData["Error"] = "Access denied. Admin privileges required.";
                return RedirectToAction("Login", "Account");
            }

            // Determine date range based on period
            DateTime endDate = DateTime.Now;
            DateTime startDate;
            DateTime previousStartDate;
            DateTime previousEndDate;

            switch (period)
            {
                case "7days":
                    startDate = endDate.AddDays(-7);
                    previousStartDate = startDate.AddDays(-7);
                    previousEndDate = startDate;
                    break;
                case "90days":
                    startDate = endDate.AddDays(-90);
                    previousStartDate = startDate.AddDays(-90);
                    previousEndDate = startDate;
                    break;
                case "year":
                    startDate = new DateTime(endDate.Year, 1, 1);
                    previousStartDate = new DateTime(endDate.Year - 1, 1, 1);
                    previousEndDate = new DateTime(endDate.Year - 1, 12, 31);
                    break;
                default: // 30days
                    period = "30days";
                    startDate = endDate.AddDays(-30);
                    previousStartDate = startDate.AddDays(-30);
                    previousEndDate = startDate;
                    break;
            }

            // Get current period sales
            var currentSales = db.Sales
                .Include(s => s.Items)
                .Include(s => s.Items.Select(i => i.Product))
                .Include(s => s.User)
                .Where(s => s.SaleDate >= startDate && s.SaleDate <= endDate)
                .ToList();

            // Get previous period sales for comparison
            var previousSales = db.Sales
                .Where(s => s.SaleDate >= previousStartDate && s.SaleDate < previousEndDate)
                .ToList();

            // Calculate key metrics
            decimal totalRevenue = currentSales.Sum(s => s.TotalAmount);
            int totalOrders = currentSales.Count;
            decimal avgOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

            // Previous period metrics for comparison
            decimal previousRevenue = previousSales.Sum(s => s.TotalAmount);
            int previousOrders = previousSales.Count;
            decimal previousAOV = previousOrders > 0 ? previousRevenue / previousOrders : 0;

            // Calculate changes
            decimal revenueChange = previousRevenue > 0 ? ((totalRevenue - previousRevenue) / previousRevenue) * 100 : 0;
            decimal ordersChange = previousOrders > 0 ? ((totalOrders - previousOrders) / (decimal)previousOrders) * 100 : 0;
            decimal aovChange = previousAOV > 0 ? ((avgOrderValue - previousAOV) / previousAOV) * 100 : 0;

            // Order status breakdown
            int pendingOrders = currentSales.Count(s => s.Status == "Pending");
            int confirmedOrders = currentSales.Count(s => s.Status == "Confirmed");
            int outForDeliveryOrders = currentSales.Count(s => s.Status == "Out for Delivery");
            int deliveredOrders = currentSales.Count(s => s.Status == "Delivered");
            int cancelledOrders = currentSales.Count(s => s.Status == "Cancelled");

            // Fulfillment rate
            decimal fulfillmentRate = totalOrders > 0 ? (deliveredOrders * 100.0m / totalOrders) : 0;

            // Top performing products
            var topProducts = currentSales
                .SelectMany(s => s.Items)
                .GroupBy(i => new { i.Product.Name, i.Product.Category })
                .Select(g => new TopProductData
                {
                    ProductName = g.Key.Name ?? "Unknown",
                    Category = g.Key.Category ?? "Uncategorized",
                    UnitsSold = g.Sum(i => i.Quantity),
                    Revenue = g.Sum(i => i.Price * i.Quantity),
                    RevenuePercentage = 0 // Will calculate after
                })
                .OrderByDescending(p => p.Revenue)
                .Take(10)
                .ToList();

            // Calculate revenue percentages
            foreach (var product in topProducts)
            {
                product.RevenuePercentage = totalRevenue > 0 ? (product.Revenue / totalRevenue) * 100 : 0;
            }

            // Top customers by revenue
            var topCustomers = currentSales
                .GroupBy(s => new { s.CustomerName, s.CustomerEmail })
                .Select(g => new TopCustomerData
                {
                    CustomerName = g.Key.CustomerName ?? "Unknown",
                    CustomerEmail = g.Key.CustomerEmail ?? "",
                    OrderCount = g.Count(),
                    TotalSpent = g.Sum(s => s.TotalAmount)
                })
                .OrderByDescending(c => c.TotalSpent)
                .Take(10)
                .ToList();

            // Customer segmentation
            var customerOrderCounts = currentSales
                .GroupBy(s => s.UserId)
                .Select(g => g.Count())
                .ToList();

            int uniqueCustomers = customerOrderCounts.Count;
            int oneTimeBuyers = customerOrderCounts.Count(c => c == 1);
            int repeatCustomers = customerOrderCounts.Count(c => c >= 2 && c <= 5);
            int loyalCustomers = customerOrderCounts.Count(c => c >= 6);

            decimal oneTimeBuyerPct = uniqueCustomers > 0 ? (oneTimeBuyers * 100.0m / uniqueCustomers) : 0;
            decimal repeatCustomerPct = uniqueCustomers > 0 ? (repeatCustomers * 100.0m / uniqueCustomers) : 0;
            decimal loyalCustomerPct = uniqueCustomers > 0 ? (loyalCustomers * 100.0m / uniqueCustomers) : 0;
            decimal avgOrdersPerCustomer = uniqueCustomers > 0 ? totalOrders / (decimal)uniqueCustomers : 0;

            // Category breakdown
            var categoryBreakdown = currentSales
                .SelectMany(s => s.Items)
                .GroupBy(i => i.Product.Category ?? "Uncategorized")
                .Select(g => new CategoryData
                {
                    Category = g.Key,
                    OrderCount = g.Select(i => i.SaleId).Distinct().Count(),
                    Revenue = g.Sum(i => i.Price * i.Quantity),
                    RevenuePercentage = 0
                })
                .OrderByDescending(c => c.Revenue)
                .ToList();

            // Calculate category percentages
            foreach (var category in categoryBreakdown)
            {
                category.RevenuePercentage = totalRevenue > 0 ? (category.Revenue / totalRevenue) * 100 : 0;
            }

            // Revenue trend data (daily for 7/30 days, weekly for 90 days, monthly for year)
            var revenueTrendLabels = new List<string>();
            var revenueTrendData = new List<decimal>();
            var ordersTrendData = new List<int>();

            if (period == "7days" || period == "30days")
            {
                // Daily breakdown
                int days = period == "7days" ? 7 : 30;
                for (int i = days - 1; i >= 0; i--)
                {
                    var date = endDate.AddDays(-i).Date;
                    var daySales = currentSales.Where(s => s.SaleDate.Date == date).ToList();

                    revenueTrendLabels.Add(date.ToString("MMM dd"));
                    revenueTrendData.Add(daySales.Sum(s => s.TotalAmount));
                    ordersTrendData.Add(daySales.Count);
                }
            }
            else if (period == "90days")
            {
                // Weekly breakdown
                for (int i = 12; i >= 0; i--)
                {
                    var weekStart = endDate.AddDays(-i * 7).Date;
                    var weekEnd = weekStart.AddDays(7);
                    var weekSales = currentSales.Where(s => s.SaleDate >= weekStart && s.SaleDate < weekEnd).ToList();

                    revenueTrendLabels.Add($"Week {13 - i}");
                    revenueTrendData.Add(weekSales.Sum(s => s.TotalAmount));
                    ordersTrendData.Add(weekSales.Count);
                }
            }
            else // year
            {
                // Monthly breakdown
                for (int month = 1; month <= 12; month++)
                {
                    var monthSales = currentSales.Where(s => s.SaleDate.Month == month).ToList();

                    revenueTrendLabels.Add(new DateTime(endDate.Year, month, 1).ToString("MMM"));
                    revenueTrendData.Add(monthSales.Sum(s => s.TotalAmount));
                    ordersTrendData.Add(monthSales.Count);
                }
            }

            // Peak order times (by hour of day)
            var peakTimes = currentSales
                .GroupBy(s => s.SaleDate.Hour)
                .Select(g => new PeakTimeData
                {
                    TimeLabel = $"{g.Key:00}:00 - {g.Key:00}:59",
                    OrderCount = g.Count()
                })
                .OrderByDescending(p => p.OrderCount)
                .Take(5)
                .ToList();

            // Voucher performance
            var voucherUsage = currentSales.Where(s => s.AppliedVoucherId.HasValue).ToList();
            var voucherStats = new VoucherStatsData
            {
                TotalVouchersUsed = voucherUsage.Count,
                TotalDiscountAmount = voucherUsage.Sum(s => s.DiscountAmount),
                OrdersWithVouchers = voucherUsage.Count,
                VoucherUsageRate = totalOrders > 0 ? (voucherUsage.Count * 100.0m / totalOrders) : 0,
                AvgDiscountPerOrder = voucherUsage.Count > 0 ? voucherUsage.Sum(s => s.DiscountAmount) / voucherUsage.Count : 0
            };

            // Generate key insights
            var insights = new List<string>();

            // Revenue insights
            if (revenueChange > 10)
                insights.Add($"📈 Revenue grew by {revenueChange:N1}% compared to previous period - excellent growth!");
            else if (revenueChange < -10)
                insights.Add($"⚠️ Revenue declined by {Math.Abs(revenueChange):N1}% - consider promotional campaigns.");

            // Customer insights
            if (loyalCustomerPct > 20)
                insights.Add($"🌟 {loyalCustomerPct:N1}% of customers are loyal buyers (6+ orders) - strong retention!");

            if (oneTimeBuyerPct > 60)
                insights.Add($"💡 {oneTimeBuyerPct:N1}% are one-time buyers - focus on retention strategies.");

            // Product insights
            if (topProducts.Any())
            {
                var topProduct = topProducts.First();
                insights.Add($"🏆 '{topProduct.ProductName}' is your top seller, generating R{topProduct.Revenue:N2} ({topProduct.RevenuePercentage:N1}% of revenue).");
            }

            // AOV insights
            if (avgOrderValue > 500)
                insights.Add($"💰 Average order value is R{avgOrderValue:N2} - customers are buying premium products.");
            else if (avgOrderValue < 200)
                insights.Add($"💡 Average order value is R{avgOrderValue:N2} - consider upselling and product bundles.");

            // Fulfillment insights
            if (fulfillmentRate < 80)
                insights.Add($"⚠️ Only {fulfillmentRate:N1}% of orders are delivered - improve fulfillment processes.");
            else if (fulfillmentRate > 95)
                insights.Add($"✅ {fulfillmentRate:N1}% fulfillment rate - excellent delivery performance!");

            // Voucher insights
            if (voucherStats.VoucherUsageRate > 30)
                insights.Add($"🎟️ {voucherStats.VoucherUsageRate:N1}% of orders use vouchers - high promotional engagement.");

            // Category insights
            if (categoryBreakdown.Any())
            {
                var topCategory = categoryBreakdown.First();
                insights.Add($"📦 '{topCategory.Category}' category dominates with {topCategory.RevenuePercentage:N1}% of total revenue.");
            }

            // Peak time insights
            if (peakTimes.Any())
            {
                var peakTime = peakTimes.First();
                insights.Add($"⏰ Most orders occur during {peakTime.TimeLabel} - optimize inventory and staffing for peak hours.");
            }

            // Build view model
            var model = new SalesAnalyticsViewModel
            {
                Period = period,
                StartDate = startDate,
                EndDate = endDate,
                TotalRevenue = totalRevenue,
                RevenueChange = revenueChange,
                TotalOrders = totalOrders,
                OrdersChange = ordersChange,
                AverageOrderValue = avgOrderValue,
                AOVChange = aovChange,
                FulfillmentRate = fulfillmentRate,
                PendingOrders = pendingOrders,
                ConfirmedOrders = confirmedOrders,
                OutForDeliveryOrders = outForDeliveryOrders,
                DeliveredOrders = deliveredOrders,
                CancelledOrders = cancelledOrders,
                TopProducts = topProducts,
                TopCustomers = topCustomers,
                UniqueCustomers = uniqueCustomers,
                OneTimeBuyers = oneTimeBuyers,
                RepeatCustomers = repeatCustomers,
                LoyalCustomers = loyalCustomers,
                OneTimeBuyerPercentage = oneTimeBuyerPct,
                RepeatCustomerPercentage = repeatCustomerPct,
                LoyalCustomerPercentage = loyalCustomerPct,
                AvgOrdersPerCustomer = avgOrdersPerCustomer,
                CategoryBreakdown = categoryBreakdown,
                RevenueTrendLabels = revenueTrendLabels,
                RevenueTrendData = revenueTrendData,
                OrdersTrendData = ordersTrendData,
                PeakOrderTimes = peakTimes,
                VoucherStats = voucherStats,
                KeyInsights = insights
            };

            return View(model);
        }

       

        // FIXED: Remove async and ThenInclude (not needed in EF6)
        private SalesDataSummary GetCurrentSalesData()
        {
            // Get data for the current period (last 30 days)
            var endDate = DateTime.Now;
            var startDate = endDate.AddDays(-30);

            var sales = db.Sales
                .Where(s => s.SaleDate >= startDate && s.SaleDate <= endDate)
                .Include(s => s.Items)
                .Include(s => s.Items.Select(i => i.Product)) // EF6 uses nested Select instead of ThenInclude
                .ToList();

            var topProducts = sales
                .SelectMany(s => s.Items)
                .GroupBy(i => new { i.Product.Name, i.Product.Category })
                .Select(g => new TopProductSummary // Create a concrete class instead of anonymous type
                {
                    ProductName = g.Key.Name ?? "Unknown",
                    Category = g.Key.Category ?? "Uncategorized",
                    Revenue = g.Sum(i => i.Price * i.Quantity),
                    UnitsSold = g.Sum(i => i.Quantity)
                })
                .OrderByDescending(p => p.Revenue)
                .Take(10)
                .ToList();

            var categoryBreakdown = sales
                .SelectMany(s => s.Items)
                .GroupBy(i => i.Product.Category ?? "Uncategorized")
                .Select(g => new CategorySummary // Concrete class
                {
                    Category = g.Key,
                    Revenue = g.Sum(i => i.Price * i.Quantity),
                    OrderCount = g.Select(i => i.SaleId).Distinct().Count()
                })
                .OrderByDescending(c => c.Revenue)
                .ToList();

            var dailyRevenue = sales
                .GroupBy(s => s.SaleDate.Date)
                .Select(g => new DailyRevenueSummary // Concrete class
                {
                    Date = g.Key,
                    Revenue = g.Sum(s => s.TotalAmount),
                    Orders = g.Count()
                })
                .OrderBy(d => d.Date)
                .ToList();

            return new SalesDataSummary
            {
                TotalRevenue = sales.Sum(s => s.TotalAmount),
                TotalOrders = sales.Count,
                AverageOrderValue = sales.Any() ? sales.Average(s => s.TotalAmount) : 0,
                TopProducts = topProducts,
                CategoryBreakdown = categoryBreakdown,
                DailyRevenue = dailyRevenue,
                Period = "Last 30 days",
                StartDate = startDate,
                EndDate = endDate
            };
        }

        // REPLACE BOTH GetAIResponse AND AskAI methods with these SIMPLIFIED versions

        [HttpPost]
        public async Task<JsonResult> AskAI(string question)
        {
            try
            {
                if (string.IsNullOrEmpty(question))
                {
                    return Json(new { success = false, answer = "Please ask a question." });
                }

                // Get sales data
                var salesData = GetCurrentSalesData();

                // Call API
                var answer = await GetSimpleAIResponse(question, salesData);

                return Json(new { success = true, answer = answer });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AskAI ERROR: {ex.Message}");
                return Json(new
                {
                    success = false,
                    answer = "Sorry, I'm having trouble connecting. Here's what I know: " +
                            GetFallbackResponse(question, GetCurrentSalesData())
                });
            }
        }

        // MUCH SIMPLER AI METHOD - USES WORKING API
        private async Task<string> GetSimpleAIResponse(string question, SalesDataSummary salesData)
        {
            try
            {
                var apiKey = ConfigurationManager.AppSettings["HuggingFaceApiKey"];

                if (string.IsNullOrEmpty(apiKey))
                {
                    return GetFallbackResponse(question, salesData);
                }

                // Use BART summarization - this is PROVEN to work from your test
                var apiUrl = "https://api-inference.huggingface.co/models/facebook/bart-large-cnn";

                // Create a simple context for BART to summarize
                var context = $@"Sales Report for {salesData.Period}:
Total Revenue: R{salesData.TotalRevenue:N2}
Total Orders: {salesData.TotalOrders}
Average Order Value: R{salesData.AverageOrderValue:N2}

Top Products:
{string.Join("\n", salesData.TopProducts.Take(3).Select((p, i) => $"{i + 1}. {p.ProductName} - R{p.Revenue:N2} revenue, {p.UnitsSold} units sold"))}

Top Categories:
{string.Join("\n", salesData.CategoryBreakdown.Take(3).Select(c => $"- {c.Category}: R{c.Revenue:N2} revenue from {c.OrderCount} orders"))}

Customer Question: {question}

To answer this question, focus on the relevant numbers above.";

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                    client.Timeout = TimeSpan.FromSeconds(45);

                    var requestBody = new
                    {
                        inputs = context,
                        parameters = new
                        {
                            max_length = 200,
                            min_length = 30,
                            do_sample = false
                        },
                        options = new
                        {
                            wait_for_model = true,
                            use_cache = false
                        }
                    };

                    var json = JsonConvert.SerializeObject(requestBody);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(apiUrl, content);
                    var responseText = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        try
                        {
                            // BART returns an array with summary_text
                            var jsonResponse = JArray.Parse(responseText);
                            if (jsonResponse.Count > 0 && jsonResponse[0]["summary_text"] != null)
                            {
                                var summary = jsonResponse[0]["summary_text"].ToString();

                                // Enhance the summary with specific answer
                                return EnhanceAIAnswer(question, summary, salesData);
                            }
                        }
                        catch
                        {
                            // Parsing failed, try as object
                            var jsonObject = JObject.Parse(responseText);
                            if (jsonObject["summary_text"] != null)
                            {
                                var summary = jsonObject["summary_text"].ToString();
                                return EnhanceAIAnswer(question, summary, salesData);
                            }
                        }
                    }
                    else if (responseText.Contains("loading"))
                    {
                        // Model is loading - use fallback with note
                        return "🤖 AI model is warming up. Here's the data: " +
                               GetFallbackResponse(question, salesData);
                    }
                }

                // If we get here, use fallback
                return GetFallbackResponse(question, salesData);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AI Error: {ex.Message}");
                return GetFallbackResponse(question, salesData);
            }
        }

        // Helper to create better answers from AI summary
        private string EnhanceAIAnswer(string question, string aiSummary, SalesDataSummary salesData)
        {
            var lowerQuestion = question.ToLower();

            // Add specific data based on question type
            if (lowerQuestion.Contains("top product") || lowerQuestion.Contains("best"))
            {
                var topProduct = salesData.TopProducts.FirstOrDefault();
                if (topProduct != null)
                {
                    return $"🏆 {topProduct.ProductName} is your top seller with R{topProduct.Revenue:N2} in revenue from {topProduct.UnitsSold} units sold. " + aiSummary;
                }
            }

            if (lowerQuestion.Contains("revenue") || lowerQuestion.Contains("sales"))
            {
                return $"💰 Revenue for {salesData.Period}: R{salesData.TotalRevenue:N2} from {salesData.TotalOrders} orders (avg R{salesData.AverageOrderValue:N2} per order). " + aiSummary;
            }

            if (lowerQuestion.Contains("category") || lowerQuestion.Contains("categories"))
            {
                var topCat = salesData.CategoryBreakdown.FirstOrDefault();
                if (topCat != null)
                {
                    return $"📦 {topCat.Category} leads with R{topCat.Revenue:N2}. " + aiSummary;
                }
            }

            // Just return the AI summary with an emoji
            return "📊 " + aiSummary;
        }

        // Helper to clean AI response
        private string CleanAIResponse(string generatedText, string prompt)
        {
            // Remove the prompt if it's included in the response
            if (generatedText.Contains(prompt))
            {
                generatedText = generatedText.Replace(prompt, "").Trim();
            }

            // Remove common artifacts
            generatedText = generatedText
                .Replace("Answer:", "")
                .Replace("Response:", "")
                .Trim();

            // Limit length
            if (generatedText.Length > 500)
            {
                generatedText = generatedText.Substring(0, 500) + "...";
            }

            return generatedText;
        }

        [HttpGet]
        public async Task<JsonResult> TestHuggingFaceAPI()
        {
            try
            {
                var apiKey = ConfigurationManager.AppSettings["HuggingFaceApiKey"];

                System.Diagnostics.Debug.WriteLine("\n=== HUGGING FACE API TEST ===");
                System.Diagnostics.Debug.WriteLine($"1. API Key Found: {!string.IsNullOrEmpty(apiKey)}");

                if (string.IsNullOrEmpty(apiKey))
                {
                    return Json(new
                    {
                        success = false,
                        error = "No API key found in Web.config",
                        instruction = "Add <add key=\"HuggingFaceApiKey\" value=\"hf_YOUR_TOKEN\" /> to Web.config"
                    }, JsonRequestBehavior.AllowGet);
                }

                System.Diagnostics.Debug.WriteLine($"2. API Key Length: {apiKey.Length} characters");
                System.Diagnostics.Debug.WriteLine($"3. API Key Preview: {apiKey.Substring(0, Math.Min(15, apiKey.Length))}...");

                // Test with a simple, reliable model
                var testUrl = "https://api-inference.huggingface.co/models/facebook/bart-large-cnn";

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                    client.Timeout = TimeSpan.FromSeconds(30);

                    var testRequest = new
                    {
                        inputs = "The tower is 324 meters tall. It was built in 1889.",
                        parameters = new
                        {
                            max_length = 50
                        },
                        options = new
                        {
                            wait_for_model = true
                        }
                    };

                    var jsonContent = JsonConvert.SerializeObject(testRequest);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    System.Diagnostics.Debug.WriteLine($"4. Sending test request to: {testUrl}");

                    var response = await client.PostAsync(testUrl, content);
                    var responseString = await response.Content.ReadAsStringAsync();

                    System.Diagnostics.Debug.WriteLine($"5. Response Status: {response.StatusCode}");
                    System.Diagnostics.Debug.WriteLine($"6. Response Body: {responseString}");

                    if (response.IsSuccessStatusCode)
                    {
                        return Json(new
                        {
                            success = true,
                            message = "✅ API key is valid and working!",
                            statusCode = (int)response.StatusCode,
                            response = responseString
                        }, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        var errorMsg = "API request failed";

                        if (responseString.Contains("401") || responseString.Contains("unauthorized"))
                        {
                            errorMsg = "❌ Invalid API key! Generate a new token at https://huggingface.co/settings/tokens";
                        }
                        else if (responseString.Contains("rate limit"))
                        {
                            errorMsg = "⏱️ Rate limited. Wait a moment and try again.";
                        }
                        else if (responseString.Contains("loading"))
                        {
                            errorMsg = "⏳ Model is loading. Wait 20 seconds and try again.";
                        }

                        return Json(new
                        {
                            success = false,
                            error = errorMsg,
                            statusCode = (int)response.StatusCode,
                            details = responseString
                        }, JsonRequestBehavior.AllowGet);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ TEST ERROR: {ex.Message}");
                return Json(new
                {
                    success = false,
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                }, JsonRequestBehavior.AllowGet);
            }
        }


        // Fallback responses for when API is not available
        private string GetFallbackResponse(string question, SalesDataSummary salesData)
        {
            var lowerQuestion = question.ToLower();

            if (lowerQuestion.Contains("top product") || lowerQuestion.Contains("best selling"))
            {
                var topProduct = salesData.TopProducts.FirstOrDefault();
                return topProduct != null ?
                    $"📊 Your top product is **{topProduct.ProductName}** with R {topProduct.Revenue:N2} revenue from {topProduct.UnitsSold} units sold in the {salesData.Period}." :
                    "No product sales data available for the period.";
            }

            if (lowerQuestion.Contains("revenue") || lowerQuestion.Contains("sales") || lowerQuestion.Contains("performance"))
            {
                return $"💰 **{salesData.Period} Performance:**\n- Total Revenue: R {salesData.TotalRevenue:N2}\n- Total Orders: {salesData.TotalOrders}\n- Average Order Value: R {salesData.AverageOrderValue:N2}";
            }

            if (lowerQuestion.Contains("category") || lowerQuestion.Contains("type"))
            {
                var topCategory = salesData.CategoryBreakdown.FirstOrDefault();
                return topCategory != null ?
                    $"📦 Your best performing category is **{topCategory.Category}** with R {topCategory.Revenue:N2} revenue from {topCategory.OrderCount} orders." :
                    "No category data available.";
            }

            if (lowerQuestion.Contains("trend") || lowerQuestion.Contains("growth"))
            {
                // Fix: Use Skip instead of TakeLast (not available in .NET Framework 4.x)
                var totalDays = salesData.DailyRevenue.Count;
                var recentDays = salesData.DailyRevenue.Skip(Math.Max(0, totalDays - 7)).ToList();

                if (recentDays.Count >= 2)
                {
                    var avgRecent = recentDays.Average(d => d.Revenue);
                    var totalOrders = recentDays.Sum(d => d.Orders);
                    return $"📈 Recent trend: Last {recentDays.Count} days averaged R {avgRecent:N2} per day with {totalOrders} total orders.";
                }
                return $"Based on {salesData.Period} data, you had {salesData.TotalOrders} orders generating R {salesData.TotalRevenue:N2} in revenue.";
            }

            if (lowerQuestion.Contains("customer"))
            {
                return $"👥 Based on {salesData.TotalOrders} orders in {salesData.Period}, customers are spending an average of R {salesData.AverageOrderValue:N2} per order.";
            }

            return "💡 I can help analyze:\n- Top products and categories\n- Revenue and sales trends\n- Customer behavior\n- Growth patterns\n\nTry asking: 'What are my top products?' or 'How is revenue trending?'";
        }

    }
}