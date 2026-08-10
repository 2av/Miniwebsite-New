using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MiniWebsite.Application.Admin.ManageCards.Dtos;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Application.Common.Options;
using MiniWebsite.Domain.Entities;
using MiniWebsite.Domain.Enums;

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

        var teamEmails = await _db.Users.AsNoTracking()
            .Where(u => u.Role == UserRole.Team)
            .Select(u => u.Email.ToLower())
            .ToListAsync(ct);
        var teamSet = teamEmails.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var cardsQuery = _db.Cards.AsNoTracking().AsQueryable();

        var paymentFilter = (query.PaymentFilter ?? "all").Trim().ToLowerInvariant();
        cardsQuery = paymentFilter switch
        {
            "paid" => cardsQuery.Where(c => c.DPaymentStatus == "Success"),
            "unpaid" or "trial" => cardsQuery.Where(c => c.DPaymentStatus == "Created"),
            _ => cardsQuery
        };

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLowerInvariant();
            cardsQuery = cardsQuery.Where(c =>
                (c.DCompName != null && c.DCompName.ToLower().Contains(s))
                || (c.UserEmail != null && c.UserEmail.ToLower().Contains(s))
                || (c.FUserEmail != null && c.FUserEmail.ToLower().Contains(s))
                || (c.CardId != null && c.CardId.ToLower().Contains(s))
                || c.Id.ToString().Contains(s));
        }

        var all = await cardsQuery.OrderByDescending(c => c.Id).ToListAsync(ct);
        all = all.Where(c =>
                string.IsNullOrWhiteSpace(c.UserEmail)
                || !teamSet.Contains(c.UserEmail.Trim().ToLowerInvariant()))
            .ToList();

        var total = all.Count;
        var pageRows = all
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        var userEmails = pageRows
            .Select(c => Normalize(c.UserEmail))
            .Where(e => e.Length > 0)
            .Distinct()
            .ToList();

        var users = await _db.Users.AsNoTracking()
            .Where(u => userEmails.Contains(u.Email.ToLower()))
            .ToListAsync(ct);
        var userByEmail = users
            .GroupBy(u => Normalize(u.Email))
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var referrerEmails = users
            .Select(u => Normalize(u.ReferredBy))
            .Where(e => e.Length > 0)
            .Distinct()
            .ToList();
        var referrers = referrerEmails.Count == 0
            ? new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase)
            : (await _db.Users.AsNoTracking()
                .Where(u => referrerEmails.Contains(u.Email.ToLower()))
                .ToListAsync(ct))
            .GroupBy(u => Normalize(u.Email))
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var cardIds = pageRows.Select(c => c.Id).ToList();
        var invoices = await GetInvoiceSummariesAsync(cardIds, ct);

        var frdPairs = await _db.ReferralEarnings.AsNoTracking()
            .Where(r => r.IsCollaboration == "YES" && userEmails.Contains(r.ReferredEmail.ToLower()))
            .Select(r => new { Referrer = r.ReferrerEmail.ToLower(), Referred = r.ReferredEmail.ToLower() })
            .ToListAsync(ct);
        var frdSet = frdPairs
            .Select(p => p.Referrer + "|" + p.Referred)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var siteBase = (_app.PhpSiteBaseUrl ?? "https://miniwebsite.in").TrimEnd('/');
        var rows = new List<ManageCardRowDto>(pageRows.Count);

        foreach (var c in pageRows)
        {
            userByEmail.TryGetValue(Normalize(c.UserEmail), out var user);
            invoices.TryGetValue(c.Id, out var inv);
            inv ??= new InvoiceSummary(null, null, false);

            var status = ResolveDisplayStatus(c);
            var validity = FormatValidity(c, status.Text, status.IsTrial);

            var company = !string.IsNullOrWhiteSpace(inv.BillingName)
                ? inv.BillingName!
                : (c.DCompName ?? "");

            var paid = string.Equals(c.DPaymentStatus, "Success", StringComparison.OrdinalIgnoreCase);
            rows.Add(new ManageCardRowDto(
                c.Id,
                c.CardId,
                c.UserEmail,
                c.FUserEmail,
                user?.Id,
                user?.Name,
                user?.Phone,
                BuildReferralSource(user, referrers, frdSet),
                company,
                c.UploadedDate,
                c.ValidityDate,
                c.DPaymentDate,
                c.DPaymentStatus,
                string.IsNullOrWhiteSpace(c.ComplimentaryEnabled) ? "No" : c.ComplimentaryEnabled,
                status.Text,
                status.Tone,
                status.IsTrial,
                validity.Display,
                validity.Tone,
                paid && c.DPaymentDate.HasValue
                    ? "Paid on " + c.DPaymentDate.Value.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)
                    : (paid ? "Paid" : "Unpaid"),
                FormatAmount(inv.TotalAmount),
                inv.HasInvoice && paid,
                !paid,
                string.IsNullOrWhiteSpace(c.CardId) ? siteBase : $"{siteBase}/{c.CardId}",
                $"{siteBase}/admin/select_theme.php?card_number={c.Id}&user_email={Uri.EscapeDataString(c.UserEmail ?? "")}"
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

        if (string.Equals(card.DPaymentStatus, "Success", StringComparison.OrdinalIgnoreCase))
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
                .Select(i => new { CardId = i.CardId!.Value, i.BillingName, i.TotalAmount })
                .ToListAsync(ct);

            foreach (var group in rows.GroupBy(r => r.CardId))
            {
                var first = group.First();
                map[group.Key] = new InvoiceSummary(
                    first.BillingName,
                    first.TotalAmount.ToString(CultureInfo.InvariantCulture),
                    true);
            }
        }
        catch
        {
            // invoice table may be unavailable — leave empty
        }

        return map;
    }

    private static string BuildReferralSource(
        User? user,
        Dictionary<string, User> referrers,
        HashSet<string> frdSet)
    {
        if (user == null || string.IsNullOrWhiteSpace(user.ReferredBy))
            return "Direct";

        var refEmail = Normalize(user.ReferredBy);
        if (!referrers.TryGetValue(refEmail, out var referrer))
            return "Ref: " + user.ReferredBy.Trim();

        var display = referrer.Role switch
        {
            UserRole.Franchisee => "FR - " + referrer.Id.ToString("D3", CultureInfo.InvariantCulture),
            UserRole.Team => "Team - " + referrer.Id,
            UserRole.Admin => "Admin - " + referrer.Id,
            _ => "User - " + referrer.Id
        };

        if (frdSet.Contains(refEmail + "|" + Normalize(user.Email)))
            display += " (FRD)";

        return display;
    }

    private static (string Text, string Tone, bool IsTrial) ResolveDisplayStatus(Card c)
    {
        if (string.Equals(c.ComplimentaryEnabled, "Yes", StringComparison.OrdinalIgnoreCase))
            return ("Active", "ok", false);

        if (string.Equals(c.DPaymentStatus, "Success", StringComparison.OrdinalIgnoreCase))
        {
            if (c.ValidityDate.HasValue && c.ValidityDate.Value < DateTime.UtcNow)
                return ("Expired on " + c.ValidityDate.Value.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture), "neutral", false);
            return ("Active", "ok", false);
        }

        var trialEnd = c.UploadedDate?.AddDays(7);
        var trialExpired = trialEnd.HasValue && trialEnd.Value < DateTime.UtcNow;
        var franchiseLinked = !string.IsNullOrWhiteSpace(c.FUserEmail) && c.FUserEmail.Trim().Length >= 3;

        if (franchiseLinked)
            return trialExpired ? ("Inactive", "neutral", false) : ("Pending Payment", "warn", false);

        if (trialExpired)
            return ("Inactive", "neutral", false);

        return ("7 Day Trial", "warn", true);
    }

    private static (string Display, string Tone) FormatValidity(Card c, string statusText, bool isTrial)
    {
        if (isTrial || statusText == "Inactive")
        {
            if (!c.UploadedDate.HasValue) return ("-", "neutral");
            var end = c.UploadedDate.Value.AddDays(7);
            var expired = end < DateTime.UtcNow || statusText == "Inactive";
            return (end.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture), expired ? "danger" : "ok");
        }

        if (statusText.StartsWith("Active", StringComparison.OrdinalIgnoreCase) || statusText == "Active")
        {
            if (c.ValidityDate.HasValue)
            {
                var expired = c.ValidityDate.Value < DateTime.UtcNow;
                return (c.ValidityDate.Value.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture),
                    expired ? "danger" : "ok");
            }

            if (string.Equals(c.ComplimentaryEnabled, "Yes", StringComparison.OrdinalIgnoreCase) && c.UploadedDate.HasValue)
            {
                var end = c.UploadedDate.Value.AddYears(1);
                return (end.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture),
                    end < DateTime.UtcNow ? "danger" : "ok");
            }

            if (c.DPaymentDate.HasValue)
            {
                var end = c.DPaymentDate.Value.AddYears(1);
                return (end.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture),
                    end < DateTime.UtcNow ? "danger" : "ok");
            }

            if (c.UploadedDate.HasValue)
            {
                var end = c.UploadedDate.Value.AddYears(1);
                return (end.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture),
                    end < DateTime.UtcNow ? "danger" : "ok");
            }
        }

        return ("-", "neutral");
    }

    private static string? FormatAmount(string? amount)
    {
        if (string.IsNullOrWhiteSpace(amount)) return null;
        var t = amount.Trim();
        if (t is "0" or "0.00") return null;
        return t;
    }

    private static string Normalize(string? value) => (value ?? string.Empty).Trim().ToLowerInvariant();

    private sealed record InvoiceSummary(string? BillingName, string? TotalAmount, bool HasInvoice);
}
