using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FarmTrack.Models
{
    public class NotificationViewModel
    {
        public string Type { get; set; }      // e.g., "Message", "Task", etc.
        public string Content { get; set; }   // Notification message
        public DateTime Time { get; set; }    // Timestamp
        public bool IsRead { get; set; }      // Optional for future use
    }

}