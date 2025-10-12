using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FarmTrack.Models
{
    public class PaymentRequest
    {
        public string PaymentMethod { get; set; }
        public string CardNumber { get; set; }
        public string ExpiryDate { get; set; }
        public string CVV { get; set; }
        public string NameOnCard { get; set; }
        public decimal Amount { get; set; }
    }
}