namespace MiniWebsite.Application.Admin.SalesChannelPartners.Dtos;

public class SalesChannelPartnersQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public string? Status { get; set; }
}

public record SalesChannelPartnerRoleDto(
    string Slug,
    int RoleId,
    string RoleName,
    string RoleDescription,
    string Prefix,
    string SalesType);

public record SalesChannelPartnersPageDto(
    SalesChannelPartnerRoleDto Role,
    IReadOnlyList<SalesChannelPartnerRowDto> Partners,
    int TotalCount,
    int Page,
    int PageSize);

public record SalesChannelPartnerRowDto(
    int Id,
    string UserCode,
    string FirstName,
    string LastName,
    string Name,
    string Email,
    string Phone,
    string District,
    string State,
    string Status,
    string StatusTone,
    string CreatedAtDisplay,
    string JoinedOnDisplay,
    string ReferralSource,
    string DateCreatedDisplay,
    string ExpiryDateDisplay,
    string UserStatusLabel,
    string UserPaymentStatus,
    decimal JoiningFees,
    string JoiningFeesDisplay,
    decimal PromoDiscount,
    string PromoDiscountDisplay,
    string CouponCode,
    int? InvoiceId,
    int DbCount,
    string DocumentStatus,
    string DocumentStatusTone,
    string? PanDocumentUrl,
    string? FrontDocumentUrl,
    string? BackDocumentUrl,
    int TotalReferred,
    decimal TotalReferralEarning,
    string TotalReferralEarningDisplay,
    string CommissionStatus,
    string RefundStatus);

public record CreateSalesChannelPartnerRequest(
    string FirstName,
    string? LastName,
    string Email,
    string? Phone,
    string? District,
    string? State,
    string Password);

public record UpdateSalesChannelPartnerRequest(
    string FirstName,
    string? LastName,
    string Email,
    string? Phone,
    string? District,
    string? State);

public record ToggleSalesChannelPartnerStatusRequest(string? NewStatus);

public record ResetSalesChannelPartnerPasswordRequest(string NewPassword);
