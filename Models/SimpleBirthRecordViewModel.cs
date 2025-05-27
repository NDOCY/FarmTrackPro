using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FarmTrack.Models
{
    public class SimpleBirthRecordViewModel
    {
        public int ReproductionRecordId { get; set; }

        public string FemaleTag { get; set; }
        public string MaleTag { get; set; }

        public string BirthOutcome { get; set; }  // "Successful", "Stillborn", etc.
        public int? NumberOfOffspring { get; set; }  // Optional
    }

}