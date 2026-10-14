using System.Globalization;
using Microsoft.EntityFrameworkCore;
using MiniWebsite.Application.Admin.FranchiseDistributors.Dtos;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Domain.Entities;
using MiniWebsite.Domain.Enums;
using MiniWebsite.Application.Common.Extensions;
using MiniWebsite.Domain.Common;

namespace MiniWebsite.Application.Admin.FranchiseDistributors;

public class AdminFranchiseDistributorsService : IAdminFranchiseDistributorsService
{
    private readonly IApplicationDbContext _db;

    public AdminFranchiseDistributorsService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ApiResult<FranchiseDistributorPageDto>> ListAsync(
        FranchiseDistributorQuery query, CancellationToken ct = default)
    {
        query.Page = query.Page < 1 ? 1 : query.Page;
        query.PageSize = query.PageSize is < 1 or > 100 ? 10 : query.PageSize;

        var q = _db.Users.AsNoTracking()
            .WhereRole(UserRole.Customer)
            .Where(u => u.Referral.CollaborationEnabled);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLowerInvariant();
            q = q.Where(u =>
                (u.FirstName + " " + u.LastName).ToLower().Contains(s)
                || u.Email.ToLower().Contains(s)
                || (u.Phone != null && u.Phone.ToLower().Contains(s)));
        }

        var total = await q.CountAsync(ct);
        var users = await q.OrderByDescending(u => u.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        var userIds = users.Select(u => u.Id).ToList();
        var websiteCounts = await _db.Cards.AsNoTracking()
            .Where(c => c.UserId != null && userIds.Contains(c.UserId.Value))
            .GroupBy(c => c.UserId!.Value)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var countMap = websiteCounts.ToDictionary(x => x.UserId, x => x.Count);

        var latestCards = await _db.Cards.AsNoTracking()
            .Where(c => c.UserId != null && userIds.Contains(c.UserId.Value))
            .OrderByDescending(c => c.Id)
            .ToListAsync(ct);
        var cardByUser = new Dictionary<int, Card>();
        foreach (var c in latestCards)
            cardByUser.TryAdd(c.UserId!.Value, c);

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

        var frdSet = (await _db.ReferralEarnings.AsNoTracking()
                .Where(r => r.IsCollaboration == "YES"
                    && r.ReferrerUserId != null
                    && r.ReferredUserId != null
                    && userIds.Contains(r.ReferredUserId.Value))
                .Select(r => new { Referrer = r.ReferrerUserId!.Value, Referred = r.ReferredUserId!.Value })
                .ToListAsync(ct))
            .Select(p => (p.Referrer, p.Referred))
            .ToHashSet();

        var mwDeals = await GetMappedDealsAsync(userIds, RolePrefixes.ShareBizzUser, ct);
        var frDeals = await GetMappedDealsAsync(userIds, RolePrefixes.FranchisePartner, ct);
        var invoices = await GetFranchiseInvoicesAsync(userIds, ct);
        var pendingMap = await GetPendingAmountsAsync(userIds, ct);
        var mwDealOptions = await ListActiveDealsAsync(RolePrefixes.ShareBizzUser, ct);
        var frDealOptions = await ListActiveDealsAsync(RolePrefixes.FranchisePartner, ct);

        var rows = users.Select(u =>
        {
            countMap.TryGetValue(u.Id, out var wc);
            cardByUser.TryGetValue(u.Id, out var card);
            mwDeals.TryGetValue(u.Id, out var mwDeal);
            frDeals.TryGetValue(u.Id, out var frDeal);
            invoices.TryGetValue(u.Id, out var inv);
            inv ??= new InvoiceInfo(null, null, null, null);
            pendingMap.TryGetValue(u.Id, out var pending);

            var feeDisplay = inv.FranchiseAmount is null
                ? "₹0.00"
                : "₹" + inv.FranchiseAmount.Value.ToString("0.00", CultureInfo.InvariantCulture);

            return new FranchiseDistributorRowDto(
                u.Id,
                u.Email,
                u.FullName,
                u.Phone,
                u.CreatedAt,
                BuildReferralSource(u, referrers, frdSet),
                string.IsNullOrWhiteSpace(card?.DCompName) ? "-" : card!.DCompName!,
                u.Referral.CollaborationEnabled ? "YES" : "NO",
                u.Referral.Influencer ? "YES" : "NO",
                u.Referral.CollaborationEnabled ? "Active" : "Inactive",
                string.IsNullOrWhiteSpace(card?.DPaymentStatus) ? "-" : card!.DPaymentStatus!,
                feeDisplay,
                inv.FranchiseInvoiceId,
                inv.JoiningDealInvoiceId,
                wc,
                pending,
                mwDeal,
                frDeal);
        }).ToList();

        return ApiResult<FranchiseDistributorPageDto>.Ok(new FranchiseDistributorPageDto(
            rows, mwDealOptions, frDealOptions, total, query.Page, query.PageSize));
    }

    public async Task<ApiResult> SetInfluencerAsync(SetInfluencerRequest request, CancellationToken ct = default)
    {
        var email = Normalize(request.Email);
        var status = NormalizeYesNo(request.Status);
        if (status is not ("YES" or "NO"))
            return ApiResult.Fail("Status must be YES or NO.");

        var user = await _db.Users.WhereRole(UserRole.Customer).FirstOrDefaultAsync(
            u => u.Email.ToLower() == email, ct);
        if (user == null)
            return ApiResult.Fail("User not found.");

        user.Referral.Influencer = status == "YES";
        user.UpdatedAt = AppClock.Now;
        await _db.SaveChangesAsync(ct);
        return ApiResult.Ok("Influencer status updated successfully");
    }

