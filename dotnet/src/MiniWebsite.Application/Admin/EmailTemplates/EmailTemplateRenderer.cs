using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MiniWebsite.Application.Common.Interfaces;

namespace MiniWebsite.Application.Admin.EmailTemplates;

public interface IEmailTemplateRenderer
{
    /// <summary>
    /// Loads an active template by key and applies variables.
    /// Returns null if missing/inactive so callers can use hardcoded fallbacks.
    /// </summary>
    Task<(string Subject, string Html)?> TryRenderAsync(
        string templateKey,
        IReadOnlyDictionary<string, string> variables,
        CancellationToken ct = default);
}

public sealed class EmailTemplateRenderer : IEmailTemplateRenderer
{
    private static readonly Regex Placeholder = new(@"\{\{\s*([a-zA-Z0-9_]+)\s*\}\}", RegexOptions.Compiled);

    private readonly IApplicationDbContext _db;

    public EmailTemplateRenderer(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<(string Subject, string Html)?> TryRenderAsync(
        string templateKey,
        IReadOnlyDictionary<string, string> variables,
        CancellationToken ct = default)
    {
        var key = (templateKey ?? "").Trim();
        if (string.IsNullOrEmpty(key)) return null;

        var tpl = await _db.EmailTemplates.AsNoTracking()
            .FirstOrDefaultAsync(t => t.TemplateKey == key && t.IsActive, ct);
        if (tpl == null) return null;

        return (Apply(tpl.Subject, variables), Apply(tpl.BodyHtml, variables));
    }

    public static string Apply(string template, IReadOnlyDictionary<string, string> variables)
    {
        if (string.IsNullOrEmpty(template)) return template;
        return Placeholder.Replace(template, m =>
        {
            var name = m.Groups[1].Value;
            if (variables.TryGetValue(name, out var value)) return value ?? "";
            // case-insensitive fallback
            foreach (var kv in variables)
            {
                if (string.Equals(kv.Key, name, StringComparison.OrdinalIgnoreCase))
                    return kv.Value ?? "";
            }
            return m.Value;
        });
    }
}
