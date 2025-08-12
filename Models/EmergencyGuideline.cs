using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FarmTrack.Models
{
    public class EmergencyGuideline
    {
        public int Id { get; set; }
        public string AnimalType { get; set; } // Cow, Goat, etc.
        public string Condition { get; set; } // Burn, Bone damage, etc.
        public string FirstAidSteps { get; set; }
        public string Notes { get; set; }
    }

}