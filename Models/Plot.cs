using FarmPro.Models;
using System;
//using FarmTrackPro.Models; 
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmTrack.Models
{
    public enum PlotStatus
    {
        Idle,              // Default
        UnderPreparation,  // Crop assigned, prepping
        ReadyForPlanting,  // All prep tasks done
        Planted,           // Planting started
        Harvested          // After harvest
    }

    public class Plot
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        public int? CropId { get; set; }
        [ForeignKey("CropId")]
        public virtual Crop Crop { get; set; }

        [Required]
        public string SoilType { get; set; }

        public string IrrigationMethod { get; set; }
        public string FertilizerType { get; set; }

        [Required]
        public double SizeInHectares { get; set; }

        [DataType(DataType.Date)]
        public DateTime? PlantingDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? MaturityDate { get; set; }

        [Required]
        
        [StringLength(50)]
        public string Status { get; set; }


        [Required]
        public string Coordinates { get; set; }

        [DataType(DataType.Date)]
        public DateTime? LastInspectionDate { get; set; }

        public int? IrrigationFrequency { get; set; }

        [StringLength(1000)]
        public string Notes { get; set; }

        public virtual ICollection<PlotCrop> PlotCrops { get; set; }
    }

}
