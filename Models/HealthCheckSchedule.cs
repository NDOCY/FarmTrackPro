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

        
        public int? AssignedToUserId { get; set; }

        [ForeignKey("AssignedToUserId")]
        public virtual User AssignedToUser { get; set; }

        public virtual ICollection<HealthCheckLivestock> HealthCheckLivestock { get; set; }
    }


}