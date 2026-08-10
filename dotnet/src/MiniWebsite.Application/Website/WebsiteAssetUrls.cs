using MiniWebsite.Application.Common.Options;

namespace MiniWebsite.Application.Website;

/// <summary>
/// Builds public image URLs on the PHP domain.
/// Upload flow (PHP later): Frontend POSTs file to PHP → gets filename → .NET saves filename only.
/// </summary>
public static class WebsiteAssetUrls
{
    public const string CompanyDetailsFolder = "company_details";
    public const string ProductPricingFolder = "product-pricing";
    public const string ProductServicesFolder = "product-and-services";
    public const string ImageGalleryFolder = "image-gallery";
    public const string SpecialOffersFolder = "special-offers";
    public const string PaymentDetailsFolder = "payment-details";

    // Planned PHP upload endpoints (not implemented yet — contract for later)
    public const string PlannedLogoUploadPath = "/api/upload/website/logo.php";
    public const string PlannedHeroUploadPath = "/api/upload/website/hero.php";
    public const string PlannedProductImageUploadPath = "/api/upload/website/product-image.php";
    public const string PlannedServiceImageUploadPath = "/api/upload/website/service-image.php";
    public const string PlannedGalleryUploadPath = "/api/upload/website/gallery.php";
    public const string PlannedOfferImageUploadPath = "/api/upload/website/offer-image.php";

    public static string PublicUrl(AppOptions app, string folder, string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "";

        var name = fileName.Trim().Replace('\\', '/');
        if (name.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return name;

        // If DB already stores a relative path like company_details/x.jpg
        if (name.Contains('/', StringComparison.Ordinal))
        {
            var baseRoot = Combine(app.PhpSiteBaseUrl, app.WebsiteUploadsPath);
            return Combine(baseRoot, name.TrimStart('/'));
        }

        return Combine(Combine(app.PhpSiteBaseUrl, app.WebsiteUploadsPath), folder, name);
    }

    public static object DescribeUploadContract(AppOptions app) => new
    {
        strategy = "Upload files to PHP domain; save filename via .NET API",
        phpSiteBaseUrl = app.PhpSiteBaseUrl.TrimEnd('/'),
        websiteUploadsPath = app.WebsiteUploadsPath,
        folders = new
        {
            logoAndHero = CompanyDetailsFolder,
            products = ProductPricingFolder,
            services = ProductServicesFolder,
            gallery = ImageGalleryFolder,
            specialOffers = SpecialOffersFolder,
            paymentQr = PaymentDetailsFolder
        },
        plannedPhpUploadEndpoints = new
        {
            logo = PlannedLogoUploadPath,
            hero = PlannedHeroUploadPath,
            productImage = PlannedProductImageUploadPath,
            serviceImage = PlannedServiceImageUploadPath,
            gallery = PlannedGalleryUploadPath,
            offerImage = PlannedOfferImageUploadPath
        },
        expectedPhpUploadResponse = new
        {
            success = true,
            fileName = "123_logo_abc.jpg",
            folder = CompanyDetailsFolder,
            publicUrl = PublicUrl(app, CompanyDetailsFolder, "123_logo_abc.jpg")
        },
        thenCallDotNet = "PUT/POST digi-cards/{id}/… with logoLocation / productImage / galleryImage / offerImage = fileName only"
    };

    private static string Combine(params string[] parts)
    {
        var result = parts[0].TrimEnd('/');
        for (var i = 1; i < parts.Length; i++)
        {
            var p = parts[i].Trim('/');
            if (string.IsNullOrEmpty(p)) continue;
            result += "/" + p;
        }
        return result;
    }
}
