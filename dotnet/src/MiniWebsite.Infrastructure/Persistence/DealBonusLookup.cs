using Microsoft.EntityFrameworkCore;
using MiniWebsite.Application.Common.Extensions;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Domain.Common;

namespace MiniWebsite.Infrastructure.Persistence;

public class DealBonusLookup : IDealBonusLookup
{
    private readonly ApplicationDbContext _db;

    public DealBonusLookup(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<decimal> GetBonusAmountAsync(int? referrerUserId, string rolePrefix, string defaultCouponCode, CancellationToken ct = default)
    {
        const decimal fallback = 250m;

        try
        {
            var mapped = referrerUserId is null
                ? 0m
                : await (
                    from dcm in _db.DealCustomerMappings.AsNoTracking()
                    join c in _db.Coupons.AsNoTracking().ForRolePrefix(rolePrefix) on dcm.DealId equals c.Id
                    where dcm.UserId == referrerUserId
                          && c.Status == CouponValues.StatusActive
                    orderby dcm.CreatedAt descending
                    select c.BonusAmount
                ).FirstOrDefaultAsync(ct);

            if (mapped > 0) return mapped;

            var coupon = await _db.Coupons.AsNoTracking()
                .Where(c => c.CouponCode == defaultCouponCode && c.Status == CouponValues.StatusActive)
                .Select(c => c.BonusAmount)
                .FirstOrDefaultAsync(ct);

            return coupon > 0 ? coupon : fallback;
        }
        catch
        {
            return fallback;
        }
    }
}
