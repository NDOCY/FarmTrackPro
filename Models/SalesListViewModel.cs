using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FarmTrack.Models
{
    public class SalesListViewModel
    {
        public List<SaleSummary> Sales { get; set; }
        public SalesFilter Filter { get; set; }
        public SalesStats Stats { get; set; }
    }

    public class SaleSummary
    {
        public int SaleId { get; set; }
        public DateTime SaleDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPhone { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentStatus { get; set; }
        public string TrackingNumber { get; set; }
        public DateTime? EstimatedDelivery { get; set; }
        public int ItemCount { get; set; }
        public string UserName { get; set; }
        public int UserId { get; set; }

        // ADD THESE DELIVERY TRACKING PROPERTIES:
        public string DeliveryDriver { get; set; }
        public string DriverPhone { get; set; }
        public string VehicleType { get; set; }
        public string VehicleNumber { get; set; }
        public DateTime? LastLocationUpdate { get; set; }
        public bool IsActiveDelivery { get; set; }
        public int? AssignedDriverId { get; set; }
    }

    public class SalesFilter
    {
        public string Status { get; set; }
        public string PaymentStatus { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string SearchTerm { get; set; }
    }

    public class SalesStats
    {
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int PendingOrders { get; set; }
        public int DeliveredOrders { get; set; }
    }
}