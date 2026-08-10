namespace MiniWebsite.Application.Admin.Franchisees.Dtos;

public class FranchiseeQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
}

public record FranchiseePageDto(
    IReadOnlyList<FranchiseeRowDto> Franchisees,
    int TotalCount,
    int Page,
    int PageSize);

public record FranchiseeRowDto(
    int Id,
    string Email,
    string Name,
    string? Phone,
    DateTime? CreatedAt,
    string ReferralSourceDisplay,
    string CompanyName,
    string Status,
    bool IsActive,
    int? FirstCardId,
    string? FirstCardUserEmail,
    string PublicUrl,
    string EditUrl,
    string PaymentStatusLabel,
    string PaymentStatusTone,
    string? PaidOnDisplay,
    decimal FranchiseFee,
    string FranchiseFeeDisplay,
    int? FranchiseInvoiceId,
    int WebsiteCount,
    string DocumentStatus,
    string DocumentStatusTone,
    decimal WalletBalance,
    string WalletBalanceDisplay);

public record CreateFranchiseeRequest(
    string Name,
    string Email,
    string Phone,
    string Password);

public record ActivateFranchiseeRequest(int Id);

public record FranchiseeDashboardDto(
    string Email,
    IReadOnlyList<FranchiseeWebsiteDto> Websites);

public record FranchiseeWebsiteDto(
    int Id,
    string? CompanyName,
    DateTime? UploadedDate,
    DateTime? ValidityDate,
    string StatusText,
    string PaymentLabel,
    string PublicUrl);
