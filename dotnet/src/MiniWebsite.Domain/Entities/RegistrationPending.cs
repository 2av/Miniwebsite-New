using MiniWebsite.Domain.Common;

namespace MiniWebsite.Domain.Entities;

/// <summary>Pending registration awaiting OTP (replaces PHP session).</summary>
public class RegistrationPending : BaseEntity
{
    public required string Role { get; set; } // CUSTOMER | FRANCHISEE
    public required string Email { get; set; }
    public required string Phone { get; set; }
    public required string FirstName { get; set; }
    public string LastName { get; set; } = "";
    public string? State { get; set; }
    public required string PasswordHash { get; set; }
    public string? ReferrerEmail { get; set; }
    public int? ReferrerUserId { get; set; }
    public required string Otp { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsConsumed { get; set; }
}
