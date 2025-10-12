using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FarmTrack.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public double PricePerUnit { get; set; }
        public int Quantity { get; set; }

        public double Total => PricePerUnit * Quantity;
    }
}