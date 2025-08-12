using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FarmTrack.Models
{
    // ViewModels/ActivityViewModel.cs
    public class ActivityViewModel
    {
        public int PlotCropId { get; set; }
        [Required]
        [Display(Name = "Activity Type")]
        public string ActivityType { get; set; }

        [Required]
        [Display(Name = "Activity Date")]
        public DateTime ActivityDate { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "Upload Image")]
        public HttpPostedFileBase ImageFile { get; set; }

        [Display(Name = "Notes")]
        public string Notes { get; set; }

        [Display(Name = "Amount Used")]
        public decimal? AmountUsed { get; set; }

        [Display(Name = "Unit (kg/ml/etc.)")]
        public string Unit { get; set; }

        [Display(Name = "Product Name / Treatment")]
        public string ProductName { get; set; }

        [Display(Name = "Tags")]
        public List<int> SelectedTagIds { get; set; }
        public MultiSelectList AvailableTags { get; set; }

        [Display(Name = "Severity Level")]
        public string Severity { get; set; }

        [Display(Name = "Growth Stage")]
        public string GrowthStage { get; set; }

        [Display(Name = "Measurement (cm/kg)")]
        public decimal? MeasurementValue { get; set; }

    }

    // ViewModels/ActivityStatsViewModel.cs
    public class ActivityStatsViewModel
    {
        public int TotalActivities { get; set; }
        public int PestCount { get; set; }
        public int DiseaseCount { get; set; }
        public Activity LastFertilization { get; set; }
    }
}