namespace MiniWebsite.Application.Admin.ManageCards.Dtos;

public class ManageCardsQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    /// <summary>ID / company / user email / franchisee email</summary>
    public string? Search { get; set; }
    /// <summary>all | paid | unpaid | trial — maps PHP Payment Done / Not Done / Trail Cards</summary>
    public string? PaymentFilter { get; set; }
}

public record ManageCardsPageDto(
    IReadOnlyList<ManageCardRowDto> Cards,
    int TotalCount,
    int Page,
    int PageSize);

public record ManageCardRowDto(
    int Id,
    string? CardId,
    string? UserEmail,
    string? FUserEmail,
    int? UserId,
    string? UserName,
    string? UserPhone,
    string ReferralSourceDisplay,
    string CompanyName,
    DateTime? UploadedDate,
    DateTime? ValidityDate,
    DateTime? PaymentDate,
    string? PaymentStatus,
    string? ComplimentaryEnabled,
    string StatusText,
    string StatusTone,
    bool IsTrial,
    string ValidityDisplay,
    string ValidityTone,
    string PaymentLabel,
    string? OrderAmount,
    bool HasInvoice,
    bool CanToggleComplimentary,
    string PublicUrl,
    string EditUrl);
