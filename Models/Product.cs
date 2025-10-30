using FarmTrack.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmTrack.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }   // "Grade A Maize", "Holstein Cow", "Urea Fertilizer"

        [Required, StringLength(50)]
        public string ProductType { get; set; } // Crop, Livestock, Inventory

        [StringLength(50)]
        public string Category { get; set; } // Breed, Inventory.Category, Crop Variety etc.

        [StringLength(20)]
        public string Unit { get; set; }  // "kg", "liters", "heads"

        public int Quantity { get; set; } // 0 for livestock (handled as count=1 each)

        [Display(Name = "Price Per Unit")]
        public double? PricePerUnit { get; set; }

        [StringLength(500)]
        [Display(Name = "Product Description")]
        public string Description { get; set; }

        [Display(Name = "Product Image URL")]
        public string ImageUrl { get; set; }

        [Display(Name = "Is Available")]
        public bool IsAvailable { get; set; } = true;

        [Display(Name = "Featured Product")]
        public bool IsFeatured { get; set; }

        [Display(Name = "Minimum Order Quantity")]
        public int MinimumOrder { get; set; } = 1;

        // Source references (optional)
        public int? HarvestOutcomeId { get; set; }
        [ForeignKey("HarvestOutcomeId")]
        public virtual HarvestOutcome HarvestOutcome { get; set; }

        public int? LivestockId { get; set; }
        [ForeignKey("LivestockId")]
        public virtual Livestock Livestock { get; set; }

        public int? InventoryId { get; set; }
        [ForeignKey("InventoryId")]
        public virtual Inventory Inventory { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Last Updated")]
        public DateTime? LastUpdated { get; set; } = DateTime.Now;

        // Navigation property for reviews
        public virtual ICollection<ProductReview> Reviews { get; set; }
    }


    public class ProductReview
    {
        [Key]
        public int ProductReviewId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [Required]
        [StringLength(500)]
        public string ReviewText { get; set; }

        public DateTime ReviewDate { get; set; } = DateTime.Now;

        public bool IsVerifiedPurchase { get; set; }

        public bool IsActive { get; set; } = true;

        // NEW: Admin Reply Properties
        [StringLength(1000)]
        public string AdminReply { get; set; }

        public DateTime? AdminReplyDate { get; set; }

        public int? AdminReplyUserId { get; set; }

        // Navigation properties
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        // NEW: Navigation property for admin who replied
        [ForeignKey("AdminReplyUserId")]
        public virtual User AdminReplyUser { get; set; }
    }

    // This is just for passing data to the view, not a database table
    public class ProductRatingViewModel
    {
        public int ProductId { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int FiveStar { get; set; }
        public int FourStar { get; set; }
        public int ThreeStar { get; set; }
        public int TwoStar { get; set; }
        public int OneStar { get; set; }

        // Helper properties for percentages
        public double FiveStarPercentage => TotalReviews > 0 ? (FiveStar * 100.0) / TotalReviews : 0;
        public double FourStarPercentage => TotalReviews > 0 ? (FourStar * 100.0) / TotalReviews : 0;
        public double ThreeStarPercentage => TotalReviews > 0 ? (ThreeStar * 100.0) / TotalReviews : 0;
        public double TwoStarPercentage => TotalReviews > 0 ? (TwoStar * 100.0) / TotalReviews : 0;
        public double OneStarPercentage => TotalReviews > 0 ? (OneStar * 100.0) / TotalReviews : 0;
    }
}