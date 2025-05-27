using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FarmTrack.Models
{
    
        public class ReproductionListViewModel
        {
            public int Id { get; set; }
            public string FemaleTag { get; set; }
            public string MaleTag { get; set; }
            public DateTime BreedingDate { get; set; }
        public DateTime? ExpectedBirthDate { get; set; }
        public bool IsBirthRecorded { get; set; }
            public int? NumberOfOffspring { get; set; }
        }
    }


