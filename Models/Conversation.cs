using FarmTrack.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmTrack.Models
{
    public class Conversation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ConversationId { get; set; }

        [Required]
        public string Subject { get; set; }

        [Required]
        public int CreatedById { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsBroadcast { get; set; } = false;

        public bool IsClosed { get; set; } = false;

        [ForeignKey("CreatedById")]
        public virtual User CreatedBy { get; set; }

        public virtual ICollection<Message> Messages { get; set; }
    }
}
