namespace MiniWebsite.Domain.Entities;

/// <summary>Admin-managed coupon (<c>coupons</c> table). Plan Type is the linked role.</summary>
public class Coupon
{
    public int Id { get; set; }
    public int? RoleId { get; set; }
    public Role? Role { get; set; }
    public string CouponName { get; set; } = "";
    public string CouponCode { get; set; } = "";
    public string CouponType { get; set; } = "";
    public string PromoDiscountType { get; set; } = "Amount";
    public decimal PromoDiscountValue { get; set; }
    public DateTime? ValidityDate { get; set; }
    /// <summary>0 = unlimited.</summary>
    public int UsageLimitPerCoupon { get; set; }
    /// <summary>0 = unlimited.</summary>
    public int UsageLimitPerUser { get; set; }
    public int CurrentUsage { get; set; }
    /// <summary>Stored as Active or InActive; Expired is derived from <see cref="ValidityDate"/>.</summary>
    public string Status { get; set; } = "Active";
    /// <summary>Referrer commission paid when a referred user signs up (not shown in the coupons grid).</summary>
    public decimal BonusAmount { get; set; }
    /// <summary>Referrer commission on the referee's renewal (not shown in the coupons grid).</summary>
    public decimal RenewalBonusAmount { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public ICollection<DealCustomerMapping> CustomerMappings { get; set; } = new List<DealCustomerMapping>();
}
