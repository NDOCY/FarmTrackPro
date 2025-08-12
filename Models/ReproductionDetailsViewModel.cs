using System;
using System.Collections.Generic;

namespace FarmTrack.Models
{
    public class ReproductionDetailsViewModel
    {
        public int Id { get; set; }

        public string FemaleTag { get; set; }
        public string MaleTag { get; set; }

        public DateTime BreedingDate { get; set; }
        public DateTime? ExpectedBirthDate { get; set; }

        public string BirthOutcome { get; set; } // e.g., "Successful", "Stillbirth", "Not Recorded"

        public List<OffspringViewModel> Offspring { get; set; } = new List<OffspringViewModel>();
    }

    public class OffspringViewModel
    {
        public int Id { get; set; }
        public string Tag { get; set; }
        public string Gender { get; set; }
        public DateTime? BirthDate { get; set; }
    }
}
