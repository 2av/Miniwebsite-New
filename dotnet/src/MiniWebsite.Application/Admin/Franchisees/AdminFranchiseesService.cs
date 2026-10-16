using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MiniWebsite.Application.Admin.EmailTemplates;
using MiniWebsite.Application.Admin.Franchisees.Dtos;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Application.Common.Options;
using MiniWebsite.Domain.Entities;
using MiniWebsite.Domain.Enums;
using MiniWebsite.Application.Common.Extensions;
using MiniWebsite.Domain.Common;

namespace MiniWebsite.Application.Admin.Franchisees;

public class AdminFranchiseesService : IAdminFranchiseesService
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailSender _email;
    private readonly IEmailTemplateRenderer _templates;
    private readonly IConfiguration _configuration;
    private readonly AppOptions _app;

    public AdminFranchiseesService(
        IApplicationDbContext db,
        IPasswordHasher passwordHasher,
        IEmailSender email,
        IEmailTemplateRenderer templates,
        IConfiguration configuration,
        IOptions<AppOptions> app)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _email = email;
        _templates = templates;
        _configuration = configuration;
        _app = app.Value;
    }

    public async Task<ApiResult<FranchiseePageDto>> ListAsync(FranchiseeQuery query, CancellationToken ct = default)
    {
        query.Page = query.Page < 1 ? 1 : query.Page;
        query.PageSize = query.PageSize is < 1 or > 100 ? 10 : query.PageSize;

        var q = _db.Users.AsNoTracking().WhereRole(UserRole.Franchisee);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLower();
            q = q.Where(u =>
                u.Email.ToLower().Contains(s)
                || (u.FirstName + " " + u.LastName).ToLower().Contains(s)
                || (u.Phone != null && u.Phone.Contains(s)));
        }

        var total = await q.CountAsync(ct);
        var users = await q
            .OrderByDescending(u => u.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        var userIds = users.Select(u => u.Id).ToList();
        var referrerIds = users
            .Where(u => u.Referral.ReferrerUserId != null)
            .Select(u => u.Referral.ReferrerUserId!.Value)
            .Distinct()
            .ToList();

        var referrers = referrerIds.Count == 0
            ? new Dictionary<int, User>()
            : await _db.Users.AsNoTracking()
                .Where(u => referrerIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, ct);

        var cards = userIds.Count == 0
            ? []
            : await _db.Cards.AsNoTracking()
                .Where(c => c.FranchiseeUserId != null && userIds.Contains(c.FranchiseeUserId.Value))
                .OrderByDescending(c => c.Id)
                .ToListAsync(ct);

        var cardsByUser = cards
            .GroupBy(c => c.FranchiseeUserId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var invoices = await GetFranchiseInvoicesAsync(userIds, ct);
        var docs = await GetDocumentStatusesAsync(userIds, ct);
        var siteBase = (_configuration["App:SiteBaseUrl"] ?? "https://sharebizz.com").TrimEnd('/');
        var publicHost = (_configuration["App:PublicHost"] ?? "miniwebsite.in").Trim();

        var rows = new List<FranchiseeRowDto>();
        foreach (var u in users)
        {
            cardsByUser.TryGetValue(u.Id, out var userCards);
            userCards ??= [];
            var first = userCards.FirstOrDefault();
            invoices.TryGetValue(u.Id, out var inv);
            docs.TryGetValue(u.Id, out var docStatus);
            docStatus ??= "Not Uploaded";

            var isActive = string.Equals(u.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase);
            var (payLabel, payTone, paidOn) = ResolvePayment(inv);
            var fee = inv?.Amount ?? 0m;
            var company = first == null
                ? "-"
                : (!string.IsNullOrWhiteSpace(first.DGstName)
                    ? first.DGstName!
                    : (first.DCompName ?? "-"));
            if (string.IsNullOrWhiteSpace(company)) company = "-";

            var publicUrl = first != null ? $"https://{publicHost}/{first.Id}" : "";
            var editUrl = first != null
                ? $"{siteBase}/admin/select_theme.php?card_number={first.Id}&user_email={Uri.EscapeDataString(first.UserEmail ?? "")}"
                : "";

            rows.Add(new FranchiseeRowDto(
                u.Id,
                u.Email,
                u.FullName,
                u.Phone,
                u.CreatedAt,
                BuildReferralSource(u, referrers),
                company,
                isActive ? "Active" : "Inactive",
                isActive,
                first?.Id,
                first?.UserEmail,
                publicUrl,
                editUrl,
                payLabel,
                payTone,
                paidOn,
                fee,
                "₹" + fee.ToString("N2", CultureInfo.CreateSpecificCulture("en-IN")),
                inv?.InvoiceId,
                userCards.Count,
                MapDocumentLabel(docStatus),
                MapDocumentTone(docStatus)));
        }

        return ApiResult<FranchiseePageDto>.Ok(new FranchiseePageDto(rows, total, query.Page, query.PageSize));
    }

    public async Task<ApiResult> ActivateAsync(ActivateFranchiseeRequest request, CancellationToken ct = default)
    {
        if (request.Id <= 0)
            return ApiResult.Fail("Invalid franchisee account selected.");

        var user = await _db.Users.WhereRole(UserRole.Franchisee).FirstOrDefaultAsync(
            u => u.Id == request.Id, ct);
        if (user == null)
            return ApiResult.Fail("Franchisee not found.");

        if (string.Equals(user.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
            return ApiResult.Fail("Unable to activate this account. It may already be active.");

        user.Status = "ACTIVE";
        user.UpdatedAt = AppClock.Now;
        await _db.SaveChangesAsync(ct);
        return ApiResult.Ok("Franchisee account activated successfully.");
    }

    public async Task<ApiResult> CreateAsync(CreateFranchiseeRequest request, CancellationToken ct = default)
    {
        var firstName = (request.FirstName ?? "").Trim();
        var lastName = (request.LastName ?? "").Trim();
        var name = $"{firstName} {lastName}".Trim();
        var email = Normalize(request.Email);
        var phone = (request.Phone ?? "").Trim();
        var password = request.Password ?? "";

        if (firstName.Length == 0) return ApiResult.Fail("First Name is required");
        if (email.Length == 0) return ApiResult.Fail("Email is required");
        if (!email.Contains('@')) return ApiResult.Fail("Please enter a valid email address");
        if (!Regex.IsMatch(phone, @"^\d{10}$")) return ApiResult.Fail("Contact number must be exactly 10 digits");
        if (password.Length < 1) return ApiResult.Fail("Password is required");

        var exists = await _db.Users.AsNoTracking().WhereRole(UserRole.Franchisee).AnyAsync(
            u => u.Email.ToLower() == email, ct);
        if (exists)
            return ApiResult.Fail("A franchisee with this email already exists. Please use a different email ID.");

        var hash = _passwordHasher.Hash(password);
        _db.Users.Add(new User
        {
            AssignedRole = await _db.GetRoleByPrefixAsync(RolePrefixes.FranchisePartner, ct),
            Email = email,
            Phone = phone,
            FirstName = firstName,
            LastName = lastName,
            Password = hash,
            Status = "ACTIVE",
            CreatedAt = AppClock.Now,
            UpdatedAt = AppClock.Now
        });
        await _db.SaveChangesAsync(ct);

        try
        {
            var vars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["name"] = name,
                ["email"] = email,
                ["password"] = password,
                ["login_url"] = _app.FranchiseeLoginUrl,
                ["host"] = _app.PublicHost,
            };
            var rendered = await _templates.TryRenderAsync("franchisee_admin_created", vars, ct);
            var subject = rendered?.Subject ?? "Welcome to ShareBizz Franchisee";
            var html = rendered?.Html ?? $"""
                <p>Hello {System.Net.WebUtility.HtmlEncode(name)},</p>
                <p>Your franchisee account has been created.</p>
                <p><strong>Login Email:</strong> {System.Net.WebUtility.HtmlEncode(email)}<br/>
                <strong>Password:</strong> {System.Net.WebUtility.HtmlEncode(password)}</p>
                <p>Please complete your franchisee registration payment if required.</p>
                """;
            await _email.SendAsync(email, subject, html, ct);
            return ApiResult.Ok("Account Created! Welcome email sent successfully.");
        }
        catch
        {
            return ApiResult.Ok("Account Created! (Email sending failed)");
        }
    }

    public async Task<ApiResult<FranchiseeDashboardDto>> GetDashboardAsync(string email, CancellationToken ct = default)
    {
        var key = Normalize(email);
        if (key.Length == 0)
            return ApiResult<FranchiseeDashboardDto>.Fail("Invalid email.");

        var publicHost = (_configuration["App:PublicHost"] ?? "miniwebsite.in").Trim();
        var franchiseeId = await _db.FindUserIdAsync(key, ct);
        var cards = franchiseeId == null
            ? []
            : await _db.Cards.AsNoTracking()
                .Where(c => c.FranchiseeUserId == franchiseeId)
                .OrderByDescending(c => c.Id)
                .ToListAsync(ct);

        var sites = cards.Select(c =>
        {
            var status = string.Equals(c.ComplimentaryEnabled, "Yes", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(c.DPaymentStatus, "Success", StringComparison.OrdinalIgnoreCase)
                ? "Active"
                : "Trial / Unpaid";
            var pay = string.Equals(c.DPaymentStatus, "Success", StringComparison.OrdinalIgnoreCase)
                ? "Paid"
                : (c.DPaymentStatus ?? "Unpaid");
            return new FranchiseeWebsiteDto(
                c.Id,
                c.DCompName,
                c.UploadedDate,
                c.ValidityDate,
                status,
                pay,
                $"https://{publicHost}/{c.Id}");
        }).ToList();

        return ApiResult<FranchiseeDashboardDto>.Ok(new FranchiseeDashboardDto(key, sites));
    }

    private async Task<Dictionary<int, InvoiceInfo>> GetFranchiseInvoicesAsync(
        IReadOnlyList<int> userIds, CancellationToken ct)
    {
        var map = new Dictionary<int, InvoiceInfo>();
        if (userIds.Count == 0) return map;

        try
        {
            var rows = await _db.Invoices.AsNoTracking()
                .Where(i => i.ServiceName == "Franchisee Registration"
                    && i.UserId != null && userIds.Contains(i.UserId.Value))
                .OrderByDescending(i => i.Id)
                .Select(i => new
                {
                    i.Id,
                    UserId = i.UserId!.Value,
                    i.TotalAmount,
                    i.PaymentStatus,
                    i.InvoiceDate,
                })
                .ToListAsync(ct);

            foreach (var row in rows)
                map.TryAdd(row.UserId, new InvoiceInfo(row.Id, row.TotalAmount, row.PaymentStatus, row.InvoiceDate));
        }
        catch
        {
            // ignore
        }

        return map;
    }

    private async Task<Dictionary<int, string>> GetDocumentStatusesAsync(
        IReadOnlyList<int> userIds, CancellationToken ct)
    {
        var map = new Dictionary<int, string>();
        if (userIds.Count == 0) return map;

        try
        {
            var rows = await _db.FranchiseVerifications.AsNoTracking()
                .Where(v => userIds.Contains(v.UserId))
                .OrderByDescending(v => v.Id)
                .Select(v => new { v.UserId, v.Status })
                .ToListAsync(ct);

            foreach (var row in rows)
                map.TryAdd(row.UserId, row.Status ?? "Not Uploaded");
        }
        catch
        {
            // table may be missing
        }

        return map;
    }

    private static (string Label, string Tone, string? PaidOn) ResolvePayment(InvoiceInfo? inv)
    {
        if (inv == null)
            return ("Unpaid", "neutral", null);

        if (string.Equals(inv.PaymentStatus, "Success", StringComparison.OrdinalIgnoreCase))
        {
            var paidOn = inv.InvoiceDate.HasValue && inv.InvoiceDate.Value.Year > 1
                ? inv.InvoiceDate.Value.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)
                : AppClock.Now.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
            return ("Paid", "ok", paidOn);
        }

        return ("Unpaid", "neutral", null);
    }

    private static string MapDocumentLabel(string status) => status.Trim().ToLowerInvariant() switch
    {
        "submitted" => "Ready to Verify",
        "approved" => "Approved",
        "rejected" => "Not Approved",
        _ => "Not Uploaded"
    };

    private static string MapDocumentTone(string status) => status.Trim().ToLowerInvariant() switch
    {
        "submitted" => "warn",
        "approved" => "ok",
        "rejected" => "danger",
        _ => "neutral"
    };

    private static string BuildReferralSource(User user, Dictionary<int, User> referrers)
    {
        if (string.IsNullOrWhiteSpace(user.Referral.ReferredBy))
            return "Direct";

        var refBy = user.Referral.ReferredBy.Trim();
        if (int.TryParse(refBy, out var numericId))
            return "User - " + numericId;

        if (user.Referral.ReferrerUserId is not int referrerId
            || !referrers.TryGetValue(referrerId, out var referrer))
            return "User - " + refBy;

        return referrer.LoginRole switch
        {
            UserRole.Franchisee => "FR - " + referrer.Id.ToString("D3", CultureInfo.InvariantCulture),
            UserRole.Team => "Team - " + referrer.Id,
            UserRole.Admin => "Admin - " + referrer.Id,
            _ => "User - " + referrer.Id
        };
    }

    private static string Normalize(string? value) => (value ?? string.Empty).Trim().ToLowerInvariant();

    private sealed record InvoiceInfo(int InvoiceId, decimal Amount, string? PaymentStatus, DateTime? InvoiceDate);
}
