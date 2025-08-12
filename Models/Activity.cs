using FarmPro.Models;
using FarmTrack.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FarmTrack.Models
{
    public class Activity
    {
        public int Id { get; set; }
        public int PlotCropId { get; set; }
        public string ActivityType { get; set; } // "Pest", "Disease", "Fertilization", "Weeding", "GrowthRecording", etc.
        public DateTime ActivityDate { get; set; }
        public string Description { get; set; }
        public string Notes { get; set; }
        public decimal? AmountUsed { get; set; }
        public string Unit { get; set; }
        public string ProductName { get; set; } // For pesticides, fertilizers, etc.
        public string Severity { get; set; } // For pests/diseases
        public string GrowthStage { get; set; } // For growth recording
        public string ImagePath { get; set; } // Add this line
        public decimal? MeasurementValue { get; set; } // For growth recording

        public virtual PlotCrop PlotCrop { get; set; }
        public virtual ICollection<Tag> Tags { get; set; }
    }
}