    private async Task<Dictionary<int, MappedDealLiteDto>> GetMappedDealsAsync(
        IReadOnlyList<int> userIds, string rolePrefix, CancellationToken ct)
    {
        var result = new Dictionary<int, MappedDealLiteDto>();
        if (userIds.Count == 0) return result;

        var rows = await (
            from m in _db.DealCustomerMappings.AsNoTracking()
            join d in _db.Coupons.AsNoTracking().ForRolePrefix(rolePrefix) on m.DealId equals d.Id
            where m.UserId != null && userIds.Contains(m.UserId.Value)
            orderby m.Id descending
            select new
            {
                MappingId = m.Id,
                m.DealId,
                UserId = m.UserId!.Value,
                d.CouponName,
                d.CouponCode,
            }).ToListAsync(ct);

        foreach (var row in rows)
            result.TryAdd(row.UserId, new MappedDealLiteDto(row.MappingId, row.DealId, row.CouponName ?? "", row.CouponCode));
        return result;
    }

    private async Task<List<DealOptionLiteDto>> ListActiveDealsAsync(string rolePrefix, CancellationToken ct)
    {
        return await _db.Coupons.AsNoTracking()
            .ForRolePrefix(rolePrefix)
            .Usable()
            .OrderBy(d => d.CouponName)
            .Select(d => new DealOptionLiteDto(d.Id, d.CouponName ?? "", d.CouponCode, d.Role!.RoleName))
            .ToListAsync(ct);
    }

    private async Task<Dictionary<int, InvoiceInfo>> GetFranchiseInvoicesAsync(
        IReadOnlyList<int> userIds, CancellationToken ct)
    {
        var map = new Dictionary<int, InvoiceInfo>();
        if (userIds.Count == 0) return map;

        try
        {
            var serviceNames = new[] { "Franchisee Registration", "Joining Deal Payment" };
            var rows = await _db.Invoices.AsNoTracking()
                .Where(i => serviceNames.Contains(i.ServiceName)
                    && i.UserId != null && userIds.Contains(i.UserId.Value))
                .OrderByDescending(i => i.Id)
                .Select(i => new
                {
                    i.Id,
                    UserId = i.UserId!.Value,
                    i.ServiceName,
                    i.TotalAmount,
                    i.PaymentStatus,
                })
                .ToListAsync(ct);

            foreach (var row in rows)
            {
                var key = row.UserId;
                map.TryGetValue(key, out var existing);
                existing ??= new InvoiceInfo(null, null, null, null);

                if (row.ServiceName == "Franchisee Registration" && existing.FranchiseInvoiceId == null)
                {
                    existing = existing with
                    {
                        FranchiseInvoiceId = row.Id,
                        FranchiseAmount = row.TotalAmount,
                        FranchisePaid = string.Equals(row.PaymentStatus, "Success", StringComparison.OrdinalIgnoreCase)
                    };
                }
                else if (row.ServiceName == "Joining Deal Payment" && existing.JoiningDealInvoiceId == null)
                {
                    existing = existing with { JoiningDealInvoiceId = row.Id };
                }

                map[key] = existing;
            }
        }
        catch
        {
            // ignore if table unavailable
        }

        return map;
    }

    private async Task<Dictionary<int, decimal>> GetPendingAmountsAsync(
        IReadOnlyList<int> userIds, CancellationToken ct)
    {
        var map = new Dictionary<int, decimal>();
        if (userIds.Count == 0) return map;

        var totals = await _db.ReferralEarnings.AsNoTracking()
            .Where(r => r.ReferrerUserId != null && userIds.Contains(r.ReferrerUserId.Value))
            .GroupBy(r => r.ReferrerUserId!.Value)
            .Select(g => new { UserId = g.Key, Total = g.Sum(x => x.Amount) })
            .ToListAsync(ct);

        foreach (var t in totals)
        {
            decimal paid = 0;
            try
            {
                paid = await (
                    from p in _db.ReferralPayments.AsNoTracking()
                    join e in _db.ReferralEarnings.AsNoTracking() on p.ReferralEarningId equals e.Id
                    where e.ReferrerUserId == t.UserId
                    select (decimal?)p.Amount).SumAsync(ct) ?? 0m;
            }
            catch { /* ignore */ }

            map[t.UserId] = Math.Max(0, t.Total - paid);
        }

        return map;
    }

    private static string BuildReferralSource(
        User user, Dictionary<int, User> referrers, HashSet<(int Referrer, int Referred)> frdSet)
    {
        User? referrer = null;
        if (user.Referral.ReferrerUserId is int referrerId)
            referrers.TryGetValue(referrerId, out referrer);

        if (referrer == null)
            return string.IsNullOrWhiteSpace(user.Referral.ReferredBy)
                ? "Direct"
                : "Ref: " + user.Referral.ReferredBy.Trim();

        var display = referrer.LoginRole switch
        {
            UserRole.Franchisee => "FR - " + referrer.Id.ToString("D3", CultureInfo.InvariantCulture),
            UserRole.Team => "Team - " + referrer.Id,
            UserRole.Admin => "Admin - " + referrer.Id,
            _ => "User - " + referrer.Id
        };

        if (frdSet.Contains((referrer.Id, user.Id)))
            display += " (FRD)";

        return display;
    }

    private static string Normalize(string? value) => (value ?? string.Empty).Trim().ToLowerInvariant();

    private static string NormalizeYesNo(string? value)
    {
        var v = (value ?? "NO").Trim().ToUpperInvariant();
        return v == "YES" ? "YES" : "NO";
    }

    private sealed record InvoiceInfo(
        int? FranchiseInvoiceId,
        int? JoiningDealInvoiceId,
        decimal? FranchiseAmount,
        bool? FranchisePaid);
}
