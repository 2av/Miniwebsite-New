namespace MiniWebsite.Application.Admin.Files.Dtos;

public class AdminFilesQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public string? Folder { get; set; }
    public string? Status { get; set; }
}

public record AdminFileRowDto(
    int Id,
    int UserId,
    string UserName,
    string UserEmail,
    string Folder,
    string Document,
    string OriginalFileName,
    string StoredFileName,
    string ContentType,
    long SizeBytes,
    string Status,
    DateTime UploadedAt,
    DateTime? ReplacedAt,
    string? ViewUrl);

public record AdminFilesPageDto(
    IReadOnlyList<AdminFileRowDto> Files,
    int TotalCount,
    int Page,
    int PageSize,
    long TotalSizeBytes);
