using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace FarmTrack.Models
{
    public class HealthCheckLivestock
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int HealthCheckScheduleId { get; set; }
        public int LivestockId { get; set; }

        public virtual HealthCheckSchedule HealthCheckSchedule { get; set; }
        public virtual Livestock Livestock { get; set; }
    }

}