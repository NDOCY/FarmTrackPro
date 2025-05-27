using FarmTrack.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace FarmTrack.Models
{
    public class HealthRecord
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int HealthRecordId { get; set; }

        [Required]
        public int LivestockId { get; set; }


        [Required]
        public string EventType { get; set; } // birth, purchased, vaccination, death, sold

        public string Notes { get; set; }
        
        [Range(0, 2000)]
        public double? Weight { get; set; } // in kilograms
        public string Diagnosis { get; set; }
        public string Treatment { get; set; }

        [Required]
        public DateTime Date { get; set; }
        public string RecordedBy { get; set; }

        public virtual Livestock Livestock { get; set; }
    }

}