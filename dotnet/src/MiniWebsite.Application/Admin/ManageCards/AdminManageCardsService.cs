using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MiniWebsite.Application.Admin.ManageCards.Dtos;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Application.Common.Options;
using MiniWebsite.Domain.Common;
using MiniWebsite.Domain.Entities;
using MiniWebsite.Domain.Enums;
using MiniWebsite.Application.Common.Extensions;

namespace MiniWebsite.Application.Admin.ManageCards;

public class AdminManageCardsService : IAdminManageCardsService
{
    private readonly IApplicationDbContext _db;
    private readonly AppOptions _app;

    public AdminManageCardsService(IApplicationDbContext db, IOptions<AppOptions> app)
    {
        _db = db;
        _app = app.Value;
    }

    public async Task<ApiResult<ManageCardsPageDto>> ListAsync(ManageCardsQuery query, CancellationToken ct = default)
    {
        query.Page = query.Page < 1 ? 1 : query.Page;
        query.PageSize = query.PageSize is < 1 or > 100 ? 10 : query.PageSize;

        var teamIds = _db.Users
            .WhereRole(UserRole.Team)
            .Select(u => u.Id);

        var cardsQuery = _db.Cards.AsNoTracking()
            .Where(c => c.UserId == null || !teamIds.Contains(c.UserId.Value));

        var paymentFilter = (query.PaymentFilter ?? "all").Trim().ToLowerInvariant();
        cardsQuery = paymentFilter switch
        {
            "paid" => cardsQuery.Where(c => _db.Invoices.Any(i =>
                i.CardId == c.Id && (i.PaymentStatus == "Success" || i.PaymentStatus == "Paid"))),
            "unpaid" or "trial" => cardsQuery.Where(c =>
                !_db.Invoices.Any(i => i.CardId == c.Id && i.PaymentStatus != "Created")),
            _ => cardsQuery
        };

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLowerInvariant();
            var compact = s.Replace(" ", "");
            var matchedIds = _db.Users
                .Where(u => u.UserCode.Replace(" ", "").Contains(compact))
                .Select(u => u.Id);
            cardsQuery = cardsQuery.Where(c =>
                (c.CompanyName != null && c.CompanyName.Contains(s))
                || (c.UserEmail != null && c.UserEmail.Contains(s))
                || (c.FranchiseeEmail != null && c.FranchiseeEmail.Contains(s))
                || (c.Slug != null && (c.Slug.Contains(s) || c.Slug.Replace(" ", "").Contains(compact)))
                || (c.UserId != null && matchedIds.Contains(c.UserId.Value))
                || c.Id.ToString().Contains(s));
        }

        var total = await cardsQuery.CountAsync(ct);
        var pageRows = await cardsQuery
            .OrderByDescending(c => c.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new Card
            {
                Id = c.Id,
                Slug = c.Slug,
                UserId = c.UserId,
                UserEmail = c.UserEmail,
                FranchiseeEmail = c.FranchiseeEmail,
                CompanyName = c.CompanyName,
                CreatedAt = c.CreatedAt,
                ValidityDate = c.ValidityDate,
                CardStatus = c.CardStatus,
                ComplimentaryEnabled = c.ComplimentaryEnabled,
            })
            .ToListAsync(ct);

        var userIds = pageRows
            .Where(c => c.UserId != null)
            .Select(c => c.UserId!.Value)
            .Distinct()
            .ToList();

