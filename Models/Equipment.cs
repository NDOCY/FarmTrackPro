using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FarmTrack.Models
{
    public class Equipment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int EquipmentId { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        public string SerialNumber { get; set; }
        public string ImagePath { get; set; }
        public string Category { get; set; }

        public DateTime? Date { get; set; }

        public string Status { get; set; } // e.g., Active, Under Maintenance, Decommissioned

        public ICollection<EquipmentRepair> EquipmentRepairs { get; set; }

        public ICollection<EquipmentRepairLog> RepairLogs { get; set; }
    }


}