using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FarmTrack.Models
{
    public class SendInventoryToProductsViewModel
    {
        public int InventoryId { get; set; }

        public string ItemName { get; set; }

        public int AvailableQuantity { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int QuantityToSend { get; set; }
    }

}