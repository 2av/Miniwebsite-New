using System.Globalization;
using Microsoft.EntityFrameworkCore;
using MiniWebsite.Application.Admin.AllOrders.Dtos;
using MiniWebsite.Application.Common.Extensions;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Domain.Common;

namespace MiniWebsite.Application.Admin.AllOrders;

public class AdminAllOrdersService : IAdminAllOrdersService
{
    private const int MaxCommentLength = 1000;

    private readonly IApplicationDbContext _db;

    public AdminAllOrdersService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ApiResult<AllOrdersPageDto>> ListAsync(AllOrdersQuery query, CancellationToken ct = default)
    {
        query.Page = query.Page < 1 ? 1 : query.Page;
        query.PageSize = query.PageSize is < 1 or > 100 ? 10 : query.PageSize;
        var offset = (query.Page - 1) * query.PageSize;
        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();

        try
        {
            var q = _db.Invoices.AsNoTracking().AsQueryable();
            if (search != null)
            {
                var term = search.ToLowerInvariant();
                var compact = term.Replace(" ", "");
                var matchedIds = _db.Users
                    .Where(u => u.UserCode.Contains(compact))
                    .Select(u => u.Id);
                q = q.Where(i =>
                    i.UserEmail.Contains(term)
                    || i.UserName.Contains(term)
                    || i.InvoiceNumber.Contains(term)
                    || i.OrderCode.Contains(compact)
                    || (i.UserId != null && matchedIds.Contains(i.UserId.Value)));
            }

            var totalCount = await q.CountAsync(ct);
            var invoices = await q
                .OrderByDescending(i => i.Id)
                .Skip(offset)
                .Take(query.PageSize)
                .Select(i => new InvoiceLite(
                    i.Id,
                    i.OrderCode,
                    i.UserId,
                    i.UserEmail,
                    i.InvoiceNumber,
                    i.CardId,
                    i.ServiceName,
                    i.ServiceDescription,
                    i.PlanName,
                    i.InvoiceDate,
                    i.OriginalAmount,
                    i.PromoDiscount,
                    i.DiscountAmount,
                    i.SubTotal,
                    i.IgstAmount + i.CgstAmount + i.SgstAmount,
                    i.TotalAmount,
                    i.AdminComment))
                .ToListAsync(ct);

            var buyerIds = invoices
                .Where(i => i.UserId != null)
                .Select(i => i.UserId!.Value)
                .Distinct()
                .ToList();

            var userById = buyerIds.Count == 0
                ? []
                : await _db.Users.AsNoTracking()
                    .Where(u => buyerIds.Contains(u.Id))
                    .Select(u => new { u.Id, u.UserCode, ReferredBy = u.Referral.ReferredBy, ReferrerId = u.Referral.ReferrerUserId })
                    .ToDictionaryAsync(u => u.Id, ct);

            var referrerIds = userById.Values
                .Where(u => u.ReferrerId != null)
                .Select(u => u.ReferrerId!.Value)
                .Distinct()
                .ToList();
            var referrerCodes = referrerIds.Count == 0
                ? new Dictionary<int, string>()
                : await _db.Users.AsNoTracking()
                    .Where(u => referrerIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, u => UserCodeGenerator.FormatDisplay(u.UserCode), ct);

            var earnings = buyerIds.Count == 0
                ? []
                : await _db.ReferralEarnings.AsNoTracking()
                    .Where(r => r.ReferredUserId != null && buyerIds.Contains(r.ReferredUserId.Value))
                    .Select(r => new { Referred = r.ReferredUserId!.Value, Referrer = r.ReferrerUserId, r.Amount, r.InvoiceId, r.ReferralDate })
                    .ToListAsync(ct);

            var buyerInvoices = earnings.Count == 0
                ? new Dictionary<int, List<(int Id, DateTime InvoiceDate)>>()
                : (await _db.Invoices.AsNoTracking()
                    .Where(i => i.UserId != null && buyerIds.Contains(i.UserId.Value))
                    .Select(i => new { i.Id, UserId = i.UserId!.Value, i.InvoiceDate })
                    .ToListAsync(ct))
                .GroupBy(i => i.UserId)
                .ToDictionary(g => g.Key, g => g.Select(i => (i.Id, i.InvoiceDate)).ToList());
            var commissions = earnings
                .Select(e => new
                {
                    e.Referred,
                    e.Referrer,
                    e.Amount,
                    InvoiceId = ReferralOrderMatching.ResolveInvoiceId(
                        e.InvoiceId, e.ReferralDate, buyerInvoices.GetValueOrDefault(e.Referred) ?? []),
                })
                .ToList();

            var cardIds = invoices.Where(i => i.CardId is > 0).Select(i => i.CardId!.Value).Distinct().ToList();
            var slugByCard = cardIds.Count == 0
                ? new Dictionary<int, string?>()
                : await _db.Cards.AsNoTracking()
                    .Where(c => cardIds.Contains(c.Id))
                    .ToDictionaryAsync(c => c.Id, c => c.Slug, ct);

            var rows = invoices.Select(inv =>
            {
                var user = inv.UserId is { } buyerId ? userById.GetValueOrDefault(buyerId) : null;

                var referralSource = "Direct";
                decimal referralAmount = 0;
                if (user?.ReferrerId is { } referrerId)
                {
                    referralSource = referrerCodes.TryGetValue(referrerId, out var code) && code.Length > 0
                        ? code
                        : "Ref: " + (user.ReferredBy ?? "").Trim();
                    referralAmount = commissions
                        .Where(e => e.InvoiceId == inv.Id && e.Referred == user.Id && e.Referrer == referrerId)
                        .Sum(e => e.Amount);
                }
                else if (!string.IsNullOrWhiteSpace(user?.ReferredBy))
                {
                    referralSource = "Ref: " + user.ReferredBy.Trim();
                }

                var dbId = "";
                if (inv.CardId is int cardId && slugByCard.TryGetValue(cardId, out var slug))
                    dbId = UserCodeGenerator.FormatDbId(slug);

                var net = inv.SubTotal > 0
                    ? inv.SubTotal
                    : Math.Max(0, inv.OriginalAmount - inv.PromoDiscount - inv.DiscountAmount);

                return new AllOrderRowDto(
                    inv.Id,
                    UserCodeGenerator.FormatDisplay(inv.OrderCode),
                    inv.InvoiceDate.Year <= 1 ? "" : inv.InvoiceDate.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture),
                    user == null ? "" : UserCodeGenerator.FormatDisplay(user.UserCode),
                    inv.UserEmail ?? "",
                    inv.CardId is > 0 ? inv.CardId : null,
                    dbId,
                    referralSource,
                    FormatRupees(referralAmount),
                    Describe(inv),
                    FormatNumber(inv.OriginalAmount),
                    FormatNumber(inv.PromoDiscount),
                    FormatNumber(net),
                    FormatRupees(inv.Gst),
                    inv.TotalAmount,
                    FormatRupees(inv.TotalAmount),
                    inv.InvoiceNumber ?? "",
                    inv.AdminComment ?? "");
            }).ToList();

            return ApiResult<AllOrdersPageDto>.Ok(new AllOrdersPageDto(
                rows,
                totalCount,
                query.Page,
                query.PageSize));
        }
        catch (Exception ex)
        {
            return ApiResult<AllOrdersPageDto>.Fail("Unable to load orders: " + ex.Message);
        }
    }

    public async Task<ApiResult> UpdateCommentAsync(int invoiceId, string? comment, CancellationToken ct = default)
    {
        var text = (comment ?? "").Trim();
        if (text.Length > MaxCommentLength)
            return ApiResult.Fail($"Comment must be {MaxCommentLength} characters or fewer.");

        var invoice = await _db.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId, ct);
        if (invoice == null)
            return ApiResult.Fail("Order not found.");

        invoice.AdminComment = text.Length == 0 ? null : text;
        invoice.UpdatedAt = AppClock.Now;
        await _db.SaveChangesAsync(ct);
        return ApiResult.Ok("Comment saved.");
    }

    private static string Describe(InvoiceLite inv)
    {
        var name = (inv.ServiceName ?? "").Trim();
        if (name.Length == 0) name = (inv.ServiceDescription ?? "").Trim();
        var plan = (inv.PlanName ?? "").Trim();
        if (plan.Length == 0 || name.Contains(plan, StringComparison.OrdinalIgnoreCase))
            return name;
        return name.Length == 0 ? plan : $"{name} ({plan})";
    }

    private static string FormatNumber(decimal value)
    {
        var rounded = decimal.Round(value, 2);
        return rounded == decimal.Truncate(rounded)
            ? decimal.Truncate(rounded).ToString(CultureInfo.InvariantCulture)
            : rounded.ToString("0.00", CultureInfo.InvariantCulture);
    }

    private static string FormatRupees(decimal value) => "₹ " + FormatNumber(value);

    private static string Normalize(string? value) => (value ?? string.Empty).Trim().ToLowerInvariant();

    private sealed record InvoiceLite(
        int Id,
        string OrderCode,
        int? UserId,
        string? UserEmail,
        string? InvoiceNumber,
        int? CardId,
        string? ServiceName,
        string? ServiceDescription,
        string? PlanName,
        DateTime InvoiceDate,
        decimal OriginalAmount,
        decimal PromoDiscount,
        decimal DiscountAmount,
        decimal SubTotal,
        decimal Gst,
        decimal TotalAmount,
        string? AdminComment);
}
