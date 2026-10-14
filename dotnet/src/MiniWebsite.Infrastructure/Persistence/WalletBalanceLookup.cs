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

    public async Task<decimal> GetLatestBalanceAsync(int? userId, CancellationToken ct = default)
    {
        if (userId is null or <= 0) return 0m;

        try
        {
            var balance = await _db.Wallets.AsNoTracking()
                .Where(w => w.UserId == userId)
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
