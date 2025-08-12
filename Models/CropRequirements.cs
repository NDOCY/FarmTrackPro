using FarmTrack.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FarmTrack.Models
{
    public class CropRequirements
    {
        //[Key]
        //[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        //public int Id { get; set; }

        //[ForeignKey("Crop")]
        [Key, ForeignKey("Crop")]
        public int CropId { get; set; }

        public virtual Crop Crop { get; set; }

        [StringLength(100)]
        [Display(Name = "Scientific Name")]
        public string ScientificName { get; set; }

        [StringLength(30)]
        [Display(Name = "Crop Type")]
        public string Type { get; set; }

        [StringLength(30)]
        [Display(Name = "Optimal Planting Season")]
        public string PlantingSeason { get; set; }

        [Range(1, 365)]
        [Display(Name = "Average Growth Duration (days)")]
        public int? GrowthDurationDays { get; set; }

        [Display(Name = "Expected Yield per Hectare (kg)")]
        [Range(0, 100000)]
        public double? ExpectedYieldKgPerHectare { get; set; }

        [StringLength(100)]
        [Display(Name = "Preferred Soil Type")]
        public string PreferredSoil { get; set; }

        public string GrowthDuration { get; set; }

        public string MinTemperature { get; set; }

        [StringLength(100)]
        [Display(Name = "Common Pests & Diseases")]
        public string CommonPestsDiseases { get; set; }

        [StringLength(1000)]
        [Display(Name = "Notes / Special Instructions")]
        public string Notes { get; set; }

        // Optional: track source of data
        [StringLength(100)]
        [Display(Name = "Trifle API")]
        public string Source { get; set; } = "Trifle API"; // e.g., "Trefle API"
                                                           // ✅ Add this if missing
        public virtual CropRequirements CropRequirement { get; set; }
    }

}