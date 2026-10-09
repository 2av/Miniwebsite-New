namespace MiniWebsite.Domain.Entities;

/// <summary>Admin-managed role catalog (greenfield <c>roles</c> table).</summary>
public class Role
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = "";
    public string? RoleDescription { get; set; }
    public string? Prefix { get; set; }
    public string? SalesType { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
