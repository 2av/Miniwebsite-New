using Microsoft.EntityFrameworkCore;
using MiniWebsite.Application.Common.Interfaces;

namespace MiniWebsite.Application.Common;

/// <summary>Profile payment taken from the invoice. A card with no invoice is unpaid.</summary>
public readonly record struct CardSale(string Status, DateTime? PaidOn, decimal? Amount)
{
    public static CardSale Unpaid { get; } = new("Created", null, null);

    public bool Paid => CardSales.IsPaid(Status);
}

public static class CardSales
{
    public static bool IsPaid(string? status) =>
        string.Equals(status, "Success", StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, "Paid", StringComparison.OrdinalIgnoreCase);

    public static CardSale For(IReadOnlyDictionary<int, CardSale> sales, int cardId) =>
        sales.TryGetValue(cardId, out var sale) ? sale : CardSale.Unpaid;

    public static async Task<Dictionary<int, CardSale>> ByCardIdAsync(
        IApplicationDbContext db,
        IReadOnlyCollection<int> cardIds,
        CancellationToken ct)
    {
        var map = new Dictionary<int, CardSale>();
        if (cardIds.Count == 0) return map;

        var rows = await db.Invoices.AsNoTracking()
            .Where(i => i.CardId != null && cardIds.Contains(i.CardId.Value))
            .Select(i => new
            {
                CardId = i.CardId!.Value,
                i.Id,
                i.PaymentStatus,
                i.PaymentDate,
                i.TotalAmount,
            })
            .ToListAsync(ct);

        foreach (var group in rows.GroupBy(r => r.CardId))
        {
            var paid = group
                .Where(r => IsPaid(r.PaymentStatus))
                .OrderByDescending(r => r.PaymentDate ?? DateTime.MinValue)
                .ThenByDescending(r => r.Id)
                .FirstOrDefault();
            var chosen = paid ?? group.OrderByDescending(r => r.Id).First();
            map[group.Key] = new CardSale(chosen.PaymentStatus, chosen.PaymentDate, chosen.TotalAmount);
        }

        return map;
    }

    public static async Task<HashSet<int>> PaidUserIdsAsync(
        IApplicationDbContext db,
        IReadOnlyCollection<int> userIds,
        CancellationToken ct)
    {
        if (userIds.Count == 0) return [];

        var ids = await (
            from c in db.Cards.AsNoTracking()
            join i in db.Invoices.AsNoTracking() on c.Id equals i.CardId
            where c.UserId != null
                  && userIds.Contains(c.UserId.Value)
                  && (i.PaymentStatus == "Success" || i.PaymentStatus == "Paid")
            select c.UserId!.Value
        ).Distinct().ToListAsync(ct);

        return ids.ToHashSet();
    }
}
