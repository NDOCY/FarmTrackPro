using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmTrack.Models
{
    public class Inventory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int InventoryId { get; set; }

        [Required]
        public string ItemName { get; set; }

        public string Category { get; set; }

        
        public int Quantity { get; set; }

        public DateTime DateAdded { get; set; } = DateTime.Now;

        public int UserId { get; set; }

        public string Barcode { get; set; } // Optional scanned barcode

        public string QrCodePath { get; set; } // Generated QR code image

        public string Notes { get; set; }

        public bool NotifySupplier { get; set; }
        public int RestockThreshold { get; set; }

        public virtual User User { get; set; }
        public int LowStockThreshold { get; set; } = 5;
        [NotMapped]
        public bool IsLowStock => Quantity <= LowStockThreshold;

        public DateTime? LastRestocked { get; set; }
        public int? SupplierId { get; set; }
        [ForeignKey("SupplierId")]
        public virtual Supplier Supplier { get; set; }
    }
}
