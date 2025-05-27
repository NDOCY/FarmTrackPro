using System.Collections.Generic;
using FarmTrack.Models;

namespace FarmTrack.ViewModels
{
    public class ReplyViewModel
    {
        public Message OriginalMessage { get; set; }
        public List<Message> Replies { get; set; }
        public Message NewReply { get; set; }
    }
}
