namespace MiniWebsite.Application.Admin.ManageUsers.Dtos;

public class ManageUsersQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    /// <summary>YES = ACTIVE, NO = INACTIVE</summary>
    public string? StatusFilter { get; set; }
    /// <summary>mapped | unmapped</summary>
    public string? DealFilter { get; set; }
    /// <summary>0 | 1-5 | 6-10 | 10+</summary>
    public string? WebsiteFilter { get; set; }
    /// <summary>today | week | month | year</summary>
    public string? DateFilter { get; set; }
}

public record ManageUsersPageDto(
    ManageUsersStatsDto Stats,
    int MwDealCount,
    int FranchiseDealCount,
    IReadOnlyList<DealOptionDto> MwDeals,
    IReadOnlyList<DealOptionDto> FranchiseDeals,
    IReadOnlyList<ManageUserRowDto> Users,
    int TotalCount,
    int Page,
    int PageSize);

public record ManageUsersStatsDto(
    int TotalUsers,
    int ActiveUsers,
    int UsersWithDeals,
    int TotalWebsites);

public record DealOptionDto(int Id, string DealName, string? CouponCode, string PlanType);

public record ManageUserRowDto(
    int Id,
    string UserCode,
    string Email,
    string Name,
    string? Phone,
    string? State,
    string? District,
    string Status,
    DateTime CreatedAt,
    string CollaborationEnabled,
    string SaleskitEnabled,
    string? RefundStatus,
    DateTime? RefundStatusDate,
    string? ReferredBy,
    string ReferralSourceDisplay,
    int WebsiteCount,
    decimal PendingReferralAmount,
    decimal TotalReferralAmount,
    decimal TotalPaidAmount,
    DateTime? LastPaymentDate,
    string MwPaymentStatusLabel,
    string CommissionStatus,
    MappedDealDto? MwDeal,
    MappedDealDto? FranchiseDeal,
    string DocumentStatus,
    string DocumentStatusTone,
    int TotalReferred,
    string PayoutStatus,
    int? InvoiceId);

public record MappedDealDto(int MappingId, int DealId, string DealName, string? CouponCode);

public record MapDealRequest(string UserEmail, int DealId, string? CreatedBy);

public record ToggleStatusRequest(string Email, string Status);

public record SetUserStatusRequest(string? Status);

public record SetRefundRequest(string Email, string RefundStatus);

public record AdminResetPasswordRequest(string Email, string Role, string NewPassword);

public record DashboardDetailsDto(string UserEmail, IReadOnlyList<DashboardWebsiteDto> Websites);

public record DashboardWebsiteDto(
    int Id,
    string? CompanyName,
    string? CardId,
    string? CardStatus,
    string? PaymentStatus,
    DateTime? PaymentDate,
    DateTime? UploadedDate,
    DateTime? ValidityDate,
    string? ComplimentaryEnabled,
    string? FUserEmail,
    string StatusClass,
    string StatusText,
    string ValidityDisplay,
    string PaymentLabel,
    string OrderIdDisplay);

public record ReferralDetailsDto(
    string ReferrerEmail,
    string UserName,
    string CollaborationEnabled,
    string SaleskitEnabled,
    decimal TotalReferralAmount,
    decimal TotalPaidAmount,
    decimal PendingAmount,
    int RegularReferrals,
    int CollaborationReferrals,
    BankDetailsDto Bank,
    IReadOnlyList<ReferredUserDto> ReferredUsers);

public record BankDetailsDto(
    string? AccountHolderName,
    string? AccountNumber,
    string? IfscCode,
    string? BankName,
    string? UpiId,
    string? UpiName);

public record UpsertBankDetailsRequest(
    string UserEmail,
    string? AccountHolderName,
    string? AccountNumber,
    string? IfscCode,
    string? BankName,
    string? UpiId,
    string? UpiName);

public record ReferredUserDto(
    int ReferralId,
    string ReferredEmail,
    DateTime ReferralDate,
    decimal Amount,
    string? IsCollaboration,
    int? CustomerId,
    int? FranchiseeId,
    string? UserName,
    string? UserContact,
    int? CardId,
    DateTime? CardUploadedDate,
    DateTime? CardValidityDate,
    string? ComplimentaryEnabled,
    string? PaymentStatus,
    DateTime? PaymentDate,
    string? FUserEmail,
    string MwStatusText,
    string ValidityDisplay);
