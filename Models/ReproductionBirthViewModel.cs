using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FarmTrack.Models
{
    public class ReproductionBirthViewModel
    {
        public int ReproductionRecordId { get; set; }

        public string BirthOutcome { get; set; }
        public int NumberOfOffspring { get; set; }

        public List<string> OffspringTags { get; set; }
        public List<string> OffspringSexes { get; set; }

        public string FemaleTag { get; set; }
        public string MaleTag { get; set; }
    }

}