namespace MiniWebsite.Application.Admin.ManagePricing.Dtos;

public class ManagePricingQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    /// <summary>Positive = that role, 0 = only plans for All (no role mapped), null = no filter.</summary>
    public int? RoleId { get; set; }
    public string? Status { get; set; }
}

public record PricingRoleOptionDto(int RoleId, string RoleName);

public record PricingPlanKeyDto(int Id, int? RoleId, string PlanName);

public record ManagePricingMetaDto(
    IReadOnlyList<PricingRoleOptionDto> Roles,
    IReadOnlyList<string> PlanNames,
    IReadOnlyList<PricingPlanKeyDto> ExistingPlans);

public record ManagePricingPageDto(
    IReadOnlyList<ManagePricingRowDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record ManagePricingRowDto(
    int Id,
    int? RoleId,
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
    string Status,
    string StatusTone,
    string? CreatedAtDisplay);

/// <param name="RoleId">Null means All (no role mapped).</param>
public record UpsertPricingRequest(
    int? RoleId,
    string PlanName,
    decimal OriginalPrice,
    decimal OfferedPrice,
    string? Status = null);
