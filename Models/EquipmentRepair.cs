using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FarmTrack.Models
{

    public class EquipmentRepair
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RepairId { get; set; }
        public int EquipmentId { get; set; }

        public DateTime RepairDate { get; set; }
        public string Description { get; set; }

        public string TechnicianType { get; set; } // "In-house" or "Outsourced"
        public int? InHouseUserId { get; set; }    // FK to User table
        public string OutsourcedTechnicianName { get; set; }
        public string OutsourcedEmail { get; set; }

        public string Status { get; set; } = "Scheduled";
        public decimal Cost { get; set; }
        public virtual User InHouseUser { get; set; } // Navigation property

        public virtual Equipment Equipment { get; set; }

        public ICollection<EquipmentRepairLog> RepairLogs { get; set; }
    }




}