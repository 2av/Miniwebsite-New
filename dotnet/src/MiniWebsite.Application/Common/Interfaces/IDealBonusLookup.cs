namespace MiniWebsite.Application.Common.Interfaces;

public interface IDealBonusLookup
{
    Task<decimal> GetBonusAmountAsync(string referrerEmail, string planType, string defaultCouponCode, CancellationToken ct = default);
}
