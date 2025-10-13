using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmTrack.Models
{
    public class DiscountVoucher
    {
        [Key]
        public int VoucherId { get; set; }

        [StringLength(50)]
        public string Code { get; set; }

        [Required]
        [StringLength(500)]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Voucher Type")]
        public VoucherType VoucherType { get; set; }

        [Required]
        [Display(Name = "Discount Value")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Discount value must be greater than 0")]
        public decimal DiscountValue { get; set; }

        [Display(Name = "Minimum Order Amount")]
        [Range(0, double.MaxValue, ErrorMessage = "Minimum order amount cannot be negative")]
        public decimal? MinimumOrderAmount { get; set; }

        [Display(Name = "Maximum Discount")]
        [Range(0, double.MaxValue, ErrorMessage = "Maximum discount cannot be negative")]
        public decimal? MaximumDiscount { get; set; }

        [Required]
        [Display(Name = "Valid From")]
        [DataType(DataType.Date)]
        public DateTime ValidFrom { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "Valid To")]
        [DataType(DataType.Date)]
        public DateTime ValidTo { get; set; }

        [Required]
        [Display(Name = "Usage Limit")]
        [Range(0, int.MaxValue, ErrorMessage = "Usage limit cannot be negative")]
        public int UsageLimit { get; set; } = 0;

        public int UsedCount { get; set; } = 0;

        [Required]
        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Required]
        [Display(Name = "Single Use Per Customer")]
        public bool IsSingleUse { get; set; } = false;

        [Required]
        [Display(Name = "Applicability")]
        public VoucherApplicability Applicability { get; set; } = VoucherApplicability.AllProducts;

        [StringLength(50)]
        [Display(Name = "Applicable Category")]
        public string ApplicableCategory { get; set; }

        [Display(Name = "Applicable Product")]
        public int? ApplicableProductId { get; set; }

        // Navigation properties
        [ForeignKey("ApplicableProductId")]
        public virtual Product ApplicableProduct { get; set; }

        public virtual ICollection<VoucherUsage> VoucherUsages { get; set; }

        // Helper properties
        [NotMapped]
        public bool IsValid => IsActive &&
                              DateTime.Now >= ValidFrom &&
                              DateTime.Now <= ValidTo &&
                              (UsageLimit == 0 || UsedCount < UsageLimit);

        [NotMapped]
        public string DisplayValue => VoucherType == VoucherType.FixedAmount
            ? $"R{DiscountValue:0.##}"
            : $"{DiscountValue}%";
    }

    public class VoucherUsage
    {
        [Key]
        public int VoucherUsageId { get; set; }

        [Required]
        public int VoucherId { get; set; }

        [Required]
        public int SaleId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public DateTime UsedAt { get; set; } = DateTime.Now;

        [Required]
        public decimal DiscountAmount { get; set; }

        [Required]
        public decimal OrderTotalBeforeDiscount { get; set; }

        [Required]
        public decimal OrderTotalAfterDiscount { get; set; }

        // Navigation properties
        [ForeignKey("VoucherId")]
        public virtual DiscountVoucher Voucher { get; set; }

        [ForeignKey("SaleId")]
        public virtual Sale Sale { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }

    public enum VoucherType
    {
        FixedAmount = 1,
        Percentage = 2
    }

    public enum VoucherApplicability
    {
        AllProducts = 1,
        SpecificCategory = 2,
        SpecificProduct = 3,
        MinimumOrderOnly = 4
    }
}