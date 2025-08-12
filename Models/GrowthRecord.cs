using System;
using System.ComponentModel.DataAnnotations;

namespace FarmTrack.Models
{
    public class GrowthRecord
    {
        public int Id { get; set; }

        [Required]
        public int PlotCropId { get; set; }

        [Required]
        public DateTime DateRecorded { get; set; }

        [StringLength(200)]
        public string Stage { get; set; } // e.g. Germination, Flowering

        [StringLength(500)]
        public string Notes { get; set; }

        public virtual PlotCrop PlotCrop { get; set; }
    }
}
