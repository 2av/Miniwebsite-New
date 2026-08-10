namespace MiniWebsite.Application.Admin.ManageFaqs.Dtos;

public class ManageFaqsQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public string? PageType { get; set; }
    public string? Status { get; set; }
}

public record ManageFaqsPageDto(
    IReadOnlyList<ManageFaqRowDto> Faqs,
    int TotalCount,
    int Page,
    int PageSize);

public record ManageFaqRowDto(
    int Id,
    string PageType,
    string PageTypeDisplay,
    string Question,
    string Answer,
    string AnswerPreview,
    int SortOrder,
    string Status,
    string StatusTone);

public record UpsertFaqRequest(
    string PageType,
    string Question,
    string Answer,
    int SortOrder,
    string? Status);

public record ManageFaqsMetaDto(IReadOnlyList<FaqPageTypeOptionDto> PageTypes);

public record FaqPageTypeOptionDto(string Value, string Label);
