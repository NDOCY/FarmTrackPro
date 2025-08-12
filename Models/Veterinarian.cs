using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FarmTrack.Models
{
    public class Veterinarian
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Full Name")]
        [StringLength(100)]
        public string FullName { get; set; }

        [Display(Name = "Phone Number")]
        [StringLength(20)]
        public string Phone { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        [StringLength(100)]
        public string Specialization { get; set; }

        [Display(Name = "Clinic Name")]
        [StringLength(150)]
        public string ClinicName { get; set; }

        public string Address { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        public string Notes { get; set; }

        // Navigation property
        public virtual ICollection<HealthCheckSchedule> HealthCheckSchedules { get; set; }
    }

}