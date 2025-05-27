using FarmTrack.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FarmTrack.Models
{
    public class TaskUpdate
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int TaskId { get; set; }

        [Required]
        public string TasksStatus { get; set; } // e.g., In Progress, On Hold, Completed

        [Range(0, 100)]
        public int Progress { get; set; }

        public string Comments { get; set; }
        public bool SeenByAdmin { get; set; } = false;

        public DateTime DateUpdated { get; set; } = DateTime.Now;

        public int UpdatedBy { get; set; }

        [ForeignKey("TaskId")]
        public virtual FarmTask Task { get; set; }

        [ForeignKey("UpdatedBy")]
        public virtual User User { get; set; }
    }

}