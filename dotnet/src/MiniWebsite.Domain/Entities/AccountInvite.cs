using MiniWebsite.Domain.Common;

namespace MiniWebsite.Domain.Entities;

/// <summary>Email link that lets a partner finish Create Account. The account row is created when they submit the form.</summary>
public class AccountInvite : BaseEntity
{
    public required string Email { get; set; }
    public required string RolePrefix { get; set; }
    public required string TokenHash { get; set; }
    public int? InvitedByUserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsConsumed { get; set; }
}
