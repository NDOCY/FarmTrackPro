using System;
//using FarmTrackPro.Models; 
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmTrack.Models
{
    public class PreparationTask
    {
        public int Id { get; set; }

        [Required]
        public int CropAssignmentId { get; set; }

        [Required]
        public string TaskType { get; set; } // e.g., "Plowing", "Fertilizing"

        public string Notes { get; set; }

        [Required]
        public bool IsCompleted { get; set; } = false;

        public DateTime? CompletedOn { get; set; }

        //[ForeignKey("CropAssignmentId")]
        //public virtual CropAssignment CropAssignment { get; set; }
    }
}
