using System.Globalization;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MiniWebsite.Application.Admin.EmailTemplates;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Application.PublicSite.Dtos;

namespace MiniWebsite.Application.PublicSite;

public class PublicSiteService : IPublicSiteService
{
    private static readonly HashSet<string> AllowedFaqPages = new(StringComparer.OrdinalIgnoreCase)
    {
        "home", "franchise", "refer_earn"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "terms_conditions",
        "privacy_policy",
        "franchisee_agreement",
        "franchisee_distributer",
        "mw_full_franchise_agreement",
        "mw_franchisee_operation_policy",
    };

    private readonly IApplicationDbContext _db;
    private readonly IEmailSender _email;
    private readonly IEmailTemplateRenderer _templates;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;

    public PublicSiteService(
        IApplicationDbContext db,
        IEmailSender email,
        IEmailTemplateRenderer templates,
        IMemoryCache cache,
        IConfiguration configuration)
    {
        _db = db;
        _email = email;
        _templates = templates;
        _cache = cache;
        _configuration = configuration;
    }

    public async Task<ApiResult<IReadOnlyList<PublicFaqItemDto>>> GetFaqsAsync(string pageType, CancellationToken ct = default)
    {
        var type = (pageType ?? "").Trim();
        if (!AllowedFaqPages.Contains(type))
            return ApiResult<IReadOnlyList<PublicFaqItemDto>>.Fail("Invalid page type.");

        try
        {
            var rows = await _db.Faqs.AsNoTracking()
                .Where(f => f.PageType == type && (f.Status ?? "active").ToLower() == "active")
                .OrderBy(f => f.SortOrder)
                .ThenBy(f => f.Id)
                .Select(f => new { f.Id, f.Question, f.Answer })
                .ToListAsync(ct);

            IReadOnlyList<PublicFaqItemDto> items = rows
                .Select(r => new PublicFaqItemDto(r.Id, r.Question ?? "", r.Answer ?? ""))
                .ToList();
            return ApiResult<IReadOnlyList<PublicFaqItemDto>>.Ok(items);
        }
        catch (Exception ex)
        {
            return ApiResult<IReadOnlyList<PublicFaqItemDto>>.Fail("Unable to load FAQs: " + ex.Message);
        }
    }

    public async Task<ApiResult<PublicContentDto>> GetContentAsync(string contentType, CancellationToken ct = default)
    {
        var type = (contentType ?? "").Trim();
        if (!AllowedContentTypes.Contains(type))
            return ApiResult<PublicContentDto>.Fail("Invalid content type.");

        try
        {
            var row = await _db.ContentPages.AsNoTracking()
                .Where(c => c.ContentType == type && c.IsActive)
                .Select(c => new
                {
                    c.ContentType,
                    c.Title,
                    c.Content,
                    c.MetaDescription,
                    c.LastUpdated,
                })
                .FirstOrDefaultAsync(ct);

            if (row == null)
            {
                return ApiResult<PublicContentDto>.Ok(new PublicContentDto(
                    type,
                    TitleFor(type),
                    "<p>Content not available. Please contact administrator.</p>",
                    null,
                    null));
            }

            string? display = null;
            if (row.LastUpdated is { Year: > 1 } dt)
                display = dt.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture);

            return ApiResult<PublicContentDto>.Ok(new PublicContentDto(
                row.ContentType ?? type,
                string.IsNullOrWhiteSpace(row.Title) ? TitleFor(type) : row.Title!,
                row.Content ?? "",
                row.MetaDescription,
                display));
        }
        catch (Exception ex)
        {
            return ApiResult<PublicContentDto>.Fail("Unable to load content: " + ex.Message);
        }
    }

    public CaptchaChallengeDto CreateCaptcha()
    {
        const string alphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";
        Span<char> chars = stackalloc char[6];
        for (var i = 0; i < chars.Length; i++)
            chars[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];

        var code = new string(chars);
        var id = Guid.NewGuid().ToString("N");
        _cache.Set("captcha:" + id, code, TimeSpan.FromMinutes(10));
        return new CaptchaChallengeDto(id, code);
    }

    public async Task<ApiResult> SubmitFranchiseEnquiryAsync(FranchiseEnquiryRequest request, CancellationToken ct = default)
    {
        var name = (request.Name ?? "").Trim();
        var email = (request.Email ?? "").Trim();
        var contact = (request.Contact ?? "").Trim();
        var address = (request.Address ?? "").Trim();
        var message = (request.Message ?? "").Trim();
        var captchaId = (request.CaptchaId ?? "").Trim();
        var captchaInput = (request.CaptchaInput ?? "").Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(contact))
            return ApiResult.Fail("Please fill all required fields.");

        if (!contact.All(char.IsDigit) || contact.Length != 10)
            return ApiResult.Fail("Enter a valid 10-digit mobile number.");

        if (!_cache.TryGetValue("captcha:" + captchaId, out string? expected) || expected == null)
            return ApiResult.Fail("Captcha expired. Please try again.");

        _cache.Remove("captcha:" + captchaId);
        if (!string.Equals(captchaInput, expected, StringComparison.OrdinalIgnoreCase))
            return ApiResult.Fail("Invalid captcha! Please try again.");

        var vars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = name,
            ["email"] = email,
            ["phone"] = contact,
            ["state"] = address,
            ["message"] = System.Net.WebUtility.HtmlEncode(message).Replace("\n", "<br>"),
        };

        var fallbackBody = $@"
<strong>Name:</strong> {System.Net.WebUtility.HtmlEncode(name)}<br>
<strong>Email:</strong> {System.Net.WebUtility.HtmlEncode(email)}<br>
<strong>Contact:</strong> {System.Net.WebUtility.HtmlEncode(contact)}<br>
<strong>Address:</strong> {System.Net.WebUtility.HtmlEncode(address)}<br>
<strong>Message:</strong><br>{System.Net.WebUtility.HtmlEncode(message).Replace("\n", "<br>")}
";

        try
        {
            var supportTo = (_configuration["Smtp:FromEmail"] ?? "support@miniwebsite.in").Trim();
            var rendered = await _templates.TryRenderAsync("franchise_enquiry_admin", vars, ct);
            var subject = rendered?.Subject ?? $"Franchise Enquiry from {name}";
            var body = rendered?.Html ?? fallbackBody;
            await _email.SendAsync(supportTo, subject, body, ct);
            return ApiResult.Ok("Enquiry sent successfully.");
        }
        catch (Exception ex)
        {
            return ApiResult.Fail("Please try again or contact us directly at support@miniwebsite.in. " + ex.Message);
        }
    }

    private static string TitleFor(string type) => type switch
    {
        "privacy_policy" => "Privacy Policy",
        "terms_conditions" => "Terms & Conditions",
        "franchisee_agreement" => "Franchise Agreement",
        "franchisee_distributer" => "Distributor Agreement",
        "mw_full_franchise_agreement" => "Full Franchise Agreement",
        "mw_franchisee_operation_policy" => "Franchise Operation Policy",
        _ => "Content"
    };

}
