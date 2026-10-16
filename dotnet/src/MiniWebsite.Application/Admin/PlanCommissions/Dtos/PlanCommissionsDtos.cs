namespace MiniWebsite.Application.Admin.PlanCommissions.Dtos;

public class PlanCommissionsQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public int? RoleId { get; set; }
    public int? PricingPlanId { get; set; }
    public string? Status { get; set; }
}

public record PlanCommissionRoleOptionDto(int RoleId, string RoleName);

public record PlanCommissionPlanOptionDto(
    int Id,
    string PlanName,
    string PlanCategory,
    string ValidityDisplay,
    decimal OfferedPrice,
    string Status);

public record PlanCommissionsMetaDto(
    IReadOnlyList<PlanCommissionRoleOptionDto> Roles,
    IReadOnlyList<PlanCommissionPlanOptionDto> Plans,
    IReadOnlyList<string> DiscountTypes,
    IReadOnlyList<string> EarningTypes,
    decimal GstRate);

public record PlanCommissionsPageDto(
    IReadOnlyList<PlanCommissionRowDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

/// <summary>Stored terms plus amounts calculated from the plan price (not stored).</summary>
public record PlanCommissionRowDto(
    int Id,
    int RoleId,
    string RoleName,
    int PricingPlanId,
    string PlanName,
    string PlanCategory,
    string ValidityDisplay,
    decimal OfferedPrice,
    string OfferedPriceDisplay,
    string DiscountType,
    decimal DiscountAmount,
    string DiscountDisplay,
    string EarningType,
    decimal EarningAmount,
    string EarningDisplay,
    decimal? EarningGst,
    string? EarningGstDisplay,
    decimal? SbRevenue,
    string? SbRevenueDisplay,
    bool DeductFromWallet,
    decimal? WalletDeduction,
    string? WalletDeductionDisplay,
    decimal? RenewalPrice,
    string? RenewalPriceDisplay,
    decimal? RenewalPriceGst,
    string? RenewalPriceGstDisplay,
    decimal? RenewalEarningPercent,
    decimal? RenewalEarning,
    string? RenewalEarningDisplay,
    string Status,
    string StatusTone,
    string? CreatedAtDisplay);

/// <param name="RoleIds">On create, one row is added per role. On update, exactly one role.</param>
/// <param name="DiscountAmount">Used only when <paramref name="DiscountType"/> is Fixed.</param>
/// <param name="EarningAmount">Used only when <paramref name="EarningType"/> is Fixed.</param>
/// <param name="RenewalPrice">Renewal price without GST for this role on this plan. Null when not renewed.</param>
/// <param name="RenewalEarningPercent">Null means no renewal earning. Requires <paramref name="RenewalPrice"/>.</param>
public record UpsertPlanCommissionRequest(
    IReadOnlyList<int> RoleIds,
    int PricingPlanId,
    string DiscountType,
    decimal DiscountAmount,
    string EarningType,
    decimal EarningAmount,
    decimal? RenewalPrice,
    decimal? RenewalEarningPercent,
    bool DeductFromWallet,
    string? Status = null);
