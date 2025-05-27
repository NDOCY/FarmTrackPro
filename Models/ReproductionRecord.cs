using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FarmTrack.Models
{
    public class ReproductionRecord
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int FemaleLivestockId { get; set; }

        public int? MaleLivestockId { get; set; }

        public DateTime BreedingDate { get; set; }

        public DateTime? ExpectedDueDate { get; set; }

        public DateTime? ActualBirthDate { get; set; } // Optional, will be set at birth

        public string BirthOutcome { get; set; } // Optional – will be set at birth

        public string Notes { get; set; } // Optional
        public int? NumberOfOffspring { get; set; } // optional, to be filled later
        public bool IsBirthRecorded { get; set; } = false; // flag to prevent double entry


        public virtual Livestock FemaleLivestock { get; set; }
        public virtual Livestock MaleLivestock { get; set; }
    }


}