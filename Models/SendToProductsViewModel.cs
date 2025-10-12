using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FarmTrack.Models;

namespace FarmTrack.Models
{
    public class SendToProductsViewModel
    {
        public int SourceId { get; set; }
        public string SourceType { get; set; } // HarvestOutcome, Livestock, Inventory
        public string Name { get; set; }
        public string Unit { get; set; }
        public double AvailableQuantity { get; set; } // Optional for livestock
        public int Quantity { get; set; } // input field

        public string Category { get; set; } // optional, prefilled for livestock/inventory/crop
    }

}