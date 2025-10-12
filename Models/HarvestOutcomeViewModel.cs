using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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

        // Add expected yield for validation
        [Display(Name = "Expected Yield (kg)")]
        public double ExpectedYield { get; set; }

        [Display(Name = "Actual Yield (kg)")]
        [Required]
        public double ActualYieldKg { get; set; }

        // Grade inputs
        [Required(ErrorMessage = "Grade A quantity is required")]
        [Range(0, 10000, ErrorMessage = "Quantity must be positive")]
        [Display(Name = "Grade A (kg)")]
        public double GradeAQty { get; set; }

        [Required(ErrorMessage = "Grade B quantity is required")]
        [Range(0, 10000, ErrorMessage = "Quantity must be positive")]
        [Display(Name = "Grade B (kg)")]
        public double GradeBQty { get; set; }

        [Required(ErrorMessage = "Grade C quantity is required")]
        [Range(0, 10000, ErrorMessage = "Quantity must be positive")]
        [Display(Name = "Grade C (kg)")]
        public double GradeCQty { get; set; }

        [Display(Name = "Grade A Notes")]
        public string GradeANotes { get; set; }

        [Display(Name = "Grade B Notes")]
        public string GradeBNotes { get; set; }

        [Display(Name = "Grade C Notes")]
        public string GradeCNotes { get; set; }

        [Display(Name = "Losses (kg)")]
        public double? LossesKg { get; set; }

        [Display(Name = "Quality Grade")]
        public string QualityGrade { get; set; }

        public string Notes { get; set; }

        // Calculated total for validation
        [NotMapped]
        public double TotalFromGrades => GradeAQty + GradeBQty + GradeCQty;
    }

}