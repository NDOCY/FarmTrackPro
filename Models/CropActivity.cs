using FarmTrack.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FarmTrack.Models
{
    public class CropActivity
    {
        [Key]
        public int ActivityId { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public int PlotId { get; set; }
        [ForeignKey("PlotId")]
        public virtual Plot Plot { get; set; }

        // Inherit all task properties
        public string Description { get; set; }
        public DateTime DueDate { get; set; }
        public int? AssignedTo { get; set; } // UserId
        public string Status { get; set; } = "Pending";
        public bool IsRecurring { get; set; }
        public string RecurrenceType { get; set; }
        public DateTime? LastGeneratedDate { get; set; }
        public int Progress { get; set; } = 0;

        // Crop-specific fields
        public string ActivityType { get; set; } // "Planting", "Fertilization", "Harvest", etc.
        public string EquipmentNeeded { get; set; }
        public string MaterialsUsed { get; set; }

        [ForeignKey("AssignedTo")]
        public virtual User AssignedUser { get; set; }

        public virtual ICollection<ActivityUpdate> Updates { get; set; }
    }

    public class ActivityUpdate
    {
        public int UpdateId { get; set; }
        public int ActivityId { get; set; }

        [ForeignKey("ActivityId")]
        public virtual CropActivity Activity { get; set; }

        public int UpdatedBy { get; set; } // UserId
        public DateTime DateUpdated { get; set; }
        public string Status { get; set; }
        public string Comments { get; set; }
        public bool SeenByAdmin { get; set; } = false;
    }
}