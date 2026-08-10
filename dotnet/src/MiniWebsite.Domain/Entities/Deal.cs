namespace MiniWebsite.Domain.Entities;

/// <summary>Promo / coupon deals (greenfield <c>deals</c>).</summary>
public class Deal
{
    public int Id { get; set; }
    public string PlanName { get; set; } = "";
    public string PlanType { get; set; } = "";
    public string? DealState { get; set; }
    public string DealName { get; set; } = "";
    public string CouponCode { get; set; } = "";
    public string CouponType { get; set; } = "";
    public decimal BonusAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal RenewalBonusAmount { get; set; }
    public DateTime? ValidityDate { get; set; }
    public int MaxUsage { get; set; }
    public int CurrentUsage { get; set; }
    public string DealStatus { get; set; } = "Active";
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public ICollection<DealCustomerMapping> CustomerMappings { get; set; } = new List<DealCustomerMapping>();
}
