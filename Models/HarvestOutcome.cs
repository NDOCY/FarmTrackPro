using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace FarmTrack.Models
{
    [Table("HarvestOutcomes")]
    public class HarvestOutcome
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("PlotCrop")]
        public int PlotCropId { get; set; }

        public virtual PlotCrop PlotCrop { get; set; }

        [Required(ErrorMessage = "Harvest date is required")]
        [Display(Name = "Harvest Date")]
        [DataType(DataType.Date)]
        public DateTime HarvestDate { get; set; }

        [Required(ErrorMessage = "Actual yield is required")]
        [Display(Name = "Actual Yield (kg)")]
        [Range(0, 100000, ErrorMessage = "Yield must be between 0 and 100,000 kg")]
        public double ActualYieldKg { get; set; } // Keep this as before

        [Display(Name = "Losses (kg)")]
        [Range(0, 100000, ErrorMessage = "Losses must be between 0 and 100,000 kg")]
        public double? LossesKg { get; set; }

        [Display(Name = "Quality Grade")]
        [StringLength(50)]
        public string QualityGrade { get; set; }

        [StringLength(1000)]
        public string Notes { get; set; }

        [Display(Name = "Recorded On")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Last Updated")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation property for grades - NEW
        public virtual ICollection<HarvestGrade> HarvestGrades { get; set; } = new List<HarvestGrade>();

        // Calculated property for total from grades (optional)
        [NotMapped]
        public double TotalFromGrades => HarvestGrades?.Sum(g => g.QuantityKg) ?? 0;
    }

    public class HarvestGrade
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int HarvestOutcomeId { get; set; }
        [ForeignKey("HarvestOutcomeId")]
        public virtual HarvestOutcome HarvestOutcome { get; set; }

        [Required]
        [StringLength(20)]
        public string GradeName { get; set; } // "Grade A", "Grade B", "Grade C"

        [Required]
        [Range(0, 10000)]
        public double QuantityKg { get; set; }

        public string Notes { get; set; }
    }
}
