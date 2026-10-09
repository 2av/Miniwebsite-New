using MiniWebsite.Domain.Common;
using MiniWebsite.Domain.Enums;

namespace MiniWebsite.Domain.Entities;

/// <summary>Unified identity (greenfield <c>users</c> table).</summary>
public class User : BaseEntity
{
    /// <summary>Public user ID (see <see cref="UserCodeGenerator"/>); assigned automatically on insert.</summary>
    public string UserCode { get; set; } = "";
    public int RoleId { get; set; }
    public Role? AssignedRole { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public required string FirstName { get; set; }
    public string LastName { get; set; } = "";
    /// <summary>Always a hash (BCrypt for new values); never plain text.</summary>
    public required string Password { get; set; }
    public string? Ip { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public string? UserToken { get; set; }
    public string? RefundStatus { get; set; }
    public DateTime? RefundStatusDate { get; set; }
    public string? SelectService { get; set; }
    public string? Department { get; set; }
    public string? ProfileImage { get; set; }

    /// <summary><c>user_addresses</c> (one row per user).</summary>
    public UserAddress Address { get; set; } = new();

    /// <summary><c>user_payment_details</c> (one row per user).</summary>
    public UserPaymentDetails Payment { get; set; } = new();

    /// <summary><c>user_referral_settings</c> (one row per user).</summary>
    public UserReferralSettings Referral { get; set; } = new();

    /// <summary>Display name; not mapped, so inside queries use <c>(u.FirstName + " " + u.LastName).Trim()</c>.</summary>
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>Portal the user signs into, derived from <see cref="AssignedRole"/> (auto-included on every query).</summary>
    public UserRole LoginRole => AssignedRole != null
        ? RolePrefixes.ToUserRole(AssignedRole.Prefix)
        : throw new InvalidOperationException($"User {Id} was loaded without its role.");

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
    public ICollection<Card> Cards { get; set; } = new List<Card>();
    public Wallet? Wallet { get; set; }
}

public class UserAddress
{
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? State { get; set; }
    public string? Pincode { get; set; }
    public string? Gst { get; set; }
}

public class UserPaymentDetails
{
    public string? GooglePay { get; set; }
    public string? Paytm { get; set; }
    public string? RzPay { get; set; }
    public string? RzPay2 { get; set; }
}

public class UserReferralSettings
{
    public string? ReferralCode { get; set; }
    public string? ReferredBy { get; set; }
    public int? MwReferralId { get; set; }
    public int? SenderUserId { get; set; }
    public bool Influencer { get; set; }
    public bool CollaborationEnabled { get; set; }
    public bool SaleskitEnabled { get; set; }
}
