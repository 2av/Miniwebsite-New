using System.Globalization;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MiniWebsite.Application.Admin.EmailTemplates;
using MiniWebsite.Application.Admin.SalesChannelPartners.Dtos;
using MiniWebsite.Application.Common.Extensions;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Application.Common.Options;
using MiniWebsite.Domain.Common;
using MiniWebsite.Domain.Entities;
using MiniWebsite.Domain.Enums;

namespace MiniWebsite.Application.Admin.SalesChannelPartners;

public class AdminSalesChannelPartnersService : IAdminSalesChannelPartnersService
{
    /// <summary>Sidebar slug → roles.Prefix.</summary>
    private static readonly (string Slug, string Prefix)[] Partners =
    [
        ("franchisee-partner", RolePrefixes.FranchisePartner),
        ("print-shop-partner", RolePrefixes.PrintShopPartner),
        ("digital-marketing-partner", RolePrefixes.DigitalMarketingPartner),
        ("social-media-partner", RolePrefixes.SocialMediaPartner),
        ("field-sales-partner", RolePrefixes.FieldSalesPartner),
        ("field-sales-team", RolePrefixes.FieldSalesTeam),
    ];

    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailSender _email;
    private readonly IEmailTemplateRenderer _templates;
    private readonly AppOptions _app;

