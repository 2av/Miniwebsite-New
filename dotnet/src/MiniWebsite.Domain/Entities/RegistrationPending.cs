using MiniWebsite.Domain.Common;

namespace MiniWebsite.Domain.Entities;

/// <summary>Pending registration awaiting OTP (replaces PHP session).</summary>
public class RegistrationPending : BaseEntity
{
    public required string Role { get; set; } // CUSTOMER | FRANCHISEE
    public required string Email { get; set; }
    public required string Phone { get; set; }
    public required string Name { get; set; }
    public string? State { get; set; }
    public required string PasswordHash { get; set; }
    /// <summary>Temporary — used only for welcome email copy (same as PHP session).</summary>
    public required string PlainPassword { get; set; }
    public string? ReferrerEmail { get; set; }
    public required string Otp { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsConsumed { get; set; }
}
