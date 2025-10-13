using FarmTrack.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace FarmTrack.Controllers
{
    public class DiscountVouchersController : Controller
    {
        private FarmTrackContext db = new FarmTrackContext();

        // GET: Admin - Voucher Management
        public ActionResult Index()
        {
            if (Session["Role"]?.ToString() != "Admin" && Session["Role"]?.ToString() != "Owner")
            {
                TempData["Error"] = "Access denied. Admin privileges required.";
                return RedirectToAction("Index", "Home");
            }

            var vouchers = db.DiscountVouchers
                .Include(v => v.ApplicableProduct)
                .OrderByDescending(v => v.ValidFrom)
                .ToList();

            return View(vouchers);
        }

        // GET: Create Voucher
        public ActionResult Create()
        {
            if (Session["Role"]?.ToString() != "Admin" && Session["Role"]?.ToString() != "Owner")
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("Index");
            }

            PopulateViewBagData();
            return View();
        }

        // POST: Create Voucher
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(DiscountVoucher voucher)
        {
            if (Session["Role"]?.ToString() != "Admin" && Session["Role"]?.ToString() != "Owner")
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("Index");
            }

            // Generate unique code if not provided
            if (string.IsNullOrWhiteSpace(voucher.Code))
            {
                voucher.Code = GenerateVoucherCode();
            }
            else
            {
                // Normalize and check if code already exists
                voucher.Code = voucher.Code.Trim().ToUpper();
                if (db.DiscountVouchers.Any(v => v.Code == voucher.Code))
                {
                    ModelState.AddModelError("Code", "This voucher code already exists.");
                }
            }

            // Validate dates
            if (voucher.ValidTo <= voucher.ValidFrom)
            {
                ModelState.AddModelError("ValidTo", "Valid To date must be after Valid From date.");
            }

            // Validate percentage voucher
            if (voucher.VoucherType == VoucherType.Percentage && voucher.DiscountValue > 100)
            {
                ModelState.AddModelError("DiscountValue", "Percentage discount cannot exceed 100%.");
            }

            // Validate discount value
            if (voucher.DiscountValue <= 0)
            {
                ModelState.AddModelError("DiscountValue", "Discount value must be greater than 0.");
            }

            // Clear applicability fields based on selection
            switch (voucher.Applicability)
            {
                case VoucherApplicability.AllProducts:
                case VoucherApplicability.MinimumOrderOnly:
                    voucher.ApplicableCategory = null;
                    voucher.ApplicableProductId = null;
                    break;
                case VoucherApplicability.SpecificCategory:
                    voucher.ApplicableProductId = null;
                    if (string.IsNullOrWhiteSpace(voucher.ApplicableCategory))
                    {
                        ModelState.AddModelError("ApplicableCategory", "Please select a category.");
                    }
                    break;
                case VoucherApplicability.SpecificProduct:
                    voucher.ApplicableCategory = null;
                    if (!voucher.ApplicableProductId.HasValue)
                    {
                        ModelState.AddModelError("ApplicableProductId", "Please select a product.");
                    }
                    break;
            }

            if (ModelState.IsValid)
            {
                try
                {
                    voucher.IsActive = true;
                    voucher.UsedCount = 0;

                    db.DiscountVouchers.Add(voucher);
                    db.SaveChanges();

                    TempData["Success"] = $"Voucher '{voucher.Code}' created successfully!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error creating voucher: " + ex.Message);
                }
            }

            // Repopulate ViewBag data when returning to view
            PopulateViewBagData(voucher.ApplicableProductId);
            return View(voucher);
        }

        // GET: Edit Voucher
        public ActionResult Edit(int? id)
        {
            if (Session["Role"]?.ToString() != "Admin" && Session["Role"]?.ToString() != "Owner")
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("Index");
            }

            if (id == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            var voucher = db.DiscountVouchers.Find(id);
            if (voucher == null)
            {
                return HttpNotFound();
            }

            PopulateViewBagData(voucher.ApplicableProductId);
            return View(voucher);
        }

        // POST: Edit Voucher
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(DiscountVoucher voucher)
        {
            if (Session["Role"]?.ToString() != "Admin" && Session["Role"]?.ToString() != "Owner")
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("Index");
            }

            // Validate dates
            if (voucher.ValidTo <= voucher.ValidFrom)
            {
                ModelState.AddModelError("ValidTo", "Valid To date must be after Valid From date.");
            }

            // Validate percentage voucher
            if (voucher.VoucherType == VoucherType.Percentage && voucher.DiscountValue > 100)
            {
                ModelState.AddModelError("DiscountValue", "Percentage discount cannot exceed 100%.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    db.Entry(voucher).State = EntityState.Modified;
                    db.SaveChanges();

                    TempData["Success"] = $"Voucher '{voucher.Code}' updated successfully!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error updating voucher: " + ex.Message);
                }
            }

            PopulateViewBagData(voucher.ApplicableProductId);
            return View(voucher);
        }

        // Toggle Voucher Status
        [HttpPost]
        public JsonResult ToggleStatus(int id)
        {
            if (Session["Role"]?.ToString() != "Admin" && Session["Role"]?.ToString() != "Owner")
            {
                return Json(new { success = false, message = "Access denied" });
            }

            try
            {
                var voucher = db.DiscountVouchers.Find(id);
                if (voucher == null)
                {
                    return Json(new { success = false, message = "Voucher not found" });
                }

                voucher.IsActive = !voucher.IsActive;
                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = $"Voucher {(voucher.IsActive ? "activated" : "deactivated")}",
                    isActive = voucher.IsActive
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating voucher: " + ex.Message });
            }
        }

        // Validate Voucher (AJAX endpoint)
        [HttpPost]
        public JsonResult ValidateVoucher(string code, decimal cartTotal, int? productId = null, string category = null)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    return Json(new { success = false, message = "Please enter a voucher code" });
                }

                var voucher = db.DiscountVouchers
                    .FirstOrDefault(v => v.Code.ToUpper() == code.ToUpper() && v.IsActive);

                if (voucher == null)
                {
                    return Json(new { success = false, message = "Invalid voucher code" });
                }

                // Check validity
                if (!voucher.IsValid)
                {
                    return Json(new { success = false, message = "This voucher has expired or reached its usage limit" });
                }

                // Check minimum order amount
                if (voucher.MinimumOrderAmount.HasValue && cartTotal < voucher.MinimumOrderAmount.Value)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Minimum order amount of R{voucher.MinimumOrderAmount.Value:0.##} required"
                    });
                }

                // Check applicability
                if (!IsVoucherApplicable(voucher, productId, category))
                {
                    return Json(new { success = false, message = "This voucher is not applicable to your cart" });
                }

                // Calculate discount
                var discount = CalculateDiscount(voucher, cartTotal);

                return Json(new
                {
                    success = true,
                    message = $"Voucher applied! Discount: {voucher.DisplayValue}",
                    discountAmount = discount,
                    newTotal = cartTotal - discount,
                    voucherType = voucher.VoucherType.ToString(),
                    voucherDescription = voucher.Description
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error validating voucher: " + ex.Message });
            }
        }

        // Helper method to populate ViewBag data
        private void PopulateViewBagData(int? selectedProductId = null)
        {
            var products = db.Products.Where(p => p.IsAvailable).ToList();
            ViewBag.ApplicableProductId = new SelectList(products, "Id", "Name", selectedProductId);

            var categories = db.Products
                .Where(p => !string.IsNullOrEmpty(p.Category))
                .Select(p => p.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            ViewBag.Categories = categories ?? new List<string>();
        }

        private bool IsVoucherApplicable(DiscountVoucher voucher, int? productId, string category)
        {
            switch (voucher.Applicability)
            {
                case VoucherApplicability.AllProducts:
                    return true;
                case VoucherApplicability.SpecificCategory:
                    return category == voucher.ApplicableCategory;
                case VoucherApplicability.SpecificProduct:
                    return productId == voucher.ApplicableProductId;
                case VoucherApplicability.MinimumOrderOnly:
                    return true;
                default:
                    return true;
            }
        }

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

        private string GenerateVoucherCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            string code;

            do
            {
                code = new string(Enumerable.Repeat(chars, 8)
                    .Select(s => s[random.Next(s.Length)]).ToArray());
            } while (db.DiscountVouchers.Any(v => v.Code == code));

            return code;
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