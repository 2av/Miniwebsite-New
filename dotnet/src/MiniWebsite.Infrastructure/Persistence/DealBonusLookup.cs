using Microsoft.EntityFrameworkCore;
using MiniWebsite.Application.Common.Interfaces;

namespace MiniWebsite.Infrastructure.Persistence;

public class DealBonusLookup : IDealBonusLookup
{
    private readonly ApplicationDbContext _db;

    public DealBonusLookup(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<decimal> GetBonusAmountAsync(string referrerEmail, string planType, string defaultCouponCode, CancellationToken ct = default)
    {
        const decimal fallback = 250m;

        try
        {
            var email = (referrerEmail ?? "").Trim().ToLowerInvariant();
            var mapped = await (
                from dcm in _db.DealCustomerMappings.AsNoTracking()
                join d in _db.Deals.AsNoTracking() on dcm.DealId equals d.Id
                where dcm.CustomerEmail.ToLower() == email
                      && d.DealStatus == "Active"
                      && d.PlanType == planType
                orderby dcm.CreatedAt descending
                select d.BonusAmount
            ).FirstOrDefaultAsync(ct);

            if (mapped > 0) return mapped;

            var coupon = await _db.Deals.AsNoTracking()
                .Where(d => d.CouponCode == defaultCouponCode && d.DealStatus == "Active")
                .Select(d => d.BonusAmount)
                .FirstOrDefaultAsync(ct);

            return coupon > 0 ? coupon : fallback;
        }
        catch
        {
            return fallback;
        }
    }
}
