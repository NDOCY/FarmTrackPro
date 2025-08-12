using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using FarmTrack.Models;
using System.Web;

namespace FarmTrack.Models
{
    public class PlantingTaskViewModel
    {
        public int PlotCropId { get; set; }
        public List<PlantingTaskInput> Tasks { get; set; } = new List<PlantingTaskInput>();
    }

    public class PlantingTaskInput
    {
        [Required]
        public string Name { get; set; }

        [Range(0, 365)]
        public int DaysAfterPlanting { get; set; }

        public bool IsRecurring { get; set; }
        public string RecurrenceType { get; set; } // "Daily", "Weekly", "Monthly"
        //public int? RecurrenceInterval { get; set; } // e.g., every 2 weeks
    }
}