namespace MiniWebsite.Infrastructure.Options;

/// <summary>Admin-panel login created on startup when no user with <see cref="Email"/> exists.</summary>
public class AdminSeedOptions
{
    public const string SectionName = "AdminSeed";
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = "ShareBizz";
    public string LastName { get; set; } = "Admin";
}
