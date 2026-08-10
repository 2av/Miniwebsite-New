namespace MiniWebsite.Application.Website.Dtos;

public record BusinessNameDto(int Id, string? CompName, string? DisplayName, string? Slug, int NameChangeCount, string? PreviousSlug = null);
public record UpsertBusinessNameRequest(string CompName, string? DisplayName, string OwnerEmail, string? FranchiseeEmail = null, bool Complimentary = false);
public record CheckNameResponse(bool Available, string Slug, string? Message);

public record ThemeDto(string? DCss);
public record UpdateThemeRequest(string DCss);
public record ThemeCatalogItemDto(int Number, string Name, string CssValue, string? PreviewImageUrl);

public record CompanyDetailsDto(
    string? FirstName, string? LastName, string? PositionPrimary, string? PositionSecondary, string? Position,
    string? Contact, string? Contact2, string? Whatsapp, string? GstNumber,
    string? Address, string? Address2, string? City, string? State, string? Pincode, string? Country,
    string? Email, string? Website, string? Location, string? CompEstDate, string? AboutUs, string? BusinessHours,
    string? LogoLocation, string? HeroImageLocation,
    string? LogoUrl, string? HeroImageUrl,
    string BusinessProfileType, string BusinessType, string BusinessOperationArea, string? BusinessOperationLocations);

/// <summary>
/// Image fields are filenames returned by the (future) PHP upload endpoint — not binary.
/// Example: logoLocation = "42_logo_x.jpg"
/// </summary>
public record UpdateCompanyDetailsRequest(
    string? FirstName, string? LastName, string? PositionPrimary, string? PositionSecondary, string? Position,
    string? Contact, string? Contact2, string? Whatsapp, string? GstNumber,
    string? Address, string? Address2, string? City, string? State, string? Pincode, string? Country,
    string? Email, string? Website, string? Location, string? CompEstDate, string? AboutUs, string? BusinessHours,
    string? LogoLocation, string? HeroImageLocation,
    string? BusinessProfileType, string? BusinessType, string? BusinessOperationArea, string? BusinessOperationLocations);

public record SocialLinksDto(string? Fb, string? Twitter, string? Instagram, string? Linkedin, string? Youtube, string? Pinterest);
public record UpdateSocialLinksRequest(string? Fb, string? Twitter, string? Instagram, string? Linkedin, string? Youtube, string? Pinterest);

public record PaymentDetailsDto(
    string? Paytm, string? GooglePay, string? PhonePay,
    string? AccountNo, string? Ifsc, string? AcName, string? BankName, string? AcType);
public record UpdatePaymentDetailsRequest(
    string? Paytm, string? GooglePay, string? PhonePay,
    string? AccountNo, string? Ifsc, string? AcName, string? BankName, string? AcType);

public record VideosDto(IReadOnlyList<string?> Urls);
public record UpdateVideosRequest(IReadOnlyList<string?> Urls);

public record ProductDto(
    int Id, string CardId, int UserId, string? ProductName, int? ProductCategory, string? CategorySource,
    string? ProductDescription, string? ProductImage, string? ProductImageUrl,
    decimal Mrp, decimal SellingPrice,
    string? PriceType, string? PricingUnit, string? CtaText, int DisplayOrder);

/// <summary>productImage = filename from PHP upload (e.g. product-pricing folder).</summary>
public record UpsertProductRequest(
    string ProductName, int? ProductCategory, string? CategorySource, string? ProductDescription,
    string? ProductImage, decimal Mrp, decimal SellingPrice, string? PriceType, string? PricingUnit,
    string? CtaText, int DisplayOrder = 0);

public record ServiceItemDto(
    int Id, string CardId, int UserId, string? ProductName, int? ProductCategory, string? CategorySource,
    string? ProductDescription, string? ProductImage, string? ProductImageUrl, int DisplayOrder);

public record UpsertServiceRequest(
    string ProductName, int? ProductCategory, string? CategorySource, string? ProductDescription,
    string? ProductImage, int DisplayOrder = 0);

public record GalleryItemDto(int Id, string CardId, int UserId, string? GalleryImage, string? GalleryImageUrl, int DisplayOrder);

/// <summary>galleryImage = filename from PHP upload.</summary>
public record UpsertGalleryRequest(string? GalleryImage, int DisplayOrder = 0);

public record SpecialOfferDto(
    int Id, string CardId, int UserId, string OfferTitle, string? OfferDescription, string? OfferImage, string? OfferImageUrl,
    string? Badge, int DiscountPercentage, DateOnly? StartDate, TimeOnly? StartTime,
    DateOnly? EndDate, TimeOnly? EndTime, string? Status, int DisplayOrder);

public record UpsertSpecialOfferRequest(
    string OfferTitle, string? OfferDescription, string? OfferImage, string? Badge,
    int DiscountPercentage, DateOnly? StartDate, TimeOnly? StartTime,
    DateOnly? EndDate, TimeOnly? EndTime, string? Status, int DisplayOrder = 0);
