using System;
using System.Collections.Generic;

namespace FarmTrack.Models
{
    public class MyOrdersViewModel
    {
        public List<OrderSummary> Orders { get; set; }
        public string FilterStatus { get; set; } // For filtering by status
    }

    public class OrderSummary
    {
        public int SaleId { get; set; }
        public DateTime SaleDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public string TrackingNumber { get; set; }
        public int ItemCount { get; set; }
        public DateTime? EstimatedDelivery { get; set; }
        public string CustomerName { get; set; }
        public List<OrderItem> Items { get; set; }
    }

    public class OrderItem
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total => Price * Quantity;
    }
}