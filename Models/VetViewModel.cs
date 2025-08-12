using FarmTrack.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FarmTrack.Models
{
    public class VeterinarianStatsViewModel
    {
        public int TotalAppointments { get; set; }
        public int CompletedAppointments { get; set; }
        public int PendingAppointments { get; set; }
        public decimal TotalCost { get; set; }
        public decimal AverageRating { get; set; }
        public DateTime? LastAppointment { get; set; }

        public decimal CompletionRate => TotalAppointments > 0 ?
            (decimal)CompletedAppointments / TotalAppointments * 100 : 0;
    }

    public class VeterinarianAnalyticsViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Specialization { get; set; }
        public int TotalAppointments { get; set; }
        public int CompletedAppointments { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageCost { get; set; }
        public DateTime? LastAppointment { get; set; }

        public decimal CompletionRate => TotalAppointments > 0 ?
            (decimal)CompletedAppointments / TotalAppointments * 100 : 0;
    }

    public class MonthlyStatsViewModel
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int AppointmentCount { get; set; }
        public decimal TotalCost { get; set; }

        public string MonthName => new DateTime(Year, Month, 1).ToString("MMM yyyy");
    }

}