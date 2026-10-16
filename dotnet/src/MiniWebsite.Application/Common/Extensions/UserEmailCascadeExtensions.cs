using Microsoft.EntityFrameworkCore;
using MiniWebsite.Application.Common.Interfaces;

namespace MiniWebsite.Application.Common.Extensions;

public static class UserEmailCascadeExtensions
{
    /// <summary>
    /// Rewrites every stored copy of a user's email (ownership, referral, payout and document links)
    /// to <paramref name="newEmail"/>. Changes are only tracked; the caller's <c>SaveChangesAsync</c> commits
    /// them together with the user row in one transaction. Contact and billing snapshots are left as recorded.
    /// </summary>
    public static async Task StageUserEmailChangeAsync(
        this IApplicationDbContext db, string oldEmail, string newEmail, CancellationToken ct = default)
    {
        var from = (oldEmail ?? "").Trim();
        var to = (newEmail ?? "").Trim();
        if (from.Length == 0 || to.Length == 0 || string.Equals(from, to, StringComparison.Ordinal)) return;

        bool Matches(string? value) => string.Equals(value?.Trim(), from, StringComparison.OrdinalIgnoreCase);

        foreach (var card in await db.Cards.Where(c => c.UserEmail == from || c.FranchiseeEmail == from).ToListAsync(ct))
        {
            if (Matches(card.UserEmail)) card.UserEmail = to;
            if (Matches(card.FranchiseeEmail)) card.FranchiseeEmail = to;
        }

        foreach (var invoice in await db.Invoices.Where(i => i.UserEmail == from).ToListAsync(ct))
            invoice.UserEmail = to;

        foreach (var order in await db.Orders.Where(o => o.UserEmail == from).ToListAsync(ct))
            order.UserEmail = to;

        foreach (var earning in await db.ReferralEarnings
                     .Where(r => r.ReferrerEmail == from || r.ReferredEmail == from).ToListAsync(ct))
        {
            if (Matches(earning.ReferrerEmail)) earning.ReferrerEmail = to;
            if (Matches(earning.ReferredEmail)) earning.ReferredEmail = to;
        }

        foreach (var referred in await db.Users.IgnoreQueryFilters()
                     .Where(u => u.Referral.ReferredBy == from).ToListAsync(ct))
            referred.Referral.ReferredBy = to;

        foreach (var pending in await db.RegistrationPendings.Where(p => p.ReferrerEmail == from).ToListAsync(ct))
            pending.ReferrerEmail = to;

        foreach (var mapping in await db.DealCustomerMappings.Where(m => m.CustomerEmail == from).ToListAsync(ct))
            mapping.CustomerEmail = to;

        foreach (var joining in await db.UserJoiningDeals.Where(d => d.UserEmail == from).ToListAsync(ct))
            joining.UserEmail = to;

        foreach (var payment in await db.FranchisePayments.Where(p => p.FranchiseEmail == from).ToListAsync(ct))
            payment.FranchiseEmail = to;

        foreach (var category in await db.UserCustomCategories.Where(c => c.UserEmail == from).ToListAsync(ct))
            category.UserEmail = to;

        foreach (var bank in await db.UserBankDetails.Where(b => b.UserEmail == from).ToListAsync(ct))
            bank.UserEmail = to;

        foreach (var verification in await db.FranchiseVerifications.Where(v => v.UserEmail == from).ToListAsync(ct))
            verification.UserEmail = to;

        foreach (var notification in await db.Notifications.Where(n => n.UserEmail == from).ToListAsync(ct))
            notification.UserEmail = to;
    }
}
