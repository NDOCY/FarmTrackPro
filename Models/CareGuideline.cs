using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FarmTrack.Models
{
    public class CareGuideline
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Animal Type")]
        [StringLength(50)]
        public string AnimalType { get; set; }

        [Required]
        [Display(Name = "Issue Type")]
        [StringLength(100)]
        public string IssueType { get; set; } // e.g., "Burns", "Bone Damage", "Respiratory Issues"

        [Required]
        [Display(Name = "Emergency Level")]
        public string EmergencyLevel { get; set; } // "Critical", "Urgent", "Moderate"

        [Required]
        [Display(Name = "Immediate Actions")]
        public string ImmediateActions { get; set; }

        [Display(Name = "What NOT to Do")]
        public string WhatNotToDo { get; set; }

        [Display(Name = "When to Call Vet")]
        public string WhenToCallVet { get; set; }

        [Display(Name = "Additional Notes")]
        public string AdditionalNotes { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Last Updated")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }


}