using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FarmPro.Models
{
    public class MessageViewModel
    {
        public int MessageId { get; set; }
        public string SenderName { get; set; }
        public string RecipientName { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
    }

}