using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Options;

namespace MiniWebsite.Application.Admin.EmailTemplates;

public enum EmailReplyMailbox
{
    Support,
    Billing,
}

public interface ILibraryEmailSender
{
    string Page(string relativePath);

    Task SendAsync(
        string templateKey,
        string toEmail,
        IReadOnlyDictionary<string, string?>? variables = null,
        EmailReplyMailbox replyTo = EmailReplyMailbox.Support,
        CancellationToken ct = default);

    Task<bool> TrySendAsync(
        string templateKey,
        string toEmail,
        IReadOnlyDictionary<string, string?>? variables = null,
        EmailReplyMailbox replyTo = EmailReplyMailbox.Support,
        CancellationToken ct = default);
}

public sealed class LibraryEmailSender : ILibraryEmailSender
{
    private readonly IEmailTemplateRenderer _templates;
    private readonly IEmailSender _email;
    private readonly IConfiguration _configuration;
    private readonly AppOptions _app;

    public LibraryEmailSender(
        IEmailTemplateRenderer templates,
        IEmailSender email,
        IConfiguration configuration,
        IOptions<AppOptions> app)
    {
        _templates = templates;
        _email = email;
        _configuration = configuration;
        _app = app.Value;
    }

    public string Page(string relativePath)
    {
        var path = (relativePath ?? "").Trim().TrimStart('/');
        return _app.SiteBaseUrl.TrimEnd('/') + "/" + path;
    }

    public async Task SendAsync(
        string templateKey,
        string toEmail,
        IReadOnlyDictionary<string, string?>? variables = null,
        EmailReplyMailbox replyTo = EmailReplyMailbox.Support,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(toEmail)) return;

        var merged = Merge(templateKey, variables);
        var rendered = await _templates.TryRenderAsync(templateKey, merged, ct);
        if (rendered == null)
        {
            var definition = EmailLibraryCatalog.Find(templateKey);
            if (definition == null) return;
            rendered = (
                EmailTemplateRenderer.Apply(definition.Subject, merged),
                EmailTemplateRenderer.Apply(definition.BodyHtml, merged));
        }

        var mailbox = replyTo == EmailReplyMailbox.Billing ? "Billing" : "Support";
        await _email.SendAsync(toEmail.Trim(), rendered.Value.Subject, rendered.Value.Html, mailbox, ct);
    }

    public async Task<bool> TrySendAsync(
        string templateKey,
        string toEmail,
        IReadOnlyDictionary<string, string?>? variables = null,
        EmailReplyMailbox replyTo = EmailReplyMailbox.Support,
        CancellationToken ct = default)
    {
        try
        {
            await SendAsync(templateKey, toEmail, variables, replyTo, ct);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private Dictionary<string, string> Merge(string templateKey, IReadOnlyDictionary<string, string?>? variables)
    {
        var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (variables != null)
        {
            foreach (var pair in variables)
                merged[pair.Key] = pair.Value ?? "";
        }

        AddIfMissing(merged, "support_email", Address("Smtp:Mailboxes:Support:Username", "support@sharebizz.com"));
        AddIfMissing(merged, "billing_email", Address("Smtp:Mailboxes:Billing:Username", "billing@sharebizz.com"));
        AddIfMissing(merged, "hr_email", Address("Smtp:Mailboxes:Hr:Username", "hr@sharebizz.com"));
        AddIfMissing(merged, "website", _app.SiteBaseUrl.TrimEnd('/'));
        AddIfMissing(merged, "sender_name", Address("Smtp:FromName", "ShareBizz"));
        AddIfMissing(merged, "host", string.IsNullOrWhiteSpace(_app.PublicHost) ? "ShareBizz" : _app.PublicHost);
        if (!merged.ContainsKey("first_name") && merged.TryGetValue("name", out var name))
            merged["first_name"] = name;

        var definition = EmailLibraryCatalog.Find(templateKey);
        if (definition != null)
        {
            foreach (var variableName in definition.Variables.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!merged.ContainsKey(variableName)) merged[variableName] = "";
            }
        }

        foreach (var key in merged.Keys.ToList())
        {
            if (EmailLibraryMessages.RawHtmlKeys.Contains(key)) continue;
            merged[key] = WebUtility.HtmlEncode(merged[key]);
        }

        return merged;
    }

    private void AddIfMissing(Dictionary<string, string> values, string key, string value)
    {
        if (!values.ContainsKey(key)) values[key] = value;
    }

    private string Address(string key, string fallback)
    {
        var value = _configuration[key];
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
