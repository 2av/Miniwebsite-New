using MiniWebsite.Domain.Common;

namespace MiniWebsite.Domain.Entities;

/// <summary>Deal recorded by admin for a Field Sales Team member. Monthly targets sum <see cref="Amount"/>.</summary>
public class FieldSalesDeal
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Details { get; set; } = "";
    public decimal Amount { get; set; }
    public DateTime DealDate { get; set; } = AppClock.Now;
    public DateTime CreatedAt { get; set; } = AppClock.Now;
}
