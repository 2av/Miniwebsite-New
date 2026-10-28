namespace MiniWebsite.Application.DigiCards.Dtos;

/// <summary>Row used on customer/team dashboard Mini Website list.</summary>
public record DigiCardListItemDto(
    int Id,
    string? CardSlug,
    string? CompanyName,
    string? UserEmail,
    string? FranchiseeEmail,
    string? PaymentStatus,
    string? CardStatus,
    DateTime? UploadedDate,
    DateTime? PaymentDate,
    DateTime? ValidityDate,
    string ValidityDisplay,
    string PublicUrl,
    string DbIdDisplay = "");

public record DigiCardDetailDto(
    int Id,
    string? CardSlug,
    string? CompanyName,
    string? DisplayName,
    string? UserEmail,
    string? FranchiseeEmail,
    string? FirstName,
    string? LastName,
    string? Position,
    string? Contact,
    string? Contact2,
    string? Whatsapp,
    string? Email,
    string? Website,
    string? Address,
    string? Address2,
    string? City,
    string? State,
    string? Pincode,
    string? Country,
    string? AboutUs,
    string? NatureOfBusiness,
    string? Speciality,
    string? PaymentStatus,
    string? CardStatus,
    string? LogoLocation,
    string? HeroImageLocation,
    DateTime? UploadedDate,
    DateTime? PaymentDate,
    DateTime? ValidityDate,
    string? GstNumber,
    string? BusinessHours,
    // PreviousSlug from CardSlugHistory; business profile fields live on Card
    string? PreviousSlug,
    string BusinessProfileType,
    string BusinessType,
    string BusinessOperationArea,
    string? BusinessOperationLocations);

public record DigiCardSlugLookupDto(
    int CardId,
    string PreviousSlug,
    string? CurrentCardSlug);

public record FranchiseeManagedUserDto(
    int UserId,
    string? Name,
    string? Phone,
    string? Email,
    DateTime? UserCreatedAt,
    string? ReferralCode,
    string? ReferredBy,
    int? CardDbId,
    string? CompanyName,
    string? PaymentStatus,
    DateTime? CardCreatedDate,
    DateTime? PaymentDate,
    DateTime? ValidityDate,
    string ValidityDisplay,
    string? CardSlug = null,
    string DbIdDisplay = "");

public record CustomerDashboardDto(
    string Email,
    string Role,
    string ReferralCode,
    string? RefundStatus,
    DateTime? RefundStatusDate,
    bool ShowInvoiceColumn,
    int? MwReferralId,
    string Influencer,
    IReadOnlyList<DigiCardListItemDto> Cards,
    string? UserName = null,
    string? Phone = null);

public record FranchiseeDashboardDto(
    string Email,
    string Role,
    int TotalCardsCreated,
    IReadOnlyList<FranchiseeManagedUserDto> ManagedUsers);
