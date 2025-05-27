using FarmTrack.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmTrack.Models
{
    public class Message
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MessageId { get; set; }

        public int SenderId { get; set; }
        public int? RecipientId { get; set; }  // Optional for group/department messages

        public string Department { get; set; } // Optional for group messaging
        public bool IsToAdmins { get; set; } = false;
        public bool IsGroupMessage { get; set; } = false;

        public int? ConversationId { get; set; } // All replies tied to a conversation
        public int? ReplyToMessageId { get; set; } // Optional: direct reply

        public DateTime SentAt { get; set; } = DateTime.Now;
        public string Subject { get; set; }
        public string Body { get; set; }
        public bool IsRead { get; set; } = false;

        // Navigation
        public virtual User Sender { get; set; }
        public virtual User Recipient { get; set; }
        public virtual Message ReplyToMessage { get; set; }
        public virtual ICollection<Message> Replies { get; set; }
    }

}
