using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FarmTrack.Models
{
    public class TaskStatusViewModel
    {
        public TaskUpdate NewUpdate { get; set; }
        public List<TaskUpdate> PreviousUpdates { get; set; }
        public string TaskTitle { get; set; }
    }

}