        var userById = await _db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, ct);

        var referrerIds = userById.Values
            .Where(u => u.Referral.ReferrerUserId != null)
            .Select(u => u.Referral.ReferrerUserId!.Value)
            .Distinct()
            .ToList();
        var referrers = referrerIds.Count == 0
            ? new Dictionary<int, User>()
            : await _db.Users.AsNoTracking()
                .Where(u => referrerIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, ct);

        var cardIds = pageRows.Select(c => c.Id).ToList();
        var invoices = await GetInvoiceSummariesAsync(cardIds, ct);

        var siteBase = (_app.SiteBaseUrl ?? "https://sharebizz.com").TrimEnd('/');
        var rows = new List<ManageCardRowDto>(pageRows.Count);

        foreach (var c in pageRows)
        {
            User? user = null;
            if (c.UserId is { } ownerId) userById.TryGetValue(ownerId, out user);
            invoices.TryGetValue(c.Id, out var inv);
            inv ??= new InvoiceSummary(null, null, false, "", null, false, false);

            var expiry = ExpiryDate(c);
            var status = ResolveProfileStatus(c, expiry);
            var validity = FormatValidity(c, status.Text, inv.PaymentDate);
            var company = c.CompanyName ?? "";
            var paidOn = inv.PaymentDate;
            var paid = inv.Paid;
            var rejected = !paid && inv.Rejected;
            var orderAmount = inv.HasInvoice ? inv.TotalAmount : null;
            var paymentStatus = paid ? "Success" : rejected ? "Rejected" : inv.HasInvoice ? "Pending" : "Created";
            var paymentLabel = paid
                ? "Paid on " + (paidOn ?? c.CreatedAt ?? AppClock.Now).ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)
                : rejected ? "Rejected" : "Pending";

            rows.Add(new ManageCardRowDto(
                c.Id,
                c.Slug,
                c.UserEmail,
                c.FranchiseeEmail,
                user?.Id,
                user?.FullName,
                user?.Phone,
                BuildReferralSource(user, referrers),
                company,
                c.CreatedAt,
                c.ValidityDate,
                paidOn,
                paymentStatus,
                string.IsNullOrWhiteSpace(c.ComplimentaryEnabled) ? "No" : c.ComplimentaryEnabled,
                status.Text,
                status.Tone,
                false,
                validity.Display,
                validity.Tone,
                paymentLabel,
                orderAmount?.ToString(CultureInfo.InvariantCulture),
                inv.HasInvoice,
                !paid,
                string.IsNullOrWhiteSpace(c.Slug) ? siteBase : $"{siteBase}/{c.Slug}",
                $"{siteBase}/admin/select_theme.php?card_number={c.Id}&user_email={Uri.EscapeDataString(c.UserEmail ?? "")}",
                user == null ? "" : UserCodeGenerator.FormatDisplay(user.UserCode),
                UserCodeGenerator.FormatDbId(c.Slug),
                FormatDay(c.CreatedAt),
                FormatDay(expiry),
                inv.PromoCode,
                FormatOrderValue(orderAmount)
            ));
        }

        return ApiResult<ManageCardsPageDto>.Ok(new ManageCardsPageDto(rows, total, query.Page, query.PageSize));
    }

    public async Task<ApiResult> SetComplimentaryAsync(int cardId, string status, CancellationToken ct = default)
    {
        var normalized = status.Trim();
        if (!normalized.Equals("Yes", StringComparison.OrdinalIgnoreCase)
            && !normalized.Equals("No", StringComparison.OrdinalIgnoreCase))
            return ApiResult.Fail("Status must be Yes or No.");

        var card = await _db.Cards.FirstOrDefaultAsync(c => c.Id == cardId, ct);
        if (card == null)
            return ApiResult.Fail("Card not found.");

        var paid = await _db.Invoices.AnyAsync(i =>
            i.CardId == cardId && (i.PaymentStatus == "Success" || i.PaymentStatus == "Paid"), ct);
        if (paid)
            return ApiResult.Fail("Complimentary disabled for paid cards.");

        card.ComplimentaryEnabled = normalized.Equals("Yes", StringComparison.OrdinalIgnoreCase) ? "Yes" : "No";
        await _db.SaveChangesAsync(ct);
        return ApiResult.Ok("Complimentary updated to: " + card.ComplimentaryEnabled);
    }

    private async Task<Dictionary<int, InvoiceSummary>> GetInvoiceSummariesAsync(
        IReadOnlyList<int> cardIds, CancellationToken ct)
    {
        var map = new Dictionary<int, InvoiceSummary>();
        if (cardIds.Count == 0) return map;

        try
        {
            var rows = await _db.Invoices.AsNoTracking()
                .Where(i => i.CardId != null && cardIds.Contains(i.CardId.Value))
                .Select(i => new
                {
                    CardId = i.CardId!.Value,
                    i.Id,
                    i.BillingName,
                    i.TotalAmount,
                    i.PromoCode,
                    i.PaymentStatus,
                    i.PaymentDate,
                })
                .ToListAsync(ct);

            foreach (var group in rows.GroupBy(r => r.CardId))
            {
                var latest = group.OrderByDescending(r => r.Id).First();
                var paid = group
                    .Where(r => IsPaid(r.PaymentStatus))
                    .OrderByDescending(r => r.PaymentDate ?? DateTime.MinValue)
                    .ThenByDescending(r => r.Id)
                    .FirstOrDefault();
                var payment = paid ?? latest;
                map[group.Key] = new InvoiceSummary(
                    latest.BillingName,
                    payment.TotalAmount,
                    true,
                    latest.PromoCode ?? "",
                    payment.PaymentDate,
                    paid != null,
                    paid == null && IsRejected(latest.PaymentStatus));
            }
        }
        catch
        {
            // invoice table may be unavailable — leave empty
        }

        return map;
    }

    private static string BuildReferralSource(User? user, Dictionary<int, User> referrers)
    {
        if (user == null) return "Direct";

        if (user.Referral.ReferrerUserId is { } referrerId && referrers.TryGetValue(referrerId, out var referrer))
        {
            var code = UserCodeGenerator.FormatDisplay(referrer.UserCode);
            if (!string.IsNullOrWhiteSpace(code)) return code;
        }

        return string.IsNullOrWhiteSpace(user.Referral.ReferredBy)
            ? "Direct"
            : "Ref: " + user.Referral.ReferredBy.Trim();
    }

    private static (string Text, string Tone) ResolveProfileStatus(Card c, DateTime? expiry)
    {
        var raw = (c.CardStatus ?? "").Trim();
        if (raw.Equals("Suspended", StringComparison.OrdinalIgnoreCase)
            || raw.Equals("Inactive", StringComparison.OrdinalIgnoreCase)
            || raw.Equals("InActive", StringComparison.OrdinalIgnoreCase))
            return ("Suspended", "danger");

        if (expiry.HasValue && expiry.Value.Date < AppClock.Now.Date)
            return ("Expired on " + expiry.Value.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture), "neutral");

        return ("Active", "ok");
    }

    private static DateTime? ExpiryDate(Card c)
    {
        if (c.ValidityDate.HasValue) return c.ValidityDate;
        if (c.CreatedAt.HasValue) return c.CreatedAt.Value.AddYears(1);
        return null;
    }

    private static string FormatDay(DateTime? value) =>
        value.HasValue ? value.Value.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture) : "";

    private static bool IsPaid(string? status) =>
        string.Equals(status, "Success", StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, "Paid", StringComparison.OrdinalIgnoreCase);

    private static bool IsRejected(string? status) =>
        string.Equals(status, "Rejected", StringComparison.OrdinalIgnoreCase);

    private static string FormatOrderValue(decimal? amount)
    {
        var value = decimal.Round(amount ?? 0, 2);
        var text = value == decimal.Truncate(value)
            ? decimal.Truncate(value).ToString(CultureInfo.InvariantCulture)
            : value.ToString("0.00", CultureInfo.InvariantCulture);
        return "₹ " + text;
    }

    private static (string Display, string Tone) FormatValidity(Card c, string statusText, DateTime? paidOn)
    {
        if (statusText == "Inactive")
        {
            if (!c.UploadedDate.HasValue) return ("-", "neutral");
            var end = c.UploadedDate.Value.AddDays(7);
            var expired = end < AppClock.Now || statusText == "Inactive";
            return (end.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture), expired ? "danger" : "ok");
        }

        if (statusText.StartsWith("Active", StringComparison.OrdinalIgnoreCase) || statusText == "Active")
        {
            if (c.ValidityDate.HasValue)
            {
                var expired = c.ValidityDate.Value < AppClock.Now;
                return (c.ValidityDate.Value.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture),
                    expired ? "danger" : "ok");
            }

            if (string.Equals(c.ComplimentaryEnabled, "Yes", StringComparison.OrdinalIgnoreCase) && c.UploadedDate.HasValue)
            {
                var end = c.UploadedDate.Value.AddYears(1);
                return (end.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture),
                    end < AppClock.Now ? "danger" : "ok");
            }

            if (paidOn.HasValue)
            {
                var end = paidOn.Value.AddYears(1);
                return (end.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture),
                    end < AppClock.Now ? "danger" : "ok");
            }

            if (c.UploadedDate.HasValue)
            {
                var end = c.UploadedDate.Value.AddYears(1);
                return (end.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture),
                    end < AppClock.Now ? "danger" : "ok");
            }
        }

        return ("-", "neutral");
    }

    private static string Normalize(string? value) => (value ?? string.Empty).Trim().ToLowerInvariant();

    private sealed record InvoiceSummary(
        string? BillingName,
        decimal? TotalAmount,
        bool HasInvoice,
        string PromoCode,
        DateTime? PaymentDate,
        bool Paid,
        bool Rejected);
}
