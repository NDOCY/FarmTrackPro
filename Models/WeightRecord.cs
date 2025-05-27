using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmTrack.Models
{
    public class WeightRecord
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int WeightRecordId { get; set; }

        [Required]
        public int LivestockId { get; set; }

        [ForeignKey("LivestockId")]
        public virtual Livestock Livestock { get; set; }

        [Required]
        [Range(0, 2000)]
        public double Weight { get; set; } // in kg

        [Required]
        public DateTime RecordedAt { get; set; } = DateTime.Now;

        public string Notes { get; set; }
    }
}
