using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace FarmTrack.Models
{
    public class HealthCheckSchedule
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string CheckType { get; set; }

        public string Notes { get; set; }

        public DateTime ScheduledDate { get; set; }

        public string Status { get; set; } = "Pending";


        [Display(Name = "Is Outsourced")]
        public bool IsOutsourced { get; set; } = false;

        [Display(Name = "Veterinarian")]
        public int? VeterinarianId { get; set; }

        [Display(Name = "Appointment Purpose")]
        [StringLength(200)]
        public string Purpose { get; set; }

        [Display(Name = "Estimated Cost")]
        [Column(TypeName = "decimal")]
        public decimal? EstimatedCost { get; set; }

        [Display(Name = "Actual Cost")]
        [Column(TypeName = "decimal")]
        public decimal? ActualCost { get; set; }

        [Display(Name = "Vet Instructions")]
        public string VetInstructions { get; set; }

        [Display(Name = "Follow-up Required")]
        public bool RequiresFollowUp { get; set; } = false;

        [Display(Name = "Follow-up Date")]
        public DateTime? FollowUpDate { get; set; }


        [ForeignKey("VeterinarianId")]
        public virtual Veterinarian Veterinarian { get; set; }


        public int? AssignedToUserId { get; set; }

        [ForeignKey("AssignedToUserId")]
        public virtual User AssignedToUser { get; set; }

        public virtual ICollection<HealthCheckLivestock> HealthCheckLivestocks { get; set; }

        public HealthCheckSchedule()
        {
            HealthCheckLivestocks = new HashSet<HealthCheckLivestock>();
        }

    }
}