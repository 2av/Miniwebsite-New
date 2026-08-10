namespace MiniWebsite.Application.Portal.Dtos;

public record PortalMeDto(
    int UserId,
    string Email,
    string Name,
    string Role,
    string? ReferralCode,
    string CollaborationEnabled,
    string SaleskitEnabled,
    string Influencer,
    bool MwPaid,
    bool FranchiseAgreementPaid,
    bool FranchiseVerified,
    string? ProfileImageUrl = null,
    bool FranchiseInvoiceAvailable = false,
    int? FranchiseInvoiceId = null);

public record ProfileImageUploadDto(string ImagePath, string ProfileImageUrl);

public record BankDetailsDto(
    string? BankName,
    string? AccountHolder,
    string? AccountNumber,
    string? IfscCode,
    string? UpiId,
    string? UpiName);

public record ReferralEarningRowDto(
    int Id,
    string? ReferredEmail,
    string? Status,
    decimal Amount,
    string? ReferralDateDisplay,
    string? PaymentStatus);

/// <summary>One referred-user table row (matches user/referral/index.php columns).</summary>
public record ReferredUserRowDto(
    int? UserId,
    int? CardId,
    string MwFrIdLabel,
    bool MwFrIsCardLink,
    string? Email,
    string UserType,
    string? Name,
    string? Phone,
    string? JoinedOnDisplay,
    string ReferralSource,
    string? DateCreatedDisplay,
    string? ValidityDisplay,
    string MwStatus,
    string MwStatusClass,
    string UserPaymentLabel,
    string UserPaymentClass,
    decimal Amount,
    string ReferralPaymentLabel,
    string ReferralPaymentClass);

public record ReferralPageDto(
    string Role,
    bool IsTeamView,
    string PageTitle,
    string ReferralCode,
    string ReferralLink,
    decimal PendingAmount,
    decimal TotalEarning,
    int ReferralCount,
    int TotalSales,
    int TotalMwCreated,
    bool BankDetailsComplete,
    BankDetailsDto Bank,
    IReadOnlyList<ReferredUserRowDto> ReferredUsers,
    /// <summary>Legacy simple earnings list (kept for older clients).</summary>
    IReadOnlyList<ReferralEarningRowDto> Earnings);

public record SaveBankDetailsRequest(
    string Email,
    string BankName,
    string AccountHolder,
    string AccountNumber,
    string IfscCode,
    string UpiId,
    string UpiName);

public record WalletTxnDto(
    int Id,
    string? Deposit,
    string? Withdraw,
    string? OrderId,
    decimal Balance,
    string? DateDisplay,
    string? Message);

public record WalletPageDto(
    decimal Balance,
    bool HasSufficientBalance,
    IReadOnlyList<WalletTxnDto> Transactions);

public record InvoiceListItemDto(
    int Id,
    string? InvoiceNumber,
    string? InvoiceDateDisplay,
    decimal Amount,
    string? Status);

public record InvoiceDetailDto(
    int Id,
    string? InvoiceNumber,
    string? InvoiceDateDisplay,
    string? UserName,
    string? UserEmail,
    string? UserContact,
    string? CompanyName,
    decimal Amount,
    string? GstNumber,
    string? HtmlPreviewHint);

public record VerificationStatusDto(
    string Status,
    string? Message,
    bool CanSubmit,
    bool AgreementPaid,
    bool IsLocked = false,
    string? PanCardUrl = null,
    string? AadhaarFrontUrl = null,
    string? AadhaarBackUrl = null,
    string? AdminRemarks = null,
    string? ReviewedAtDisplay = null,
    string? ReviewedBy = null);

public record SubmitVerificationRequest(
    string Email,
    string? PanCardBase64 = null,
    string? AadhaarFrontBase64 = null,
    string? AadhaarBackBase64 = null);

public record IdCardProfileDto(
    string Name,
    string Email,
    string? Phone,
    string? Department,
    string? ProfileImageUrl,
    string Role,
    /// <summary>Padded ID shown on the card (3 digits franchisee / 6 digits team).</summary>
    string EmployeeId,
    /// <summary>e.g. "FR ID: " or "ID: ".</summary>
    string IdPrefix,
    /// <summary>e.g. "Franchise Partner" or "Sales Executive".</summary>
    string Designation,
    string Website = "www.miniwebsite.in");

public record CustomerManagerRowDto(
    int Id,
    string CustomerName,
    string PhoneNumber,
    string? EmailId,
    string? CompanyName,
    string? Website,
    string? AddressLine1,
    string? AreaCity,
    string? AddressDisplay,
    string? Comments,
    string LabelTag,
    string Source,
    string Status,
    string? BusinessType,
    string? ApproachedFor,
    string? FollowupMethod,
    string? CreatedDisplay,
    string? LastSharedDisplay,
    string? LastUpdatedDisplay);

public record CustomerFollowupRowDto(
    int Id,
    string? FollowupDatetimeDisplay,
    string FollowupMethod,
    string FollowupStatus,
    string? Comments);

public record CustomerTrackerPageDto(
    string Variant,
    bool ShowApproachedFor,
    string Role,
    int OwnerUserId,
    IReadOnlyList<string> BusinessTypeOptions,
    IReadOnlyList<CustomerManagerRowDto> Customers);

public record SaveCustomerRequest(
    string OwnerEmail,
    int? Id,
    string CustomerName,
    string PhoneNumber,
    string? LabelTag = null,
    string? EmailId = null,
    string? CompanyName = null,
    string? Website = null,
    string? AddressLine1 = null,
    string? AreaCity = null,
    string? Comments = null,
    string? Source = null,
    string? Status = null,
    string? BusinessType = null,
    string? ApproachedFor = null,
    string? FollowupMethod = null);

public record AddFollowupRequest(
    string OwnerEmail,
    int CustomerId,
    string FollowupDatetime,
    string FollowupMethod,
    string FollowupStatus,
    string? FollowupComments = null);
