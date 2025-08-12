using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FarmTrack.Models
{
    public class CompleteHealthCheckViewModel
    {
        public int ScheduleId { get; set; }
        public string CheckType { get; set; }
        public DateTime ScheduledDate { get; set; }

        public List<LivestockHealthResult> LivestockResults { get; set; }


        public decimal? ActualCost { get; set; }
        public string VetInstructions { get; set; }
        public bool RequiresFollowUp { get; set; }
        public DateTime? FollowUpDate { get; set; }
    }

    public class LivestockHealthResult
    {
        public int LivestockId { get; set; }
        public string TagNumber { get; set; }
        public string Type { get; set; }

        public bool IsSick { get; set; }
        public string Notes { get; set; }
    }

}