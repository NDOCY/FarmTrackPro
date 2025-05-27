using FarmTrack.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace FarmTrack.Models
{
    public class JobApplication
    {

        public int JobApplicationId { get; set; }

        [Required]
        public int JobId { get; set; }

        [Required]
        public int UserId { get; set; }

        public DateTime AppliedDate { get; set; } = DateTime.Now;

        public string PhoneNumber { get; set; }

        public string Address { get; set; }

        public string ID { get; set; }

        public string CV { get; set; }

        public string Status { get; set; } = "Waiting for Review";

        public string ReviewNotes { get; set; }

        public string Education { get; set; }

        public string Institution { get; set; }

        public string Experience { get; set; }

        public DateTime? InterviewDate { get; set; }

        public string InterviewVenue { get; set; }

        public string InterviewerName { get; set; }
        public bool InterviewEmailSent { get; set; } = false;


        public virtual Job Job { get; set; }

        public virtual User User { get; set; }
    }
}
