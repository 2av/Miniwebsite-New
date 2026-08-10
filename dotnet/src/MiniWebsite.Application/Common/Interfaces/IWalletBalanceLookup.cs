namespace MiniWebsite.Application.Common.Interfaces;

public interface IWalletBalanceLookup
{
    Task<decimal> GetLatestBalanceAsync(string franchiseeEmail, CancellationToken ct = default);
}
