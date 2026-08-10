namespace MiniWebsite.Application.Admin.EmailTemplates.Dtos;

public class EmailTemplateQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public string? Category { get; set; }
    public string? Status { get; set; }
}

public record EmailTemplateCategoryOption(string Value, string Label);

public record EmailTemplateMetaDto(IReadOnlyList<EmailTemplateCategoryOption> Categories);

public record EmailTemplateRowDto(
    int Id,
    string TemplateKey,
    string Name,
    string Category,
    string CategoryDisplay,
    string Subject,
    string BodyHtml,
    string BodyPreview,
    string? AvailableVariables,
    string? Description,
    string Status,
    string StatusTone,
    bool IsSystem,
    DateTime? UpdatedAt,
    string? UpdatedBy);

public record EmailTemplatePageDto(
    IReadOnlyList<EmailTemplateRowDto> Templates,
    int TotalCount,
    int Page,
    int PageSize);

public record UpsertEmailTemplateRequest(
    string TemplateKey,
    string Name,
    string Category,
    string Subject,
    string BodyHtml,
    string? AvailableVariables,
    string? Description,
    string? Status,
    string? UpdatedBy);

public record PreviewEmailTemplateRequest(
    string Subject,
    string BodyHtml,
    Dictionary<string, string>? SampleVariables);

public record PreviewEmailTemplateDto(string Subject, string BodyHtml);
