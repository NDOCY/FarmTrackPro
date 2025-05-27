using FarmTrack.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FarmTrack.Models
{
    public class UserDashboardViewModel
    {
        
            public User User { get; set; }
            public List<FarmTask> AssignedTasks { get; set; }
            public List<ActivityLog> RecentActivities { get; set; }
            public List<Job> Job { get; set; }

            public int JobApplicationsCount { get; set; }
            public int MessagesCount { get; set; }

    }
}