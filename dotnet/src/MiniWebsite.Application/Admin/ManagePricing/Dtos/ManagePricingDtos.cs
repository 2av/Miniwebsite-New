namespace MiniWebsite.Application.Admin.ManagePricing.Dtos;

public class ManagePricingQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public string? PlanType { get; set; }
    public string? Status { get; set; }
}

public record PricingDealOptionDto(int Id, string DealName, string? CouponCode);

public record PricingPlanTypeDto(string Key, IReadOnlyList<string> PlanNames);

public record PricingPlanKeyDto(int Id, string PlanType, string PlanName);

public record ManagePricingMetaDto(
    IReadOnlyList<PricingPlanTypeDto> PlanCatalog,
    IReadOnlyList<PricingDealOptionDto> Deals,
    IReadOnlyList<PricingPlanKeyDto> ExistingPlans);

public record ManagePricingPageDto(
    IReadOnlyList<ManagePricingRowDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record ManagePricingRowDto(
    int Id,
    string PlanType,
    string PlanName,
    decimal OriginalPrice,
    string OriginalPriceDisplay,
    decimal OriginalPriceGst,
    string OriginalPriceGstDisplay,
    decimal OfferedPrice,
    string OfferedPriceDisplay,
    decimal OfferedPriceGst,
    string OfferedPriceGstDisplay,
    int? DealId,
    string? DealName,
    string? CouponCode,
    string DealDisplay,
    string Status,
    string StatusTone,
    string? CreatedAtDisplay);

public record UpsertPricingRequest(
    string PlanType,
    string PlanName,
    decimal OriginalPrice,
    decimal OfferedPrice,
    int? DealId = null,
    string? Status = null);
