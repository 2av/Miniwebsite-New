namespace MiniWebsite.Application.Admin.Coupons.Dtos;

public class CouponsQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public int? RoleId { get; set; }
    public string? Status { get; set; }
}

public record CouponsPageDto(
    IReadOnlyList<CouponRowDto> Coupons,
    int TotalCount,
    int Page,
    int PageSize);

public record CouponRowDto(
    int Id,
    int? RoleId,
    string PlanType,
    string CouponName,
    string CouponCode,
    string CouponType,
    DateTime? CreatedAt,
    string CreatedAtDisplay,
    DateTime? ValidityDate,
    string ValidityDateDisplay,
    string PromoDiscountType,
    decimal PromoDiscountValue,
    string PromoDiscountDisplay,
    int UsageLimitPerCoupon,
    string UsageLimitPerCouponDisplay,
    int UsageLimitPerUser,
    string UsageLimitPerUserDisplay,
    int CurrentUsage,
    decimal ReferrerCommission,
    decimal RenewalCommission,
    string Status,
    string StatusDisplay,
    string StatusTone);

public record UpsertCouponRequest(
    int RoleId,
    string CouponName,
    string CouponCode,
    string CouponType,
    DateTime ValidityDate,
    string PromoDiscountType,
    decimal PromoDiscountValue,
    int UsageLimitPerCoupon,
    int UsageLimitPerUser,
    string? Status = null,
    decimal ReferrerCommission = 0,
    decimal RenewalCommission = 0,
    string? CreatedBy = null);

public record CouponRoleOptionDto(int RoleId, string RoleName);

public record CouponsMetaDto(
    IReadOnlyList<CouponRoleOptionDto> Roles,
    IReadOnlyList<string> CouponTypes,
    IReadOnlyList<string> Statuses);

public record CouponCodeDto(string Code);