    public AdminSalesChannelPartnersService(
        IApplicationDbContext db,
        IPasswordHasher passwordHasher,
        IEmailSender email,
        IEmailTemplateRenderer templates,
        IOptions<AppOptions> app)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _email = email;
        _templates = templates;
        _app = app.Value;
    }

    public async Task<ApiResult<IReadOnlyList<SalesChannelPartnerRoleDto>>> ListRolesAsync(CancellationToken ct = default)
    {
        var roles = await _db.Roles.AsNoTracking().ToListAsync(ct);
        var result = new List<SalesChannelPartnerRoleDto>();
        foreach (var p in Partners)
        {
            var role = roles.Where(r => string.Equals(r.Prefix, p.Prefix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(r => r.RoleId)
                .FirstOrDefault();
            if (role != null) result.Add(MapRole(p.Slug, role));
        }
        return ApiResult<IReadOnlyList<SalesChannelPartnerRoleDto>>.Ok(result);
    }

    public async Task<ApiResult<SalesChannelPartnersPageDto>> ListAsync(
        string slug, SalesChannelPartnersQuery query, CancellationToken ct = default)
    {
        var (partner, role, error) = await ResolveAsync(slug, ct);
        if (error != null) return ApiResult<SalesChannelPartnersPageDto>.Fail(error);

        query.Page = query.Page < 1 ? 1 : query.Page;
        query.PageSize = query.PageSize is < 1 or > 100 ? 10 : query.PageSize;

        try
        {
            var q = _db.Users.AsNoTracking().Where(u => u.RoleId == role!.RoleId);
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var s = query.Search.Trim().ToLower();
                var compact = s.Replace(" ", "");
                q = q.Where(u =>
                    u.Email.Contains(s)
                    || (u.FirstName + " " + u.LastName).Contains(s)
                    || u.UserCode.Contains(compact)
                    || (u.Phone != null && u.Phone.Contains(s))
                    || (u.Address.District != null && u.Address.District.Contains(s))
                    || (u.Address.State != null && u.Address.State.Contains(s)));
            }

            var status = NormalizeStatusFilter(query.Status);
            if (status != null)
                q = q.Where(u => u.Status == status);

            var total = await q.CountAsync(ct);
            var users = await q
                .OrderByDescending(u => u.CreatedAt)
                .ThenByDescending(u => u.Id)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(ct);

            var facts = await LoadFactsAsync(users, ct);
            return ApiResult<SalesChannelPartnersPageDto>.Ok(new SalesChannelPartnersPageDto(
                MapRole(partner.Slug, role!),
                users.Select(u => MapRow(u, facts.GetValueOrDefault(u.Id))).ToList(),
                total,
                query.Page,
                query.PageSize));
        }
        catch (Exception ex)
        {
            return ApiResult<SalesChannelPartnersPageDto>.Fail("Unable to load partners: " + ex.Message);
        }
    }

    public async Task<ApiResult<SalesChannelPartnerRowDto>> CreateAsync(
        string slug, CreateSalesChannelPartnerRequest request, CancellationToken ct = default)
    {
        var (partner, role, error) = await ResolveAsync(slug, ct);
        if (error != null) return ApiResult<SalesChannelPartnerRowDto>.Fail(error);

        var firstName = (request.FirstName ?? "").Trim();
        var lastName = (request.LastName ?? "").Trim();
        var email = NormalizeEmail(request.Email);
        var phone = (request.Phone ?? "").Trim();
        var password = request.Password ?? "";

        if (firstName.Length == 0) return ApiResult<SalesChannelPartnerRowDto>.Fail("First name is required.");
        if (email.Length == 0 || !email.Contains('@')) return ApiResult<SalesChannelPartnerRowDto>.Fail("A valid email address is required.");
        if (password.Length < 6) return ApiResult<SalesChannelPartnerRowDto>.Fail("Password must be at least 6 characters long.");

        var conflict = await FindConflictAsync(null, email, phone, ct);
        if (conflict != null) return ApiResult<SalesChannelPartnerRowDto>.Fail(conflict);

        var hash = _passwordHasher.Hash(password);
        var user = new User
        {
            RoleId = role!.RoleId,
            Email = email,
            Phone = phone,
            FirstName = firstName,
            LastName = lastName,
            Address = new UserAddress
            {
                District = (request.District ?? "").Trim(),
                State = (request.State ?? "").Trim(),
            },
            Password = hash,
            Status = "ACTIVE",
            CreatedAt = AppClock.Now,
            UpdatedAt = AppClock.Now,
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        var message = $"{role.RoleName} added successfully.";
        try
        {
            await SendWelcomeAsync(RolePrefixes.ToUserRole(partner.Prefix), role, user.FullName, email, password, ct);
        }
        catch
        {
            message = $"{role.RoleName} created, but welcome email could not be sent.";
        }

        return ApiResult<SalesChannelPartnerRowDto>.Ok(MapRow(user, null), message);
    }

    public async Task<ApiResult<SalesChannelPartnerRowDto>> UpdateAsync(
        string slug, int id, UpdateSalesChannelPartnerRequest request, CancellationToken ct = default)
    {
        var (_, role, error) = await ResolveAsync(slug, ct);
        if (error != null) return ApiResult<SalesChannelPartnerRowDto>.Fail(error);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.RoleId == role!.RoleId, ct);
        if (user == null) return ApiResult<SalesChannelPartnerRowDto>.Fail($"{role!.RoleName} not found");

        var firstName = (request.FirstName ?? "").Trim();
        var lastName = (request.LastName ?? "").Trim();
        var email = NormalizeEmail(request.Email);
        var phone = (request.Phone ?? "").Trim();

        if (firstName.Length == 0) return ApiResult<SalesChannelPartnerRowDto>.Fail("First name is required.");
        if (email.Length == 0 || !email.Contains('@')) return ApiResult<SalesChannelPartnerRowDto>.Fail("A valid email address is required.");

        var conflict = await FindConflictAsync(user.Id, email, phone, ct);
        if (conflict != null) return ApiResult<SalesChannelPartnerRowDto>.Fail(conflict);

        await _db.StageUserEmailChangeAsync(user.Email, email, ct);
        user.FirstName = firstName;
        user.LastName = lastName;
        user.Email = email;
        user.Phone = phone;
        user.Address.District = (request.District ?? "").Trim();
        user.Address.State = (request.State ?? "").Trim();
        user.UpdatedAt = AppClock.Now;
        await _db.SaveChangesAsync(ct);

        return ApiResult<SalesChannelPartnerRowDto>.Ok(MapRow(user, null), $"{role!.RoleName} updated successfully.");
    }

    public async Task<ApiResult<SalesChannelPartnerRowDto>> ToggleStatusAsync(
        string slug, int id, ToggleSalesChannelPartnerStatusRequest request, CancellationToken ct = default)
    {
        var (_, role, error) = await ResolveAsync(slug, ct);
        if (error != null) return ApiResult<SalesChannelPartnerRowDto>.Fail(error);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.RoleId == role!.RoleId, ct);
        if (user == null) return ApiResult<SalesChannelPartnerRowDto>.Fail($"{role!.RoleName} not found");

        var requested = (request.NewStatus ?? "").Trim().ToUpperInvariant();
        user.Status = requested is "ACTIVE" or "INACTIVE"
            ? requested
            : string.Equals(user.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase) ? "INACTIVE" : "ACTIVE";
        user.UpdatedAt = AppClock.Now;
        await _db.SaveChangesAsync(ct);

        return ApiResult<SalesChannelPartnerRowDto>.Ok(MapRow(user, null), "Status updated.");
    }

    public async Task<ApiResult> ResetPasswordAsync(
        string slug, int id, ResetSalesChannelPartnerPasswordRequest request, CancellationToken ct = default)
    {
        var (_, role, error) = await ResolveAsync(slug, ct);
        if (error != null) return ApiResult.Fail(error);

        var password = (request.NewPassword ?? "").Trim();
        if (password.Length < 6)
            return ApiResult.Fail("Please provide a valid password (minimum 6 characters).");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.RoleId == role!.RoleId, ct);
        if (user == null) return ApiResult.Fail($"{role!.RoleName} not found");

        var hash = _passwordHasher.Hash(password);
        user.Password = hash;
        user.UpdatedAt = AppClock.Now;
        await _db.SaveChangesAsync(ct);
        return ApiResult.Ok("Password reset successfully.");
    }

    private async Task<((string Slug, string Prefix) Partner, Role? Role, string? Error)> ResolveAsync(
        string slug, CancellationToken ct)
    {
        var key = (slug ?? "").Trim().ToLowerInvariant();
        var partner = Partners.FirstOrDefault(p => p.Slug == key);
        if (partner.Slug == null) return (partner, null, "Unknown sales channel partner type.");

        var role = await _db.Roles.AsNoTracking()
            .Where(r => r.Prefix == partner.Prefix)
            .OrderBy(r => r.RoleId)
            .FirstOrDefaultAsync(ct);
        if (role == null)
            return (partner, null, $"Role with prefix '{partner.Prefix}' is missing in Role Management.");

        return (partner, role, null);
    }

    private async Task<string?> FindConflictAsync(int? excludeId, string email, string phone, CancellationToken ct)
    {
        // Soft-deleted users still hold the unique email index.
        var emailOwner = await _db.Users.IgnoreQueryFilters().AsNoTracking()
            .Where(u => (excludeId == null || u.Id != excludeId) && u.Email == email)
            .Select(u => new { RoleName = u.AssignedRole != null ? u.AssignedRole.RoleName : null })
            .FirstOrDefaultAsync(ct);
        if (emailOwner != null)
            return emailOwner.RoleName != null
                ? $"This email address is already registered as a {emailOwner.RoleName}. Please use a different email."
                : "This email address is already registered. Please use a different email.";

        if (phone.Length == 0) return null;

        var phoneTaken = await _db.Users.AsNoTracking()
            .AnyAsync(u => (excludeId == null || u.Id != excludeId) && u.Phone == phone, ct);
        return phoneTaken ? "This mobile number is already registered. Please use a different mobile number." : null;
    }

    private async Task SendWelcomeAsync(
        UserRole loginRole, Role role, string name, string email, string password, CancellationToken ct)
    {
        var loginUrl = string.IsNullOrWhiteSpace(_app.SiteBaseUrl)
            ? $"https://{_app.PublicHost}"
            : _app.SiteBaseUrl.TrimEnd('/');
        var vars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = name,
            ["email"] = email,
            ["password"] = password,
            ["login_url"] = loginUrl,
            ["host"] = _app.PublicHost,
        };
        var templateKey = loginRole == UserRole.Franchisee ? "franchisee_admin_created" : "team_member_welcome";
        var rendered = await _templates.TryRenderAsync(templateKey, vars, ct);
        var subject = rendered?.Subject ?? $"Welcome to {_app.PublicHost} — {role.RoleName}";
        var html = rendered?.Html ?? $"""
            <p>Hello {WebUtility.HtmlEncode(name)},</p>
            <p>Your {WebUtility.HtmlEncode(role.RoleName)} account has been created.</p>
            <p><strong>Login Email:</strong> {WebUtility.HtmlEncode(email)}<br/>
            <strong>Password:</strong> {WebUtility.HtmlEncode(password)}</p>
            <p>Please keep these credentials secure.</p>
            """;
        await _email.SendAsync(email, subject, html, ct);
    }

    private static SalesChannelPartnerRoleDto MapRole(string slug, Role role) => new(
        slug,
        role.RoleId,
        role.RoleName,
        role.RoleDescription ?? "",
        role.Prefix ?? "",
        role.SalesType ?? "");

    private async Task<Dictionary<int, PartnerRowFacts>> LoadFactsAsync(IReadOnlyList<User> users, CancellationToken ct)
    {
        var map = users.ToDictionary(u => u.Id, _ => PartnerRowFacts.Empty);
        if (users.Count == 0) return map;

        var userIds = users.Select(u => u.Id).ToList();

        var referrerIds = users
            .Where(u => u.Referral.ReferrerUserId != null)
            .Select(u => u.Referral.ReferrerUserId!.Value)
            .Distinct()
            .ToList();
        var referrerCodes = referrerIds.Count == 0
            ? new Dictionary<int, string>()
            : await _db.Users.AsNoTracking()
                .Where(u => referrerIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => UserCodeGenerator.FormatDisplay(u.UserCode), ct);

        var referred = await _db.Users.AsNoTracking()
            .Where(u => u.Referral.ReferrerUserId != null && userIds.Contains(u.Referral.ReferrerUserId.Value))
            .Select(u => new { Referrer = u.Referral.ReferrerUserId!.Value, u.Id })
            .ToListAsync(ct);
        var referredByPartner = referred
            .GroupBy(r => r.Referrer)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).Distinct().ToList());

        var ownerIds = userIds.Concat(referred.Select(r => r.Id)).Distinct().ToList();
        var cards = await _db.Cards.AsNoTracking()
            .Where(c =>
                (c.UserId != null && ownerIds.Contains(c.UserId.Value))
                || (c.FranchiseeUserId != null && userIds.Contains(c.FranchiseeUserId.Value)))
            .Select(c => new { c.Id, c.UserId, c.FranchiseeUserId })
            .ToListAsync(ct);

        var earnings = await _db.ReferralEarnings.AsNoTracking()
            .Where(r => r.ReferrerUserId != null && userIds.Contains(r.ReferrerUserId.Value))
            .Select(r => new { UserId = r.ReferrerUserId!.Value, r.Amount, r.Status })
            .ToListAsync(ct);
        var earningsByUser = earnings.GroupBy(r => r.UserId).ToDictionary(g => g.Key, g => g.ToList());

        var docs = await _db.FranchiseVerifications.AsNoTracking()
            .Where(v => userIds.Contains(v.UserId))
            .OrderByDescending(v => v.Id)
            .Select(v => new { v.UserId, v.Status, v.DocumentPath })
            .ToListAsync(ct);
        var docByUser = new Dictionary<int, (string Status, string? Path)>();
        foreach (var doc in docs)
            docByUser.TryAdd(doc.UserId, (doc.Status, doc.DocumentPath));

        var deals = await (
            from d in _db.UserJoiningDeals.AsNoTracking()
            join j in _db.JoiningDeals.AsNoTracking() on d.JoiningDealId equals j.Id
            where userIds.Contains(d.UserId)
            orderby d.Id descending
            select new
            {
                d.UserId,
                d.StartDate,
                d.ExpiryDate,
                d.PaymentStatus,
                d.PaymentDate,
                d.InvoiceId,
                Fees = j.Fees,
            }).ToListAsync(ct);
        var dealByUser = new Dictionary<int, (DateTime? Start, DateTime? Expiry, string PaymentStatus, DateTime? PaymentDate, int? InvoiceId, decimal Fees)>();
        foreach (var deal in deals)
        {
            if (!dealByUser.ContainsKey(deal.UserId))
                dealByUser[deal.UserId] = (deal.StartDate, deal.ExpiryDate, deal.PaymentStatus, deal.PaymentDate, deal.InvoiceId, deal.Fees);
        }

        var invoiceIds = dealByUser.Values.Where(d => d.InvoiceId != null).Select(d => d.InvoiceId!.Value).Distinct().ToList();
        var invoices = invoiceIds.Count == 0
            ? []
            : await _db.Invoices.AsNoTracking()
                .Where(i => invoiceIds.Contains(i.Id))
                .Select(i => new { i.Id, i.OriginalAmount, i.PromoDiscount, i.PromoCode, i.PaymentStatus, i.PaymentDate, i.InvoiceDate })
                .ToListAsync(ct);
        var invoiceById = invoices.ToDictionary(i => i.Id);

        foreach (var user in users)
        {
            var referralSource = "Direct";
            if (user.Referral.ReferrerUserId is { } referrerId
                && referrerCodes.TryGetValue(referrerId, out var code)
                && !string.IsNullOrWhiteSpace(code))
                referralSource = code;
            else if (!string.IsNullOrWhiteSpace(user.Referral.ReferredBy))
                referralSource = "Ref: " + user.Referral.ReferredBy.Trim();

            referredByPartner.TryGetValue(user.Id, out var referredIds);
            referredIds ??= [];
            var dbCount = cards.Count(c =>
                c.UserId == user.Id
                || c.FranchiseeUserId == user.Id
                || (c.UserId is { } ownerId && referredIds.Contains(ownerId)));

            earningsByUser.TryGetValue(user.Id, out var userEarnings);
            userEarnings ??= [];
            var earningTotal = userEarnings.Sum(e => e.Amount);
            var rejected = userEarnings.Count(e => string.Equals(e.Status, "Rejected", StringComparison.OrdinalIgnoreCase));
            var commission = earningTotal <= 0
                ? "Not Eligible"
                : userEarnings.Count > 0 && rejected == userEarnings.Count
                    ? "Rejected"
                    : "Eligible";

            var documentStatus = "Not Submitted";
            var documentTone = "neutral";
            string? pan = null, front = null, back = null;
            if (docByUser.TryGetValue(user.Id, out var doc))
            {
                documentStatus = MapDocumentStatus(doc.Status);
                documentTone = MapDocumentTone(documentStatus);
                (pan, front, back) = SplitDocuments(doc.Path);
            }

            var dateCreated = "";
            var expiry = "";
            var payment = "Unpaid";
            decimal fees = 0;
            decimal promo = 0;
            var coupon = "";
            int? invoiceId = null;
            if (dealByUser.TryGetValue(user.Id, out var deal))
            {
                dateCreated = FormatDay(deal.Start);
                expiry = FormatDay(deal.Expiry);
                fees = deal.Fees;
                invoiceId = deal.InvoiceId;
                var paid = IsPaid(deal.PaymentStatus);
                DateTime? paidOn = deal.PaymentDate;
                if (deal.InvoiceId is int id && invoiceById.TryGetValue(id, out var invoice))
                {
                    fees = invoice.OriginalAmount;
                    promo = invoice.PromoDiscount;
                    coupon = invoice.PromoCode ?? "";
                    paid = paid || IsPaid(invoice.PaymentStatus);
                    paidOn ??= invoice.PaymentDate ?? invoice.InvoiceDate;
                }
                if (paid)
                    payment = "Paid on " + (paidOn ?? AppClock.Now).ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
            }

            var refund = user.RefundStatus;
            if (string.IsNullOrWhiteSpace(refund) || refund.Equals("None", StringComparison.OrdinalIgnoreCase))
                refund = "";

            map[user.Id] = new PartnerRowFacts(
                referralSource,
                dateCreated,
                expiry,
                payment,
                fees,
                FormatMoney(fees, true),
                promo,
                FormatMoney(promo, true),
                coupon,
                invoiceId,
                dbCount,
                documentStatus,
                documentTone,
                PublicDoc(pan),
                PublicDoc(front),
                PublicDoc(back),
                referredIds.Count,
                earningTotal,
                FormatMoney(earningTotal, false),
                commission,
                refund);
        }

        return map;
    }

    private string? PublicDoc(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored)) return null;
        var path = stored.Trim().Replace('\\', '/');
        if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return path;
        var baseUrl = (_app.SiteBaseUrl ?? "").TrimEnd('/');
        return string.IsNullOrWhiteSpace(baseUrl) ? path : baseUrl + "/" + path.TrimStart('/');
    }

    private static (string? Pan, string? Front, string? Back) SplitDocuments(string? documentPath)
    {
        if (string.IsNullOrWhiteSpace(documentPath)) return (null, null, null);
        var parts = documentPath.Split('|', 3, StringSplitOptions.None);
        string? At(int i) => parts.Length > i && !string.IsNullOrWhiteSpace(parts[i]) ? parts[i].Trim() : null;
        return (At(0), At(1), At(2));
    }

    private static string MapDocumentStatus(string? status) => (status ?? "").Trim().ToLowerInvariant() switch
    {
        "submitted" or "pending review" => "Pending Review",
        "approved" or "verified" => "Approved",
        "resubmission" or "resubmission required" or "resubmit" => "Resubmission Required",
        "rejected" => "Rejected",
        _ => "Not Submitted",
    };

    private static string MapDocumentTone(string label) => label switch
    {
        "Approved" => "ok",
        "Rejected" => "danger",
        "Pending Review" or "Resubmission Required" => "warn",
        _ => "neutral",
    };

    private static bool IsPaid(string? status) =>
        status != null && status.Trim().ToLowerInvariant() is "success" or "paid";

    private static string FormatDay(DateTime? value) =>
        value is { Year: > 1 } date ? date.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture) : "";

    private static string FormatMoney(decimal amount, bool decimals)
    {
        var culture = CultureInfo.GetCultureInfo("en-IN");
        var text = decimals
            ? amount.ToString("N2", culture)
            : Math.Round(amount, 0).ToString("N0", culture);
        return "₹ " + text;
    }

    private static string FormatUserStatus(string? status)
    {
        if (string.Equals(status, "ACTIVE", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
            return "Active";
        return "InActive";
    }

    private static SalesChannelPartnerRowDto MapRow(User u, PartnerRowFacts? facts)
    {
        var status = (u.Status ?? "ACTIVE").ToUpperInvariant();
        var extra = facts ?? PartnerRowFacts.Empty;
        var joined = u.CreatedAt.Year < 2 ? "" : u.CreatedAt.ToString("dd-MMM-yy", CultureInfo.InvariantCulture);
        return new SalesChannelPartnerRowDto(
            u.Id,
            UserCodeGenerator.FormatDisplay(u.UserCode),
            u.FirstName,
            u.LastName,
            u.FullName,
            u.Email,
            u.Phone ?? "",
            u.Address.District ?? "",
            u.Address.State ?? "",
            status,
            status == "ACTIVE" ? "ok" : "danger",
            u.CreatedAt.Year < 2 ? "—" : u.CreatedAt.ToString("dd MMM yyyy", CultureInfo.InvariantCulture),
            joined,
            extra.ReferralSource,
            extra.DateCreatedDisplay,
            extra.ExpiryDateDisplay,
            FormatUserStatus(status),
            extra.UserPaymentStatus,
            extra.JoiningFees,
            extra.JoiningFeesDisplay,
            extra.PromoDiscount,
            extra.PromoDiscountDisplay,
            extra.CouponCode,
            extra.InvoiceId,
            extra.DbCount,
            extra.DocumentStatus,
            extra.DocumentStatusTone,
            extra.PanDocumentUrl,
            extra.FrontDocumentUrl,
            extra.BackDocumentUrl,
            extra.TotalReferred,
            extra.TotalReferralEarning,
            extra.TotalReferralEarningDisplay,
            extra.CommissionStatus,
            extra.RefundStatus);
    }

    private sealed record PartnerRowFacts(
        string ReferralSource,
        string DateCreatedDisplay,
        string ExpiryDateDisplay,
        string UserPaymentStatus,
        decimal JoiningFees,
        string JoiningFeesDisplay,
        decimal PromoDiscount,
        string PromoDiscountDisplay,
        string CouponCode,
        int? InvoiceId,
        int DbCount,
        string DocumentStatus,
        string DocumentStatusTone,
        string? PanDocumentUrl,
        string? FrontDocumentUrl,
        string? BackDocumentUrl,
        int TotalReferred,
        decimal TotalReferralEarning,
        string TotalReferralEarningDisplay,
        string CommissionStatus,
        string RefundStatus)
    {
        public static PartnerRowFacts Empty { get; } = new(
            "Direct", "", "", "Unpaid", 0, "₹ 0.00", 0, "₹ 0.00", "", null, 0,
            "Not Submitted", "neutral", null, null, null, 0, 0, "₹ 0", "Not Eligible", "");
    }

    private static string? NormalizeStatusFilter(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("all", StringComparison.OrdinalIgnoreCase))
            return null;
        var v = value.Trim().ToUpperInvariant();
        return v is "ACTIVE" or "INACTIVE" ? v : null;
    }

    private static string NormalizeEmail(string? email) => (email ?? "").Trim().ToLowerInvariant();
}
