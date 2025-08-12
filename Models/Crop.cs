using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmTrack.Models
{
    public class Crop
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Crop Name")]
        public string Name { get; set; }  // e.g., Maize, Wheat, Carrots

        [StringLength(50)]
        [Display(Name = "Variety")]
        public string Variety { get; set; }  // e.g., SC701, PAN53

        // Navigation property for related data
        public virtual ICollection<Plot> Plots { get; set; }

        // Link to fetched crop requirements
        //public virtual CropRequirements Requirements { get; set; }
        //[ForeignKey("CropRequirementId")]   
        public virtual CropRequirements CropRequirement { get; set; }
    }
}
