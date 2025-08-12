using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        public double ActualYieldKg { get; set; }

        [Display(Name = "Losses (kg)")]
        [Range(0, 100000, ErrorMessage = "Losses must be between 0 and 100,000 kg")]
        public double? LossesKg { get; set; }

        [Display(Name = "Quality Grade")]
        [StringLength(50)]
        public string QualityGrade { get; set; } // e.g., Excellent, Good, Average, Poor

        [StringLength(1000)]
        public string Notes { get; set; }

        [Display(Name = "Recorded On")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Last Updated")]
        public DateTime? UpdatedAt { get; set; }
    }
}
