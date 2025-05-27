using FarmTrack.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmTrack.Models
{
    public class Job
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int JobId { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        public string JobType { get; set; }

        public DateTime DatePosted { get; set; } = DateTime.Now;

        public int UserId { get; set; }

        public string Location { get; set; }

        public string EmploymentType { get; set; }

        public DateTime ApplicationDeadline { get; set; }

        public string SalaryRange { get; set; }

        public string RequiredSkills { get; set; }

        public virtual User User { get; set; }
        public virtual ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
    }
}
