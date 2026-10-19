namespace MiniWebsite.Domain.Entities;

public class ReferralEarning
{
    public int Id { get; set; }
    /// <summary>Public commission ID, e.g. <c>COM-26-27-00000001</c>; assigned automatically on insert.</summary>
    public string? CommissionCode { get; set; }
    public string? AdminComment { get; set; }
    public int? ReferrerUserId { get; set; }
    public User? ReferrerUser { get; set; }
    public int? ReferredUserId { get; set; }
    public User? ReferredUser { get; set; }
    public string ReferrerEmail { get; set; } = "";
    public string ReferredEmail { get; set; } = "";
    public DateTime ReferralDate { get; set; }
    public string Status { get; set; } = "Pending";
    public decimal Amount { get; set; }
    public string IsCollaboration { get; set; } = "NO";
    public DateTime? PaymentDate { get; set; }
    public int? CouponId { get; set; }
    public Coupon? Coupon { get; set; }
    /// <summary>Order this commission was earned on; when unset, readers fall back to the referred user's invoice closest to <see cref="ReferralDate"/>.</summary>
    public int? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    public ICollection<ReferralPayment> Payments { get; set; } = new List<ReferralPayment>();
}
