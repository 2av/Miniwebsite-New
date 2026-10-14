namespace MiniWebsite.Application.Common.Interfaces;

public interface IWalletBalanceLookup
{
    Task<decimal> GetLatestBalanceAsync(int? userId, CancellationToken ct = default);
}
