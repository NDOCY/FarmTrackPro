using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FarmTrack.Models
{
    public class EmergencyDashboardViewModel
    {
        public List<EmergencyContact> EmergencyContacts { get; set; }
        public List<CareGuideline> CareGuidelines { get; set; }
        public List<HealthCheckSchedule> RecentEmergencies { get; set; }
    }
}