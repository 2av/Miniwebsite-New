using Microsoft.EntityFrameworkCore;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Domain.Common;

namespace MiniWebsite.Infrastructure.Persistence;

public class ReferralEarningLookup : IReferralEarningLookup
{
    private readonly ApplicationDbContext _db;

    public ReferralEarningLookup(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<decimal> GetSignupEarningAsync(int? referrerUserId, string planCategory, CancellationToken ct = default)
    {
        if (referrerUserId is null) return 0m;

        var roleId = await _db.Users.AsNoTracking()
            .Where(u => u.Id == referrerUserId)
            .Select(u => (int?)u.RoleId)
            .FirstOrDefaultAsync(ct);
        if (roleId is null) return 0m;

        var earning = await _db.PlanCommissions.AsNoTracking()
            .Where(c => c.RoleId == roleId
                        && c.Status == PlanCommissionValues.StatusActive
                        && c.EarningType == PlanCommissionValues.EarningFixed
                        && c.PricingPlan.PlanCategory == planCategory
                        && c.PricingPlan.Status == "Active")
            .OrderBy(c => c.PricingPlan.ValidityMonths == null)
            .ThenBy(c => c.PricingPlan.ValidityMonths)
            .ThenBy(c => c.PricingPlan.OfferedPrice)
            .Select(c => (decimal?)c.EarningAmount)
            .FirstOrDefaultAsync(ct);

        return earning ?? 0m;
    }
}
