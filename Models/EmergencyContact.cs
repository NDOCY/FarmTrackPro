using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FarmTrack.Models
{
    public class EmergencyContact
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Contact Type")]
        [StringLength(50)]
        public string ContactType { get; set; } // "Veterinarian", "Farm Manager", "Owner", etc.

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Phone Number")]
        [StringLength(20)]
        public string Phone { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        [Display(Name = "Available Hours")]
        [StringLength(50)]
        public string AvailableHours { get; set; }

        [Display(Name = "Is Primary Contact")]
        public bool IsPrimary { get; set; } = false;

        public string Notes { get; set; }
    }

}