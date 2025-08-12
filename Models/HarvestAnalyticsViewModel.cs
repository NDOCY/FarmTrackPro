using System;
using System.ComponentModel.DataAnnotations;

namespace FarmTrack.Models
{
    public class HarvestAnalyticsViewModel
    {
        public int PlotCropId { get; set; }

        [Display(Name = "Crop Name")]
        public string CropName { get; set; }

        [Display(Name = "Plot Name")]
        public string PlotName { get; set; }

        [Display(Name = "Expected Yield (kg/ha)")]
        public double ExpectedYield { get; set; }

        [Display(Name = "Actual Yield (kg)")]
        public double ActualYield { get; set; }

        [Display(Name = "Yield Difference (kg)")]
        public double YieldDifference { get; set; }

        [Display(Name = "Losses (kg)")]
        public double LossKg { get; set; }

        [Display(Name = "Loss Percentage (%)")]
        public double LossPercentage { get; set; }

        [Display(Name = "Days From Planting to Harvest")]
        public int DaysFromPlantingToHarvest { get; set; }

        [Display(Name = "Loss Reason")]
        public string LossReason { get; set; }
    }
}
