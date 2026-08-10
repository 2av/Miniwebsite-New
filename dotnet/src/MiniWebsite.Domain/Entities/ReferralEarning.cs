namespace MiniWebsite.Domain.Entities;

public class ReferralEarning
{
    public int Id { get; set; }
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
    public int? DealId { get; set; }
    public Deal? Deal { get; set; }
    public ICollection<ReferralPayment> Payments { get; set; } = new List<ReferralPayment>();
}
