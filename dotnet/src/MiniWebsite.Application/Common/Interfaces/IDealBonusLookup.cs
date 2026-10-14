namespace MiniWebsite.Application.Common.Interfaces;

public interface IDealBonusLookup
{
    /// <param name="rolePrefix">Plan Type of the coupon, as a role prefix (e.g. SBU, FRP).</param>
    Task<decimal> GetBonusAmountAsync(int? referrerUserId, string rolePrefix, string defaultCouponCode, CancellationToken ct = default);
}
