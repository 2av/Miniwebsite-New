namespace MiniWebsite.Application.Admin.GrowWithMw.Dtos;

public class DocPagesQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public int? SectionId { get; set; }
    public string? Status { get; set; }
}

public record DocPagesPageDto(
    IReadOnlyList<DocPageListItemDto> Pages,
    int TotalCount,
    int Page,
    int PageSize);

public record DocPageListItemDto(
    int Id,
    string Title,
    string Slug,
    string Status,
    string StatusTone,
    int SortOrder,
    string UpdatedAtDisplay,
    int SectionId,
    string SectionTitle);

public record DocPageDetailDto(
    int Id,
    int SectionId,
    string SectionTitle,
    string Title,
    string Slug,
    string Status,
    string ContentHtml,
    string MetaTitle,
    string MetaDescription,
    string MetaKeywords,
    int SortOrder,
    string? PublishedAtDisplay,
    string UpdatedAtDisplay,
    string PublicUrl,
    IReadOnlyList<DocPageOrderItemDto> SectionPages);

public record DocPageOrderItemDto(int Id, string Title, string Slug, string Status, int SortOrder);

public record UpsertDocPageRequest(
    int SectionId,
    string Title,
    string? Slug,
    string? ContentHtml,
    string? MetaTitle,
    string? MetaDescription,
    string? MetaKeywords,
    string Action);

public record DocSectionDto(
    int Id,
    string Title,
    string Slug,
    string Description,
    int SortOrder,
    bool CollapsedDefault,
    int PageCount);

public record UpsertDocSectionRequest(
    string Title,
    string? Slug,
    string? Description,
    bool CollapsedDefault);

public record ReorderRequest(IReadOnlyList<int> Order);

public record ReorderPagesRequest(int SectionId, IReadOnlyList<int> Order);

public record DocMediaItemDto(
    int Id,
    string Filename,
    string RelPath,
    string Url,
    string Mime,
    int SizeBytes,
    string? UploadedBy,
    string CreatedAtDisplay,
    bool IsImage);

public record DocMediaListDto(IReadOnlyList<DocMediaItemDto> Items);

public record DocUploadResultDto(string Location, DocMediaItemDto Media);

public record GrowWithMwMetaDto(
    IReadOnlyList<DocSectionOptionDto> Sections,
    string PublicDocsPrefix,
    string GrowWithMwPrefix);

public record DocSectionOptionDto(int Id, string Title);
