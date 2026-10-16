namespace MiniWebsite.Application.Admin.ManagePricing.Dtos;

public class ManagePricingQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public string? Category { get; set; }
    public string? Status { get; set; }
}

public record PricingPlanKeyDto(int Id, string PlanName);

public record ManagePricingMetaDto(
    IReadOnlyList<string> Categories,
    IReadOnlyList<PricingPlanKeyDto> ExistingPlans);

public record ManagePricingPageDto(
    IReadOnlyList<ManagePricingRowDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record ManagePricingRowDto(
    int Id,
    string PlanName,
    string PlanCategory,
    int? ValidityMonths,
    string ValidityDisplay,
    decimal OriginalPrice,
    string OriginalPriceDisplay,
    decimal OriginalPriceGst,
    string OriginalPriceGstDisplay,
    decimal OfferedPrice,
    string OfferedPriceDisplay,
    decimal OfferedPriceGst,
    string OfferedPriceGstDisplay,
    string Status,
    string StatusTone,
    string? CreatedAtDisplay);

/// <param name="ValidityMonths">Null means lifetime validity.</param>
public record UpsertPricingRequest(
    string PlanName,
    string PlanCategory,
    int? ValidityMonths,
    decimal OriginalPrice,
    decimal OfferedPrice,
    string? Status = null);
