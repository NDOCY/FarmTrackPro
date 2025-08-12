using FarmPro.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

namespace FarmTrack.Models
{
    public class FarmTask
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TaskId { get; set; }

        [Required]
        public string Title { get; set; }

        public string Description { get; set; }

        public DateTime DueDate { get; set; }

        // Add TaskPhase for crop lifecycle stages
        public string TaskPhase { get; set; } // "Preparation", "Planting", "Growing", "Harvest"

        // Add PlotCrop foreign key
        public int? PlotCropId { get; set; } // Nullable for tasks not tied to PlotCrop

        [ForeignKey("PlotCropId")]
        public virtual PlotCrop PlotCrop { get; set; }
        public int? AssignedTo { get; set; }

        [Required]
        public string Status { get; set; } = "Pending"; // "Pending", "InProgress", "OnHold", "Completed"

        public bool IsRecurring { get; set; }

        public string AssignedDepartment { get; set; }

        public string RecurrenceType { get; set; }

        public DateTime? LastGeneratedDate { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; }

        public virtual User AssignedUser { get; set; }

        // 🔽 New additions:
        public int? DependsOnTaskId { get; set; }
        [ForeignKey("DependsOnTaskId")]
        public virtual FarmTask DependsOn { get; set; }

        public int Progress { get; set; } = 0; // 0 to 100
        public string Comments { get; set; }
        public virtual ICollection<TaskStatus> StatusUpdates { get; set; }

    }

}
