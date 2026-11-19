using System.Text.RegularExpressions;
using MiniWebsite.Application.Admin.EmailTemplates;

namespace MiniWebsite.UnitTests;

public class EmailLibraryCatalogTests
{
    private static readonly Regex Placeholder = new(@"\{\{\s*([a-zA-Z0-9_]+)\s*\}\}", RegexOptions.Compiled);

    private static readonly string[] SentKeys =
    [
        EmailLibraryCatalog.RegistrationInvitation,
        EmailLibraryCatalog.EmailVerificationOtp,
        EmailLibraryCatalog.RegistrationWelcome,
        EmailLibraryCatalog.AccountCredentials,
        EmailLibraryCatalog.PasswordResetOtp,
        EmailLibraryCatalog.PasswordChanged,
        EmailLibraryCatalog.DocumentsReceived,
        EmailLibraryCatalog.DocumentsVerified,
        EmailLibraryCatalog.DocumentResubmission,
        EmailLibraryCatalog.PaymentSuccessful,
        EmailLibraryCatalog.PaymentFailed,
        EmailLibraryCatalog.ReferralStatement,
        EmailLibraryCatalog.ReferralPayout,
        EmailLibraryCatalog.ReferralActivation,
        EmailLibraryCatalog.ProspectLoginPayment,
    ];

    [Fact]
    public void Library_has_thirty_events()
    {
        Assert.Equal(30, EmailLibraryCatalog.All.Length);
        Assert.Equal(30, EmailLibraryCatalog.All.Select(d => d.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void Sent_events_match_the_wired_set()
    {
        var sent = EmailLibraryCatalog.All.Where(d => d.Sent).Select(d => d.Key).OrderBy(k => k).ToArray();
        var expected = SentKeys.OrderBy(k => k).ToArray();
        Assert.Equal(expected, sent);
    }

    [Fact]
    public void Copy_uses_named_placeholders_and_fits_the_table()
    {
        foreach (var def in EmailLibraryCatalog.All)
        {
            Assert.True(def.Key.Length <= 80, def.Key);
            Assert.True(def.Name.Length <= 200, def.Name);
            Assert.True(def.Subject.Length <= 500, def.Name);
            Assert.True(def.Variables.Length <= 500, def.Name);
            Assert.True(def.Description.Length <= 1000, def.Name);
            Assert.DoesNotContain("[", def.Subject);
            Assert.DoesNotContain("[", def.BodyHtml);

            var listed = def.Variables.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var used = Placeholder.Matches(def.Subject + def.BodyHtml)
                .Select(m => m.Groups[1].Value);
            foreach (var name in used)
                Assert.Contains(name, listed);
        }
    }

    [Fact]
    public void Welcome_and_credential_mail_keep_the_password()
    {
        Assert.Contains("{{password}}", Find(EmailLibraryCatalog.RegistrationWelcome).BodyHtml, StringComparison.Ordinal);
        Assert.Contains("{{password}}", Find(EmailLibraryCatalog.AccountCredentials).BodyHtml, StringComparison.Ordinal);
        Assert.Contains("{{password}}", Find(EmailLibraryCatalog.ProspectLoginPayment).BodyHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void Sent_templates_render_when_every_placeholder_is_filled()
    {
        foreach (var def in EmailLibraryCatalog.All.Where(d => d.Sent))
        {
            var values = def.Variables.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToDictionary(name => name, name => "sample-" + name, StringComparer.OrdinalIgnoreCase);
            var subject = EmailTemplateRenderer.Apply(def.Subject, values);
            var body = EmailTemplateRenderer.Apply(def.BodyHtml, values);
            Assert.DoesNotContain("{{", subject);
            Assert.DoesNotContain("{{", body);
            Assert.Contains("sample-", body);
        }
    }

    [Fact]
    public void Renderer_substitutes_a_sample_variable()
    {
        var welcome = Find(EmailLibraryCatalog.RegistrationWelcome);
        var subject = EmailTemplateRenderer.Apply(welcome.Subject, new Dictionary<string, string>
        {
            ["first_name"] = "Asha",
        });
        Assert.Equal("Welcome to ShareBizz, Asha!", subject);
    }

    [Fact]
    public void Legacy_rows_are_replaced_only_while_they_still_have_the_old_copy()
    {
        Assert.True(EmailLibraryCatalog.ShouldReplaceLegacy(
            EmailLibraryCatalog.EmailVerificationOtp, "Registration OTP", "Already edited"));
        Assert.False(EmailLibraryCatalog.ShouldReplaceLegacy(
            EmailLibraryCatalog.EmailVerificationOtp,
            "CE-02 — Email Verification OTP",
            "Your ShareBizz verification code"));
        Assert.False(EmailLibraryCatalog.ShouldReplaceLegacy(
            "collaboration_upgraded", "Collaboration Feature Enabled", "Congratulations"));
    }

    private static EmailLibraryDefinition Find(string key) =>
        EmailLibraryCatalog.Find(key) ?? throw new InvalidOperationException(key);
}
