namespace MiniWebsite.Domain.Common;

public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = AppClock.Now;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
