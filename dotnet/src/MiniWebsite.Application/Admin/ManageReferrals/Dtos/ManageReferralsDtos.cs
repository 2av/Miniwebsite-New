namespace MiniWebsite.Application.Admin.ManageReferrals.Dtos;

public class ManageReferralsQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
}

public record ManageReferralsPageDto(
    IReadOnlyList<ManageReferralRowDto> Referrals,
    int TotalCount,
    int Page,
    int PageSize);

public record ManageReferralRowDto(
    int ReferralId,
    string CommissionId,
    string UserIdDisplay,
    string ReferrerEmail,
    string ReferredCustomer,
    string ReferredEmail,
    string OrderDescription,
    string ListPriceDisplay,
    string PromoDiscountDisplay,
    string NetPriceDisplay,
    string GstDisplay,
    string TotalAmountDisplay,
    decimal ReferralAmount,
    string ReferralAmountDisplay,
    string RenewalAmountDisplay,
    string CommissionStatus,
    string SbPaymentStatus,
    string SbPaymentStatusTone,
    decimal PaidAmount,
    decimal PendingAmount,
    bool CanAddPayment,
    bool HasPaymentHistory,
    string Comment);

public record UpdateReferralCommentRequest(string? Comment);

public record ReferrerPaymentDetailsDto(
    string ReferrerEmail,
    string ReferrerName,
    IReadOnlyList<ReferrerPaymentLineDto> Lines);

public record ReferrerPaymentLineDto(
    int ReferralId,
    string ReferredEmail,
    string ReferredName,
    DateTime ReferralDate,
    string UserPaymentStatusLabel,
    string UserPaymentStatusTone,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal PendingAmount,
    string StatusLabel,
    string StatusTone,
    bool CanAddPayment,
    bool HasHistory);

public record ReferralPaymentHistoryDto(
    int ReferralId,
    string ReferrerName,
    string ReferredName,
    decimal ReferralAmount,
    IReadOnlyList<ReferralPaymentHistoryItemDto> Items);

public record ReferralPaymentHistoryItemDto(
    DateTime PaymentDate,
    decimal Amount,
    string TransactionNumber,
    string PaymentMethod,
    string? PaymentNotes,
    string ProcessedBy);

public record ProcessReferralPaymentRequest(
    int ReferralId,
    decimal Amount,
    string TransactionNumber,
    string PaymentMethod,
    string? PaymentNotes,
    string? ProcessedBy);

public record ManageReferralBankDetailsDto(
    string UserEmail,
    string? AccountHolderName,
    string? AccountNumber,
    string? IfscCode,
    string? BankName,
    string? UpiId,
    string? UpiName);
