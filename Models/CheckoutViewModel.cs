using FarmTrack.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FarmTrack.Models
{
    public class CheckoutViewModel
    {
        public List<CartItem> CartItems { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [Display(Name = "Full Name")]
        public string CustomerName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string CustomerEmail { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Phone Number")]
        public string CustomerPhone { get; set; }

        [Required(ErrorMessage = "Delivery address is required")]
        [Display(Name = "Delivery Address")]
        public string DeliveryAddress { get; set; }

        [Display(Name = "Delivery Instructions")]
        public string DeliveryInstructions { get; set; }

        [Required(ErrorMessage = "Payment method is required")]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; }
    }
}