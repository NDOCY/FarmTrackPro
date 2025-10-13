using System;
using System.Collections.Generic;

namespace FarmTrack.Models
{
    public class SalesAnalyticsViewModel
    {
        // Period Info
        public string Period { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // Key Metrics
        public decimal TotalRevenue { get; set; }
        public decimal RevenueChange { get; set; }
        public int TotalOrders { get; set; }
        public decimal OrdersChange { get; set; }
        public decimal AverageOrderValue { get; set; }
        public decimal AOVChange { get; set; }
        public decimal FulfillmentRate { get; set; }

        // Order Status Counts
        public int PendingOrders { get; set; }
        public int ConfirmedOrders { get; set; }
        public int OutForDeliveryOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int CancelledOrders { get; set; }

        // Product Performance
        public List<TopProductData> TopProducts { get; set; }

        // Customer Analytics
        public List<TopCustomerData> TopCustomers { get; set; }
        public int UniqueCustomers { get; set; }
        public int OneTimeBuyers { get; set; }
        public int RepeatCustomers { get; set; }
        public int LoyalCustomers { get; set; }
        public decimal OneTimeBuyerPercentage { get; set; }
        public decimal RepeatCustomerPercentage { get; set; }
        public decimal LoyalCustomerPercentage { get; set; }
        public decimal AvgOrdersPerCustomer { get; set; }

        // Category Breakdown
        public List<CategoryData> CategoryBreakdown { get; set; }

        // Trend Data for Charts
        public List<string> RevenueTrendLabels { get; set; }
        public List<decimal> RevenueTrendData { get; set; }
        public List<int> OrdersTrendData { get; set; }

        // Peak Times
        public List<PeakTimeData> PeakOrderTimes { get; set; }

        // Voucher Performance
        public VoucherStatsData VoucherStats { get; set; }

        // Key Insights
        public List<string> KeyInsights { get; set; }
    }

    public class TopProductData
    {
        public string ProductName { get; set; }
        public string Category { get; set; }
        public int UnitsSold { get; set; }
        public decimal Revenue { get; set; }
        public decimal RevenuePercentage { get; set; }
    }

    public class TopCustomerData
    {
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public int OrderCount { get; set; }
        public decimal TotalSpent { get; set; }
    }

    public class CategoryData
    {
        public string Category { get; set; }
        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }
        public decimal RevenuePercentage { get; set; }
    }

    public class PeakTimeData
    {
        public string TimeLabel { get; set; }
        public int OrderCount { get; set; }
    }

    public class VoucherStatsData
    {
        public int TotalVouchersUsed { get; set; }
        public decimal TotalDiscountAmount { get; set; }
        public int OrdersWithVouchers { get; set; }
        public decimal VoucherUsageRate { get; set; }
        public decimal AvgDiscountPerOrder { get; set; }
    }
}