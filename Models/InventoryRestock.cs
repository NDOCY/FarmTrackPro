using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmTrack.Models
{
    public class InventoryRestock
    {
        [Key]
        public int InventoryRestockId { get; set; }

        [Required]
        public int InventoryId { get; set; }
        public virtual Inventory Inventory { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int Quantity { get; set; }

        public DateTime RequestedOn { get; set; }
        public bool SupplierNotified { get; set; }
        public bool IsCompleted { get; set; }
        public bool Failed { get; set; }
        public DateTime? CompletedOn { get; set; }

        [Required]
        public int RequestedById { get; set; }
        public virtual User RequestedBy { get; set; }

        [Required(ErrorMessage = "Please select a supplier")]
        public int SupplierId { get; set; }
        [ForeignKey("SupplierId")]
        public virtual Supplier Supplier { get; set; }
    }
}
