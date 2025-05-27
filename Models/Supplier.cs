using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace FarmTrack.Models
{
    public class Supplier
    {
        public int SupplierId { get; set; }

        [Required]
        [Display(Name = "Supplier Name")]
        public string Name { get; set; }

        [Display(Name = "Contact Person")]
        public string ContactPerson { get; set; }

        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        public string Address { get; set; }

        public virtual ICollection<InventoryRestock> Restocks { get; set; }
    }
}
