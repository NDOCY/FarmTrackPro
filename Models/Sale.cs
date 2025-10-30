using FarmTrack.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmTrack.Models
{
    public class Sale
    {
        public int SaleId { get; set; }

        [Required]
        public DateTime SaleDate { get; set; } = DateTime.Now;

        [Required]
        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = "Pending";
        public string TrackingNumber { get; set; }
        public DateTime? EstimatedDelivery { get; set; }

        // Customer Information
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPhone { get; set; }
        public string DeliveryAddress { get; set; }

        // Coordinates
        public decimal? DestinationLatitude { get; set; }
        public decimal? DestinationLongitude { get; set; }

        // Payment Information
        public string PaymentMethod { get; set; }
        public string PaymentStatus { get; set; } = "Pending";
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        // Delivery Assignment & Tracking
        public int? AssignedDriverId { get; set; }
        public string DeliveryDriver { get; set; }
        public string DriverPhone { get; set; }
        public string VehicleType { get; set; }
        public string VehicleNumber { get; set; }

        // Real-time Location Tracking
        public decimal? CurrentLatitude { get; set; }
        public decimal? CurrentLongitude { get; set; }
        public DateTime? LastLocationUpdate { get; set; }
        public bool IsActiveDelivery { get; set; }

        // Discount Voucher Properties
        public int? AppliedVoucherId { get; set; }
        public string AppliedVoucherCode { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal Subtotal { get; set; }

        // Delivery Verification Properties
        public string DeliveryVerificationCode { get; set; }
        public DateTime? DeliveryCodeGeneratedAt { get; set; }
        public string DeliveryCodeStatus { get; set; } // Generated, Used, Expired
        public DateTime? DeliveryVerifiedAt { get; set; }
        public int? DeliveryVerifiedByDriverId { get; set; }

        // ✅ NEW: Shipping Fee
        public decimal ShippingFee { get; set; }

        [ForeignKey("AppliedVoucherId")]
        public virtual DiscountVoucher AppliedVoucher { get; set; }

        [ForeignKey("AssignedDriverId")]
        public virtual User AssignedDriver { get; set; }


        public virtual ICollection<DeliveryLocation> DeliveryLocations { get; set; }
        public virtual ICollection<SaleItem> Items { get; set; }
        public virtual ICollection<OrderStatusUpdate> OrderStatusUpdates { get; set; }
    }

    public class OrderStatusUpdate
    {
        public int OrderStatusUpdateId { get; set; }
        public int SaleId { get; set; }

        [Required]
        public string Status { get; set; }

        public DateTime UpdateTime { get; set; } = DateTime.Now;
        public string Notes { get; set; }

        public virtual Sale Sale { get; set; }
    }

    public class SaleItem
    {
        public int SaleItemId { get; set; }

        [Required]
        public int SaleId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public decimal Price { get; set; }

        public virtual Sale Sale { get; set; }
        public virtual Product Product { get; set; }
    }

    public class DeliveryLocation
    {
        public int DeliveryLocationId { get; set; }
        public int SaleId { get; set; }
        public int? DriverUserId { get; set; }

        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public int Sequence { get; set; }

        public virtual Sale Sale { get; set; }
        [ForeignKey("DriverUserId")]
        public virtual User DriverUser { get; set; }
    }

    public class DeliveryAssignment
    {
        public int DeliveryAssignmentId { get; set; }
        public int SaleId { get; set; }
        public int DriverUserId { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Assigned";

        public virtual Sale Sale { get; set; }
        [ForeignKey("DriverUserId")]
        public virtual User DriverUser { get; set; }
    }

    public class DeliveryVerification
    {
        public int Id { get; set; }
        public int SaleId { get; set; }
        public virtual Sale Sale { get; set; }

        public int? DriverUserId { get; set; }
        public virtual User Driver { get; set; }

        public int? CustomerUserId { get; set; }
        public virtual User Customer { get; set; }

        public string VerificationCode { get; set; }
        public string VerificationMethod { get; set; } // Code, CustomerApp, Photo, etc.
        public DateTime VerifiedAt { get; set; }
        public bool IsSuccessful { get; set; }
        public string Notes { get; set; }
    }

    // Helper classes for the summary data
    public class TopProductSummary
    {
        public string ProductName { get; set; }
        public string Category { get; set; }
        public decimal Revenue { get; set; }
        public int UnitsSold { get; set; }
    }

    public class CategorySummary
    {
        public string Category { get; set; }
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }

    public class DailyRevenueSummary
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int Orders { get; set; }
    }

    public class SalesDataSummary
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
        public List<TopProductSummary> TopProducts { get; set; }
        public List<CategorySummary> CategoryBreakdown { get; set; }
        public List<DailyRevenueSummary> DailyRevenue { get; set; }
        public string Period { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class OpenAIResponse
    {
        public List<OpenAIChoice> choices { get; set; }
    }

    public class OpenAIChoice
    {
        public OpenAIMessage message { get; set; }
    }

    public class OpenAIMessage
    {
        public string content { get; set; }
    }

    public class AIRequest
    {
        public string question { get; set; }
    }
}