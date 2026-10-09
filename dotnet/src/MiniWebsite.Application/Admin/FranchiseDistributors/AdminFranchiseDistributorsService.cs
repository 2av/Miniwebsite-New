using System.Globalization;
using Microsoft.EntityFrameworkCore;
using MiniWebsite.Application.Admin.FranchiseDistributors.Dtos;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Domain.Entities;
using MiniWebsite.Domain.Enums;
using MiniWebsite.Application.Common.Extensions;

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

        var emails = users.Select(u => Normalize(u.Email)).Distinct().ToList();
        var websiteCounts = await _db.Cards.AsNoTracking()
            .Where(c => c.UserEmail != null && emails.Contains(c.UserEmail.ToLower()))
            .GroupBy(c => c.UserEmail!.ToLower())
            .Select(g => new { Email = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var countMap = websiteCounts.ToDictionary(x => x.Email, x => x.Count, StringComparer.OrdinalIgnoreCase);

        var latestCards = await _db.Cards.AsNoTracking()
            .Where(c => c.UserEmail != null && emails.Contains(c.UserEmail.ToLower()))
            .OrderByDescending(c => c.Id)
            .ToListAsync(ct);
        var cardByEmail = new Dictionary<string, Card>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in latestCards)
        {
            var key = Normalize(c.UserEmail);
            if (!cardByEmail.ContainsKey(key))
                cardByEmail[key] = c;
        }

        var referrerEmails = users
            .Select(u => Normalize(u.Referral.ReferredBy))
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

        var frdPairs = await _db.ReferralEarnings.AsNoTracking()
            .Where(r => r.IsCollaboration == "YES" && emails.Contains(r.ReferredEmail.ToLower()))
            .Select(r => new { Referrer = r.ReferrerEmail.ToLower(), Referred = r.ReferredEmail.ToLower() })
            .ToListAsync(ct);
        var frdSet = frdPairs.Select(p => p.Referrer + "|" + p.Referred)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var mwDeals = await GetMappedDealsAsync(emails, "MiniWebsite", ct);
        var frDeals = await GetMappedDealsAsync(emails, "Franchise", ct);
        var invoices = await GetFranchiseInvoicesAsync(emails, ct);
        var pendingMap = await GetPendingAmountsAsync(emails, ct);
        var mwDealOptions = await ListActiveDealsAsync("MiniWebsite", ct);
        var frDealOptions = await ListActiveDealsAsync("Franchise", ct);

        var rows = users.Select(u =>
        {
            var email = Normalize(u.Email);
            countMap.TryGetValue(email, out var wc);
            cardByEmail.TryGetValue(email, out var card);
            mwDeals.TryGetValue(email, out var mwDeal);
            frDeals.TryGetValue(email, out var frDeal);
            invoices.TryGetValue(email, out var inv);
            inv ??= new InvoiceInfo(null, null, null, null);
            pendingMap.TryGetValue(email, out var pending);

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
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ApiResult.Ok("Influencer status updated successfully");
    }

    private async Task<Dictionary<string, MappedDealLiteDto>> GetMappedDealsAsync(
        IReadOnlyList<string> emails, string planType, CancellationToken ct)
    {
        var result = new Dictionary<string, MappedDealLiteDto>(StringComparer.OrdinalIgnoreCase);
        if (emails.Count == 0) return result;

        var rows = await (
            from m in _db.DealCustomerMappings.AsNoTracking()
            join d in _db.Deals.AsNoTracking() on m.DealId equals d.Id
            where d.PlanType == planType
            orderby m.Id descending
            select new
            {
                MappingId = m.Id,
                m.DealId,
                m.CustomerEmail,
                d.DealName,
                d.CouponCode,
            }).ToListAsync(ct);

        var set = emails.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            if (row.CustomerEmail == null) continue;
            var key = Normalize(row.CustomerEmail);
            if (!set.Contains(key) || result.ContainsKey(key)) continue;
            result[key] = new MappedDealLiteDto(row.MappingId, row.DealId, row.DealName ?? "", row.CouponCode);
        }
        return result;
    }

    private async Task<List<DealOptionLiteDto>> ListActiveDealsAsync(string planType, CancellationToken ct)
    {
        return await _db.Deals.AsNoTracking()
            .Where(d => d.DealStatus == "Active" && d.PlanType == planType)
            .OrderBy(d => d.DealName)
            .Select(d => new DealOptionLiteDto(d.Id, d.DealName ?? "", d.CouponCode, d.PlanType ?? planType))
            .ToListAsync(ct);
    }

    private async Task<Dictionary<string, InvoiceInfo>> GetFranchiseInvoicesAsync(
        IReadOnlyList<string> emails, CancellationToken ct)
    {
        var map = new Dictionary<string, InvoiceInfo>(StringComparer.OrdinalIgnoreCase);
        if (emails.Count == 0) return map;

        try
        {
            var serviceNames = new[] { "Franchisee Registration", "Joining Deal Payment" };
            var rows = await _db.Invoices.AsNoTracking()
                .Where(i => serviceNames.Contains(i.ServiceName))
                .OrderByDescending(i => i.Id)
                .Select(i => new
                {
                    i.Id,
                    i.UserEmail,
                    i.ServiceName,
                    i.TotalAmount,
                    i.PaymentStatus,
                })
                .ToListAsync(ct);

            var set = emails.ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows)
            {
                if (row.UserEmail == null) continue;
                var key = Normalize(row.UserEmail);
                if (!set.Contains(key)) continue;

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

    private async Task<Dictionary<string, decimal>> GetPendingAmountsAsync(
        IReadOnlyList<string> emails, CancellationToken ct)
    {
        var map = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        if (emails.Count == 0) return map;

        var totals = await _db.ReferralEarnings.AsNoTracking()
            .Where(r => emails.Contains(r.ReferrerEmail.ToLower()))
            .GroupBy(r => r.ReferrerEmail.ToLower())
            .Select(g => new { Email = g.Key, Total = g.Sum(x => x.Amount) })
            .ToListAsync(ct);

        foreach (var t in totals)
        {
            decimal paid = 0;
            try
            {
                paid = await (
                    from p in _db.ReferralPayments.AsNoTracking()
                    join e in _db.ReferralEarnings.AsNoTracking() on p.ReferralEarningId equals e.Id
                    where e.ReferrerEmail.ToLower() == t.Email
                    select (decimal?)p.Amount).SumAsync(ct) ?? 0m;
            }
            catch { /* ignore */ }

            map[t.Email] = Math.Max(0, t.Total - paid);
        }

        return map;
    }

    private static string BuildReferralSource(
        User user, Dictionary<string, User> referrers, HashSet<string> frdSet)
    {
        if (string.IsNullOrWhiteSpace(user.Referral.ReferredBy))
            return "Direct";

        var refEmail = Normalize(user.Referral.ReferredBy);
        if (!referrers.TryGetValue(refEmail, out var referrer))
            return "Ref: " + user.Referral.ReferredBy.Trim();

        var display = referrer.LoginRole switch
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
