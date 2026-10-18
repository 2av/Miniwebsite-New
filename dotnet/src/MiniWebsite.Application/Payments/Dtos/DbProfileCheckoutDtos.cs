namespace MiniWebsite.Application.Payments.Dtos;

public record DbProfileBillingDto(
    string Name,
    string Email,
    string Phone,
    string Address,
    string State,
    string City,
    string Pincode,
    string GstNumber);

public record DbProfilePlanDto(
    int Id,
    string PlanName,
    int? ValidityMonths,
    string ValidityLabel,
    decimal OriginalPrice,
    decimal OfferedPrice,
    decimal OriginalPriceGst,
    decimal OfferedPriceGst,
    decimal SaveAmount,
    decimal? PerMonthApprox,
    string? Badge,
    string? LockLabel);

public record DbProfileCheckoutPageDto(
    int CardId,
    string? CompanyName,
    bool AlreadyPaid,
    string SellerState,
    DbProfileBillingDto Billing,
    IReadOnlyList<DbProfilePlanDto> Plans);

public record DbProfileQuoteDto(
    decimal OriginalPrice,
    decimal Discount,
    decimal PromoDiscount,
    decimal SubTotal,
    decimal CgstAmount,
    decimal SgstAmount,
    decimal IgstAmount,
    decimal FinalTotal,
    bool IntraState);

public record DbProfilePromoRequest(
    string Email,
    int CardId,
    int PlanId,
    string? PromoCode,
    string? State);

public record DbProfilePromoResultDto(string Message, DbProfileQuoteDto Quote);

public record DbProfileCheckoutRequest(
    string Email,
    int CardId,
    int PlanId,
    string? PromoCode,
    string? GstNumber,
    string Name,
    string BillingEmail,
    string Phone,
    string Address,
    string State,
    string City,
    string Pincode,
    bool Agreed);

public record DbProfileCheckoutResultDto(
    int InvoiceId,
    string OrderId,
    decimal Amount,
    string Currency,
    string KeyId,
    string PlanName);

public record DbProfileVerifyRequest(
    string Email,
    int CardId,
    int InvoiceId,
    string RazorpayOrderId,
    string RazorpayPaymentId,
    string RazorpaySignature);
