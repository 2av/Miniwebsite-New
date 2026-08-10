namespace MiniWebsite.Application.PublicSite.Dtos;

public record PublicFaqItemDto(int Id, string Question, string Answer);

public record PublicContentDto(
    string ContentType,
    string Title,
    string ContentHtml,
    string? MetaDescription,
    string? LastUpdatedDisplay);

public record CaptchaChallengeDto(string CaptchaId, string Challenge);

public record FranchiseEnquiryRequest(
    string Name,
    string Email,
    string Contact,
    string? Address,
    string? Message,
    string CaptchaId,
    string CaptchaInput);

/// <summary>
/// Public digi-card viewer payload (n.php equivalent).
/// Status: ok | not_found | expired | redirect
/// </summary>
public record PublicCardViewerDto(
    string Status,
    string? Message,
    string? CompanyLabel,
    string? RedirectSlug,
    string? RedirectUrl,
    string? ExpiryDateDisplay,
    PublicCardPayloadDto? Card);

public record PublicCardPayloadDto(
    int Id,
    string Slug,
    int ThemeNumber,
    string ThemeCssPath,
    string LayoutCssPath,
    string HeroName,
    string? PrimaryCategoryTitle,
    string HeroCoverUrl,
    string LogoUrl,
    string DefaultImageUrl,
    string? About,
    string? Phone,
    string? Whatsapp,
    string? Email,
    string? Location,
    string? GoogleDirection,
    string? Website,
    string ShareUrl,
    string? GstNumber,
    string? FullAddress,
    IReadOnlyList<PublicBusinessHourDto> BusinessHours,
    IReadOnlyList<PublicSocialLinkDto> SocialLinks,
    IReadOnlyList<PublicServiceDto> Services,
    IReadOnlyList<PublicOfferDto> Offers,
    IReadOnlyList<PublicProductDto> Products,
    IReadOnlyList<string> GalleryUrls,
    IReadOnlyList<string> VideoUrls,
    IReadOnlyList<PublicPaymentQrDto> PaymentQrs);

public record PublicBusinessHourDto(string Days, string Hours);
public record PublicSocialLinkDto(string Icon, string Url);
public record PublicServiceDto(string Name, string? Description, string ImageUrl);
public record PublicOfferDto(
    string Title,
    string? Description,
    string ImageUrl,
    string? Badge,
    int DiscountPercentage,
    string? Status);
public record PublicProductDto(
    string Name,
    string? Description,
    string ImageUrl,
    decimal Mrp,
    decimal SellingPrice,
    string? PriceType,
    string? PricingUnit,
    string? CtaText,
    string? CategoryLabel);
public record PublicPaymentQrDto(string Label, string? ImageDataUrl, string? Upi);
