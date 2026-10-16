namespace MiniWebsite.Application.Common.Interfaces;

public interface IReferralEarningLookup
{
    /// <summary>
    /// Earning (without GST) for the referrer when someone they referred signs up, taken from the referrer's
    /// role terms in <c>plan_commissions</c> for the shortest active plan in <paramref name="planCategory"/>.
    /// Returns 0 when there is no referrer or no matching Fixed earning.
    /// </summary>
    Task<decimal> GetSignupEarningAsync(int? referrerUserId, string planCategory, CancellationToken ct = default);
}
