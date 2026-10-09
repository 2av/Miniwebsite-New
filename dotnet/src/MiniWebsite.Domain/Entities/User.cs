using MiniWebsite.Domain.Common;
using MiniWebsite.Domain.Enums;

namespace MiniWebsite.Domain.Entities;

/// <summary>Unified identity (greenfield <c>users</c> table).</summary>
public class User : BaseEntity
{
    public UserRole Role { get; set; } = UserRole.Customer;
    public int? RoleId { get; set; }
    public MiniWebsite.Domain.Entities.Role? AssignedRole { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public required string Name { get; set; }
    public required string Password { get; set; }
    public string? PasswordHash { get; set; }
    public string? Ip { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public string? UserToken { get; set; }
    public string? RefundStatus { get; set; }
    public DateTime? RefundStatusDate { get; set; }
    public int? MwReferralId { get; set; }
    public bool CollaborationEnabled { get; set; }
    public bool SaleskitEnabled { get; set; }
    public bool Influencer { get; set; }
    public string? ReferredBy { get; set; }
    public int? SenderUserId { get; set; }
    public string? SelectService { get; set; }
    public string? GooglePay { get; set; }
    public string? Paytm { get; set; }
    public string? RzPay { get; set; }
    public string? RzPay2 { get; set; }
    public string? Department { get; set; }
    public string? ProfileImage { get; set; }
    public string? ReferralCode { get; set; }
    public string? District { get; set; }
    public string? State { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Pincode { get; set; }
    public string? Gst { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
    public ICollection<Card> Cards { get; set; } = new List<Card>();
    public Wallet? Wallet { get; set; }
}
