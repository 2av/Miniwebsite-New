namespace MiniWebsite.Domain.Entities;

/// <summary>Editable system email template (greenfield <c>email_templates</c>).</summary>
public class EmailTemplate
{
    public int Id { get; set; }
    /// <summary>Stable key used by code, e.g. registration_otp.</summary>
    public string TemplateKey { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "general";
    public string Subject { get; set; } = "";
    public string BodyHtml { get; set; } = "";
    /// <summary>Comma-separated placeholder names without braces, e.g. name,email,otp.</summary>
    public string? AvailableVariables { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsSystem { get; set; } = true;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
