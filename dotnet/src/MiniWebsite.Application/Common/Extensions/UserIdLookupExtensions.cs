using Microsoft.EntityFrameworkCore;
using MiniWebsite.Application.Common.Interfaces;

namespace MiniWebsite.Application.Common.Extensions;

public static class UserIdLookupExtensions
{
    /// <summary>
    /// Id of the user with this email, including soft-deleted users so their cards, commissions and payouts
    /// still resolve. Returns null for blank or unknown emails.
    /// </summary>
    public static async Task<int?> FindUserIdAsync(this IApplicationDbContext db, string? email, CancellationToken ct = default)
    {
        var key = (email ?? "").Trim();
        if (key.Length == 0) return null;
        return await db.Users.IgnoreQueryFilters().AsNoTracking()
            .Where(u => u.Email == key)
            .Select(u => (int?)u.Id)
            .FirstOrDefaultAsync(ct);
    }
}
