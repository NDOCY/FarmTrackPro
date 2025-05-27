using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Web.Services.Description;

namespace FarmTrack.Models
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Phone]
        public string PhoneNumber { get; set; }

        public string Address { get; set; }

        public string ProfilePictureUrl { get; set; }

        public DateTime DateRegistered { get; set; } = DateTime.Now;

        public string Department { get; set; }

        public string Role { get; set; }

        public string ID { get; set; }

        public string CV { get; set; }

        public virtual ICollection<FarmTask> Tasks { get; set; } = new List<FarmTask>();
        public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
        public virtual ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
        public virtual ICollection<Job> Jobs { get; set; } = new List<Job>();
        public virtual ICollection<Livestock> Livestocks { get; set; } = new List<Livestock>();
        public virtual ICollection<Message> SentMessages { get; set; } = new List<Message>();
        public virtual ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
    }
}
