using Microsoft.EntityFrameworkCore;
using MiniWebsite.Application.Common.Interfaces;

namespace MiniWebsite.Infrastructure.Persistence;

public class WalletBalanceLookup : IWalletBalanceLookup
{
    private readonly ApplicationDbContext _db;

    public WalletBalanceLookup(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<decimal> GetLatestBalanceAsync(string franchiseeEmail, CancellationToken ct = default)
    {
        var email = (franchiseeEmail ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email)) return 0m;

        try
        {
            var balance = await _db.Wallets.AsNoTracking()
                .Where(w => w.UserEmail.ToLower() == email)
                .OrderByDescending(w => w.Id)
                .Select(w => (decimal?)w.Balance)
                .FirstOrDefaultAsync(ct);

            return balance ?? 0m;
        }
        catch
        {
            return 0m;
        }
    }
}
