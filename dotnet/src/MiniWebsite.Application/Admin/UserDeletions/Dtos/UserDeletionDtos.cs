namespace MiniWebsite.Application.Admin.UserDeletions.Dtos;

public class UserDeletionQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public string? Role { get; set; }
    /// <summary>all | active | deleted</summary>
    public string? Status { get; set; }
}

public record UserDeletionPageDto(
    IReadOnlyList<UserDeletionRowDto> Users,
    int TotalCount,
    int ActiveCount,
    int DeletedCount,
    int Page,
    int PageSize);

public record UserDeletionRowDto(
    int Id,
    string Email,
    string Name,
    string Role,
    string Status,
    bool IsDeleted,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record BulkUserIdsRequest(IReadOnlyList<int> UserIds);
