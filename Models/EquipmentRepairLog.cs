using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FarmTrack.Models
{
    public class EquipmentRepairLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int EquipmentRepairLogId { get; set; }

        public int EquipmentId { get; set; }

        [Required]
        public DateTime RepairDate { get; set; }

        public string IssueReported { get; set; }

        public string RepairDetails { get; set; }

        public string RepairedBy { get; set; }

        public virtual Equipment Equipment { get; set; }
        
    }
}