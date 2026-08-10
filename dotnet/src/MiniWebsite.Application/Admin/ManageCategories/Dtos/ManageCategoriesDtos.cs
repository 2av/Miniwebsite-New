namespace MiniWebsite.Application.Admin.ManageCategories.Dtos;

public class ManageCategoriesQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public string? Active { get; set; }
}

public record ManageCategoriesPageDto(
    IReadOnlyList<ManageCategoryRowDto> Categories,
    int TotalCount,
    int Page,
    int PageSize);

public record ManageCategoryRowDto(
    int Id,
    string BusinessProfileType,
    string BusinessHeading,
    string BusinessCategory,
    string BusinessCategorySlug,
    string ProductCategory,
    string ProductCategorySlug,
    int DirectoryPriority,
    bool IsActive,
    string ActiveLabel,
    string ActiveTone,
    string Keywords,
    string Tags);

public record UpsertCategoryRequest(
    string BusinessProfileType,
    string BusinessHeading,
    string BusinessCategory,
    string? BusinessCategorySlug,
    string ProductCategory,
    string? ProductCategorySlug,
    int DirectoryPriority,
    bool IsActive,
    string? Keywords,
    string? Tags,
    string? CreatedBy);

public record CategoryImportResultDto(
    int Imported,
    int Skipped,
    IReadOnlyList<string> Errors);

public record ManageCategoriesMetaDto(
    IReadOnlyList<string> CsvHeaders);
