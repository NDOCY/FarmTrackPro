using FarmTrack.Models;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace FarmTrack.Models
{
    public class ActivityLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LogId { get; set; }

        public int UserId { get; set; }

        public string Description { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        public virtual User User { get; set; }
    }
}
