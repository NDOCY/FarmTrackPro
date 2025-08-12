using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FarmTrack.Models
{
    public class HarvestOutcomeViewModel
    {
        public int PlotCropId { get; set; }
        public string CropName { get; set; }
        public string PlotName { get; set; }

        [Display(Name = "Harvest Date")]
        public DateTime HarvestDate { get; set; }

        [Display(Name = "Actual Yield (kg)")]
        [Required]
        public double ActualYieldKg { get; set; }

        [Display(Name = "Losses (kg)")]
        public double? LossesKg { get; set; }

        [Display(Name = "Quality Grade")]
        public string QualityGrade { get; set; }

        public string Notes { get; set; }
    }

}