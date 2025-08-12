using FarmTrack.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FarmTrack.Models
{
    public class PlotCrop
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PlotId { get; set; }
        [ForeignKey("PlotId")]
        public virtual Plot Plot { get; set; }

        [Required]
        public int CropId { get; set; }
        [ForeignKey("CropId")]
        public virtual Crop Crop { get; set; }

        public DateTime DateAssigned { get; set; } = DateTime.Now;
        public DateTime? DatePlanted { get; set; }
        public DateTime? ExpectedMaturityDate { get; set; }
        public DateTime? HarvestDate { get; set; }

        public double ExpectedYield { get; set; } // in kg/hectare
        public double? ActualYield { get; set; }

        public string Status { get; set; } = "Preparation"; // Preparation, Planting, Growing, Harvested

        // Navigation properties
        public virtual ICollection<FarmTask> Tasks { get; set; }
        public virtual ICollection<GrowthRecord> GrowthRecords { get; set; }
        public virtual ICollection<HarvestOutcome> HarvestOutcomes { get; set; }
    }
}