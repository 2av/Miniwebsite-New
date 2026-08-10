using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Application.Common.Options;
using MiniWebsite.Application.Website.Dtos;
using MiniWebsite.Domain.Entities;

namespace MiniWebsite.Application.Website;

public partial class WebsiteService : IWebsiteService
{
    private readonly IApplicationDbContext _db;
    private readonly AppOptions _app;

    public WebsiteService(IApplicationDbContext db, IOptions<AppOptions> app)
    {
        _db = db;
        _app = app.Value;
    }

    public async Task<ApiResult<BusinessNameDto>> GetBusinessNameAsync(int cardId, string ownerEmail, CancellationToken ct = default)
    {
        var card = await LoadOwnedCardAsync(cardId, ownerEmail, ct);
        if (card == null) return ApiResult<BusinessNameDto>.Fail("Card not found or access denied.");
        var meta = await _db.CardSlugHistories.AsNoTracking().FirstOrDefaultAsync(m => m.CardId == cardId, ct);
        return ApiResult<BusinessNameDto>.Ok(new BusinessNameDto(card.Id, card.DCompName, card.DDisplayName, card.CardId, card.DNameChangeCount, meta?.PreviousSlug));
    }

    public async Task<ApiResult<CheckNameResponse>> CheckNameAsync(string compName, int? excludeId, CancellationToken ct = default)
    {
        var cleaned = CleanCompName(compName);
        if (string.IsNullOrWhiteSpace(cleaned))
            return ApiResult<CheckNameResponse>.Ok(new CheckNameResponse(false, "", "Business name is required."));

        var slug = ToSlug(cleaned);
        var nameTaken = await _db.Cards.AnyAsync(c => c.DCompName == cleaned && (!excludeId.HasValue || c.Id != excludeId), ct);
        if (nameTaken)
            return ApiResult<CheckNameResponse>.Ok(new CheckNameResponse(false, slug, "Business name already exists."));

        var slugTaken = await _db.Cards.AnyAsync(c => c.CardId == slug && (!excludeId.HasValue || c.Id != excludeId), ct);
        if (slugTaken)
            return ApiResult<CheckNameResponse>.Ok(new CheckNameResponse(false, slug, "URL slug already taken."));

        var prevTaken = await _db.CardSlugHistories.AnyAsync(p => p.PreviousSlug == slug && (!excludeId.HasValue || p.CardId != excludeId.Value), ct);
        if (prevTaken)
            return ApiResult<CheckNameResponse>.Ok(new CheckNameResponse(false, slug, "URL slug is reserved (previous)."));

        return ApiResult<CheckNameResponse>.Ok(new CheckNameResponse(true, slug, null));
    }

    public async Task<ApiResult<BusinessNameDto>> CreateBusinessNameAsync(UpsertBusinessNameRequest request, CancellationToken ct = default)
    {
        var check = await CheckNameAsync(request.CompName, null, ct);
        if (!check.Success || check.Data is null || !check.Data.Available)
            return ApiResult<BusinessNameDto>.Fail(check.Data?.Message ?? "Name unavailable.");

        var email = request.OwnerEmail.Trim().ToLowerInvariant();
        var cleaned = CleanCompName(request.CompName);
        var slug = check.Data.Slug;
        var display = string.IsNullOrWhiteSpace(request.DisplayName) ? cleaned : request.DisplayName.Trim();
        var now = DateTime.Now;

        var card = new Card
        {
            DCompName = cleaned,
            DDisplayName = display,
            CardId = slug,
            UserEmail = email,
            FUserEmail = string.IsNullOrWhiteSpace(request.FranchiseeEmail) ? "" : request.FranchiseeEmail.Trim(),
            DPaymentStatus = "Created",
            DCardStatus = "Active",
            UploadedDate = now,
            ValidityDate = now.AddYears(1),
            ComplimentaryEnabled = request.Complimentary ? "Yes" : "No",
            DPositionPrimary = "",
            DPositionSecondary = "",
            DGstNumber = "",
            DAddress2 = "",
            DCity = "",
            DState = "",
            DPincode = "",
            DCountry = ""
        };
        _db.Cards.Add(card);
        await _db.SaveChangesAsync(ct);

        return ApiResult<BusinessNameDto>.Ok(
            new BusinessNameDto(card.Id, card.DCompName, card.DDisplayName, card.CardId, card.DNameChangeCount),
            "Mini Website created.");
    }

    public async Task<ApiResult<BusinessNameDto>> UpdateBusinessNameAsync(int cardId, UpsertBusinessNameRequest request, CancellationToken ct = default)
    {
        var card = await LoadOwnedCardTrackedAsync(cardId, request.OwnerEmail, ct);
        if (card == null) return ApiResult<BusinessNameDto>.Fail("Card not found or access denied.");

        var cleaned = CleanCompName(request.CompName);
        var check = await CheckNameAsync(cleaned, cardId, ct);
        if (!check.Success || check.Data is null || !check.Data.Available)
            return ApiResult<BusinessNameDto>.Fail(check.Data?.Message ?? "Name unavailable.");

        var oldSlug = card.CardId ?? "";
        var newSlug = check.Data.Slug;
        if (!string.Equals(oldSlug, newSlug, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(oldSlug))
        {
            var meta = await _db.CardSlugHistories.FirstOrDefaultAsync(m => m.CardId == cardId, ct);
            if (meta == null)
            {
                _db.CardSlugHistories.Add(new CardSlugHistory
                {
                    CardId = cardId,
                    PreviousSlug = oldSlug
                });
            }
            else
            {
                meta.PreviousSlug = oldSlug;
            }
            card.DNameChangeCount += 1;
        }

        card.DCompName = cleaned;
        card.DDisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? cleaned : request.DisplayName.Trim();
        card.CardId = newSlug;
        await _db.SaveChangesAsync(ct);

        return ApiResult<BusinessNameDto>.Ok(
            new BusinessNameDto(card.Id, card.DCompName, card.DDisplayName, card.CardId, card.DNameChangeCount),
            "Business name updated.");
    }

    public async Task<ApiResult<ThemeDto>> GetThemeAsync(int cardId, string ownerEmail, CancellationToken ct = default)
    {
        var card = await LoadOwnedCardAsync(cardId, ownerEmail, ct);
        return card == null
            ? ApiResult<ThemeDto>.Fail("Card not found or access denied.")
            : ApiResult<ThemeDto>.Ok(new ThemeDto(card.DCss));
    }

    public async Task<ApiResult<ThemeDto>> UpdateThemeAsync(int cardId, string ownerEmail, UpdateThemeRequest request, CancellationToken ct = default)
    {
        var card = await LoadOwnedCardTrackedAsync(cardId, ownerEmail, ct);
        if (card == null) return ApiResult<ThemeDto>.Fail("Card not found or access denied.");
        card.DCss = request.DCss?.Trim();
        await _db.SaveChangesAsync(ct);
        return ApiResult<ThemeDto>.Ok(new ThemeDto(card.DCss), "Theme updated.");
    }

    public async Task<ApiResult<IReadOnlyList<ThemeCatalogItemDto>>> ListThemesAsync(CancellationToken ct = default)
    {
        try
        {
            var rows = await _db.ThemeCatalog.AsNoTracking()
                .Where(t => t.IsActive)
                .OrderBy(t => t.SortOrder)
                .ThenBy(t => t.ThemeNumber)
                .Select(t => new
                {
                    Number = t.ThemeNumber,
                    Name = t.ThemeName,
                    t.CssValue,
                    t.PreviewImage,
                })
                .ToListAsync(ct);

            if (rows.Count > 0)
            {
                IReadOnlyList<ThemeCatalogItemDto> list = rows
                    .Where(r => r.Number > 0)
                    .Select(r => new ThemeCatalogItemDto(
                        r.Number,
                        string.IsNullOrWhiteSpace(r.Name) ? $"Theme {r.Number}" : r.Name!,
                        string.IsNullOrWhiteSpace(r.CssValue) ? $"theme/css/theme{r.Number}.css" : r.CssValue!,
                        string.IsNullOrWhiteSpace(r.PreviewImage)
                            ? null
                            : CombinePublic($"assets/images/templates/{r.PreviewImage.Trim()}")))
                    .ToList();
                return ApiResult<IReadOnlyList<ThemeCatalogItemDto>>.Ok(list);
            }
        }
        catch
        {
            // catalog table may be missing — fall through
        }

        // Fallback: themes 1..60
        IReadOnlyList<ThemeCatalogItemDto> fallback = Enumerable.Range(1, 60)
            .Select(n => new ThemeCatalogItemDto(
                n,
                $"Theme {n}",
                $"theme/css/theme{n}.css",
                CombinePublic($"assets/images/templates/template{n}.png")))
            .ToList();
        return ApiResult<IReadOnlyList<ThemeCatalogItemDto>>.Ok(fallback);
    }

    private string? CombinePublic(string relative)
    {
        var baseUrl = (_app.PhpSiteBaseUrl ?? "").TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl)) return relative;
        return $"{baseUrl}/{relative.TrimStart('/')}";
    }

    public async Task<ApiResult<CompanyDetailsDto>> GetCompanyDetailsAsync(int cardId, string ownerEmail, CancellationToken ct = default)
    {
        var card = await LoadOwnedCardAsync(cardId, ownerEmail, ct);
        if (card == null) return ApiResult<CompanyDetailsDto>.Fail("Card not found or access denied.");
        return ApiResult<CompanyDetailsDto>.Ok(MapCompany(card));
    }

    public async Task<ApiResult<CompanyDetailsDto>> UpdateCompanyDetailsAsync(int cardId, string ownerEmail, UpdateCompanyDetailsRequest request, CancellationToken ct = default)
    {
        var card = await LoadOwnedCardTrackedAsync(cardId, ownerEmail, ct);
        if (card == null) return ApiResult<CompanyDetailsDto>.Fail("Card not found or access denied.");

        card.DFName = request.FirstName?.Trim();
        card.DLName = request.LastName?.Trim();
        card.DPositionPrimary = request.PositionPrimary?.Trim() ?? "";
        card.DPositionSecondary = request.PositionSecondary?.Trim() ?? "";
        card.DPosition = request.Position?.Trim();
        card.DContact = request.Contact?.Trim();
        card.DContact2 = request.Contact2?.Trim();
        card.DWhatsapp = request.Whatsapp?.Trim();
        card.DGstNumber = request.GstNumber?.Trim() ?? "";
        card.DAddress = request.Address?.Trim();
        card.DAddress2 = request.Address2?.Trim() ?? "";
        card.DCity = request.City?.Trim() ?? "";
        card.DState = request.State?.Trim() ?? "";
        card.DPincode = request.Pincode?.Trim() ?? "";
        card.DCountry = request.Country?.Trim() ?? "";
        card.DEmail = request.Email?.Trim();
        card.DWebsite = request.Website?.Trim();
        card.DLocation = request.Location?.Trim();
        card.DCompEstDate = request.CompEstDate?.Trim();
        card.DAboutUs = request.AboutUs?.Trim();
        card.DBusinessHours = request.BusinessHours;
        if (request.LogoLocation != null) card.DLogoLocation = request.LogoLocation;
        if (request.HeroImageLocation != null) card.DHeroImageLocation = request.HeroImageLocation;
        if (request.BusinessProfileType != null) card.BusinessProfileType = request.BusinessProfileType.Trim();
        if (request.BusinessType != null) card.BusinessType = request.BusinessType.Trim();
        if (request.BusinessOperationArea != null) card.BusinessOperationArea = request.BusinessOperationArea.Trim();
        if (request.BusinessOperationLocations != null) card.BusinessOperationLocations = request.BusinessOperationLocations;

        await _db.SaveChangesAsync(ct);
        return ApiResult<CompanyDetailsDto>.Ok(MapCompany(card), "Company details updated.");
    }

    public async Task<ApiResult<SocialLinksDto>> GetSocialAsync(int cardId, string ownerEmail, CancellationToken ct = default)
    {
        var card = await LoadOwnedCardAsync(cardId, ownerEmail, ct);
        return card == null
            ? ApiResult<SocialLinksDto>.Fail("Card not found or access denied.")
            : ApiResult<SocialLinksDto>.Ok(new SocialLinksDto(card.DFb, card.DTwitter, card.DInstagram, card.DLinkedin, card.DYoutube, card.DPinterest));
    }

    public async Task<ApiResult<SocialLinksDto>> UpdateSocialAsync(int cardId, string ownerEmail, UpdateSocialLinksRequest request, CancellationToken ct = default)
    {
        var card = await LoadOwnedCardTrackedAsync(cardId, ownerEmail, ct);
        if (card == null) return ApiResult<SocialLinksDto>.Fail("Card not found or access denied.");
        card.DFb = request.Fb;
        card.DTwitter = request.Twitter;
        card.DInstagram = request.Instagram;
        card.DLinkedin = request.Linkedin;
        card.DYoutube = request.Youtube;
        card.DPinterest = request.Pinterest;
        await _db.SaveChangesAsync(ct);
        return ApiResult<SocialLinksDto>.Ok(new SocialLinksDto(card.DFb, card.DTwitter, card.DInstagram, card.DLinkedin, card.DYoutube, card.DPinterest), "Social links updated.");
    }

    public async Task<ApiResult<PaymentDetailsDto>> GetPaymentAsync(int cardId, string ownerEmail, CancellationToken ct = default)
    {
        var card = await LoadOwnedCardAsync(cardId, ownerEmail, ct);
        return card == null
            ? ApiResult<PaymentDetailsDto>.Fail("Card not found or access denied.")
            : ApiResult<PaymentDetailsDto>.Ok(new PaymentDetailsDto(card.DPaytm, card.DGooglePay, card.DPhonePay, card.DAccountNo, card.DIfsc, card.DAcName, card.DBankName, card.DAcType));
    }

    public async Task<ApiResult<PaymentDetailsDto>> UpdatePaymentAsync(int cardId, string ownerEmail, UpdatePaymentDetailsRequest request, CancellationToken ct = default)
    {
        var card = await LoadOwnedCardTrackedAsync(cardId, ownerEmail, ct);
        if (card == null) return ApiResult<PaymentDetailsDto>.Fail("Card not found or access denied.");
        card.DPaytm = request.Paytm;
        card.DGooglePay = request.GooglePay;
        card.DPhonePay = request.PhonePay;
        card.DAccountNo = request.AccountNo;
        card.DIfsc = request.Ifsc;
        card.DAcName = request.AcName;
        card.DBankName = request.BankName;
        card.DAcType = request.AcType;
        await _db.SaveChangesAsync(ct);
        return ApiResult<PaymentDetailsDto>.Ok(new PaymentDetailsDto(card.DPaytm, card.DGooglePay, card.DPhonePay, card.DAccountNo, card.DIfsc, card.DAcName, card.DBankName, card.DAcType), "Payment details updated.");
    }

    public async Task<ApiResult<VideosDto>> GetVideosAsync(int cardId, string ownerEmail, CancellationToken ct = default)
    {
        var card = await LoadOwnedCardAsync(cardId, ownerEmail, ct);
        if (card == null) return ApiResult<VideosDto>.Fail("Card not found or access denied.");
        return ApiResult<VideosDto>.Ok(new VideosDto(GetYoutubeList(card)));
    }

    public async Task<ApiResult<VideosDto>> UpdateVideosAsync(int cardId, string ownerEmail, UpdateVideosRequest request, CancellationToken ct = default)
    {
        var card = await LoadOwnedCardTrackedAsync(cardId, ownerEmail, ct);
        if (card == null) return ApiResult<VideosDto>.Fail("Card not found or access denied.");
        SetYoutubeList(card, request.Urls);
        await _db.SaveChangesAsync(ct);
        return ApiResult<VideosDto>.Ok(new VideosDto(GetYoutubeList(card)), "Videos updated.");
    }

    // ---------- products ----------
    public async Task<ApiResult<IReadOnlyList<ProductDto>>> ListProductsAsync(int cardId, string ownerEmail, CancellationToken ct = default)
    {
        var ctx = await ResolveChildContextAsync(cardId, ownerEmail, ct);
        if (ctx == null) return ApiResult<IReadOnlyList<ProductDto>>.Fail("Card not found or access denied.");
        var items = await _db.CardProducts.AsNoTracking()
            .Where(p => p.CardId == ctx.Value.CardId || p.CardSlug == ctx.Value.CardSlug)
            .OrderBy(p => p.DisplayOrder).ThenBy(p => p.Id)
            .ToListAsync(ct);
        return ApiResult<IReadOnlyList<ProductDto>>.Ok(items.Select(MapProduct).ToList());
    }

    public async Task<ApiResult<ProductDto>> CreateProductAsync(int cardId, string ownerEmail, UpsertProductRequest request, CancellationToken ct = default)
    {
        var ctx = await ResolveChildContextAsync(cardId, ownerEmail, ct);
        if (ctx == null) return ApiResult<ProductDto>.Fail("Card not found or access denied.");
        var row = new CardProduct
        {
            CardId = ctx.Value.CardId,
            CardSlug = ctx.Value.CardSlug,
            UserId = ctx.Value.UserId,
            ProductName = request.ProductName,
            ProductCategory = request.ProductCategory,
            CategorySource = request.CategorySource ?? "system",
            ProductDescription = request.ProductDescription ?? "",
            ProductImage = request.ProductImage,
            Mrp = request.Mrp,
            SellingPrice = request.SellingPrice,
            PriceType = request.PriceType ?? "fixed_price",
            PricingUnit = request.PricingUnit ?? "",
            CtaText = request.CtaText ?? "",
            DisplayOrder = request.DisplayOrder,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        _db.CardProducts.Add(row);
        await _db.SaveChangesAsync(ct);
        return ApiResult<ProductDto>.Ok(MapProduct(row), "Product created.");
    }

    public async Task<ApiResult<ProductDto>> UpdateProductAsync(int cardId, string ownerEmail, int productId, UpsertProductRequest request, CancellationToken ct = default)
    {
        var ctx = await ResolveChildContextAsync(cardId, ownerEmail, ct);
        if (ctx == null) return ApiResult<ProductDto>.Fail("Card not found or access denied.");
        var row = await _db.CardProducts.FirstOrDefaultAsync(
            p => p.Id == productId && (p.CardId == ctx.Value.CardId || p.CardSlug == ctx.Value.CardSlug), ct);
        if (row == null) return ApiResult<ProductDto>.Fail("Product not found.");
        row.ProductName = request.ProductName;
        row.ProductCategory = request.ProductCategory;
        row.CategorySource = request.CategorySource ?? row.CategorySource;
        row.ProductDescription = request.ProductDescription ?? "";
        if (request.ProductImage != null) row.ProductImage = request.ProductImage;
        row.Mrp = request.Mrp;
        row.SellingPrice = request.SellingPrice;
        row.PriceType = request.PriceType ?? row.PriceType;
        row.PricingUnit = request.PricingUnit ?? "";
        row.CtaText = request.CtaText ?? "";
        row.DisplayOrder = request.DisplayOrder;
        row.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync(ct);
        return ApiResult<ProductDto>.Ok(MapProduct(row), "Product updated.");
    }

    public async Task<ApiResult> DeleteProductAsync(int cardId, string ownerEmail, int productId, CancellationToken ct = default)
    {
        var ctx = await ResolveChildContextAsync(cardId, ownerEmail, ct);
        if (ctx == null) return ApiResult.Fail("Card not found or access denied.");
        var row = await _db.CardProducts.FirstOrDefaultAsync(
            p => p.Id == productId && (p.CardId == ctx.Value.CardId || p.CardSlug == ctx.Value.CardSlug), ct);
        if (row == null) return ApiResult.Fail("Product not found.");
        _db.CardProducts.Remove(row);
        await _db.SaveChangesAsync(ct);
        return ApiResult.Ok("Product deleted.");
    }

    // ---------- services ----------
    public async Task<ApiResult<IReadOnlyList<ServiceItemDto>>> ListServicesAsync(int cardId, string ownerEmail, CancellationToken ct = default)
    {
        var ctx = await ResolveChildContextAsync(cardId, ownerEmail, ct);
        if (ctx == null) return ApiResult<IReadOnlyList<ServiceItemDto>>.Fail("Card not found or access denied.");
        var items = await _db.CardServices.AsNoTracking()
            .Where(p => p.CardId == ctx.Value.CardId || p.CardSlug == ctx.Value.CardSlug)
            .OrderBy(p => p.DisplayOrder).ThenBy(p => p.Id)
            .ToListAsync(ct);
        return ApiResult<IReadOnlyList<ServiceItemDto>>.Ok(items.Select(MapService).ToList());
    }

    public async Task<ApiResult<ServiceItemDto>> CreateServiceAsync(int cardId, string ownerEmail, UpsertServiceRequest request, CancellationToken ct = default)
    {
        var ctx = await ResolveChildContextAsync(cardId, ownerEmail, ct);
        if (ctx == null) return ApiResult<ServiceItemDto>.Fail("Card not found or access denied.");
        var row = new CardService
        {
            CardId = ctx.Value.CardId,
            CardSlug = ctx.Value.CardSlug,
            UserId = ctx.Value.UserId,
            ProductName = request.ProductName,
            ProductCategory = request.ProductCategory,
            CategorySource = request.CategorySource ?? "system",
            ProductDescription = request.ProductDescription ?? "",
            ProductImage = request.ProductImage,
            DisplayOrder = request.DisplayOrder,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        _db.CardServices.Add(row);
        await _db.SaveChangesAsync(ct);
        return ApiResult<ServiceItemDto>.Ok(MapService(row), "Service created.");
    }

    public async Task<ApiResult<ServiceItemDto>> UpdateServiceAsync(int cardId, string ownerEmail, int serviceId, UpsertServiceRequest request, CancellationToken ct = default)
    {
        var ctx = await ResolveChildContextAsync(cardId, ownerEmail, ct);
        if (ctx == null) return ApiResult<ServiceItemDto>.Fail("Card not found or access denied.");
        var row = await _db.CardServices.FirstOrDefaultAsync(
            p => p.Id == serviceId && (p.CardId == ctx.Value.CardId || p.CardSlug == ctx.Value.CardSlug), ct);
        if (row == null) return ApiResult<ServiceItemDto>.Fail("Service not found.");
        row.ProductName = request.ProductName;
        row.ProductCategory = request.ProductCategory;
        row.CategorySource = request.CategorySource ?? row.CategorySource;
        row.ProductDescription = request.ProductDescription ?? "";
        if (request.ProductImage != null) row.ProductImage = request.ProductImage;
        row.DisplayOrder = request.DisplayOrder;
        row.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync(ct);
        return ApiResult<ServiceItemDto>.Ok(MapService(row), "Service updated.");
    }

    public async Task<ApiResult> DeleteServiceAsync(int cardId, string ownerEmail, int serviceId, CancellationToken ct = default)
    {
        var ctx = await ResolveChildContextAsync(cardId, ownerEmail, ct);
        if (ctx == null) return ApiResult.Fail("Card not found or access denied.");
        var row = await _db.CardServices.FirstOrDefaultAsync(
            p => p.Id == serviceId && (p.CardId == ctx.Value.CardId || p.CardSlug == ctx.Value.CardSlug), ct);
        if (row == null) return ApiResult.Fail("Service not found.");
        _db.CardServices.Remove(row);
        await _db.SaveChangesAsync(ct);
        return ApiResult.Ok("Service deleted.");
    }

    // ---------- gallery ----------
    public async Task<ApiResult<IReadOnlyList<GalleryItemDto>>> ListGalleryAsync(int cardId, string ownerEmail, CancellationToken ct = default)
    {
        var ctx = await ResolveChildContextAsync(cardId, ownerEmail, ct);
        if (ctx == null) return ApiResult<IReadOnlyList<GalleryItemDto>>.Fail("Card not found or access denied.");
        var items = await _db.CardGallery.AsNoTracking()
            .Where(p => p.CardId == ctx.Value.CardId || p.CardSlug == ctx.Value.CardSlug)
            .OrderBy(p => p.DisplayOrder).ThenBy(p => p.Id)
            .ToListAsync(ct);
        return ApiResult<IReadOnlyList<GalleryItemDto>>.Ok(items.Select(MapGallery).ToList());
    }

    public async Task<ApiResult<GalleryItemDto>> CreateGalleryAsync(int cardId, string ownerEmail, UpsertGalleryRequest request, CancellationToken ct = default)
    {
        var ctx = await ResolveChildContextAsync(cardId, ownerEmail, ct);
        if (ctx == null) return ApiResult<GalleryItemDto>.Fail("Card not found or access denied.");
        var maxOrder = await _db.CardGallery
            .Where(g => g.CardId == ctx.Value.CardId || g.CardSlug == ctx.Value.CardSlug)
            .MaxAsync(g => (int?)g.DisplayOrder, ct) ?? 0;
        var row = new CardGalleryImage
        {
            CardId = ctx.Value.CardId,
            CardSlug = ctx.Value.CardSlug,
            UserId = ctx.Value.UserId,
            GalleryImage = request.GalleryImage,
            DisplayOrder = request.DisplayOrder > 0 ? request.DisplayOrder : maxOrder + 1,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        _db.CardGallery.Add(row);
        await _db.SaveChangesAsync(ct);
        return ApiResult<GalleryItemDto>.Ok(MapGallery(row), "Gallery image saved.");
    }

    public async Task<ApiResult> DeleteGalleryAsync(int cardId, string ownerEmail, int imageId, CancellationToken ct = default)
    {
        var ctx = await ResolveChildContextAsync(cardId, ownerEmail, ct);
        if (ctx == null) return ApiResult.Fail("Card not found or access denied.");
        var row = await _db.CardGallery.FirstOrDefaultAsync(
            g => g.Id == imageId && (g.CardId == ctx.Value.CardId || g.CardSlug == ctx.Value.CardSlug), ct);
        if (row == null) return ApiResult.Fail("Gallery image not found.");
        _db.CardGallery.Remove(row);
        await _db.SaveChangesAsync(ct);
        return ApiResult.Ok("Gallery image deleted.");
    }

    // ---------- offers ----------
    public async Task<ApiResult<IReadOnlyList<SpecialOfferDto>>> ListOffersAsync(int cardId, string ownerEmail, CancellationToken ct = default)
    {
        var ctx = await ResolveChildContextAsync(cardId, ownerEmail, ct);
        if (ctx == null) return ApiResult<IReadOnlyList<SpecialOfferDto>>.Fail("Card not found or access denied.");
        var items = await _db.CardOffers.AsNoTracking()
            .Where(p => p.CardId == ctx.Value.CardId || p.CardSlug == ctx.Value.CardSlug)
            .OrderBy(p => p.DisplayOrder).ThenBy(p => p.Id)
            .ToListAsync(ct);
        return ApiResult<IReadOnlyList<SpecialOfferDto>>.Ok(items.Select(MapOffer).ToList());
    }

    public async Task<ApiResult<SpecialOfferDto>> CreateOfferAsync(int cardId, string ownerEmail, UpsertSpecialOfferRequest request, CancellationToken ct = default)
    {
        var ctx = await ResolveChildContextAsync(cardId, ownerEmail, ct);
        if (ctx == null) return ApiResult<SpecialOfferDto>.Fail("Card not found or access denied.");
        var row = new CardOffer
        {
            CardId = ctx.Value.CardId,
            CardSlug = ctx.Value.CardSlug,
            UserId = ctx.Value.UserId,
            OfferTitle = request.OfferTitle,
            OfferDescription = request.OfferDescription,
            OfferImage = request.OfferImage,
            Badge = request.Badge,
            DiscountPercentage = request.DiscountPercentage,
            StartDate = request.StartDate,
            StartTime = request.StartTime,
            EndDate = request.EndDate,
            EndTime = request.EndTime,
            Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status,
            DisplayOrder = request.DisplayOrder,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        _db.CardOffers.Add(row);
        await _db.SaveChangesAsync(ct);
        return ApiResult<SpecialOfferDto>.Ok(MapOffer(row), "Offer created.");
    }

    public async Task<ApiResult<SpecialOfferDto>> UpdateOfferAsync(int cardId, string ownerEmail, int offerId, UpsertSpecialOfferRequest request, CancellationToken ct = default)
    {
        var ctx = await ResolveChildContextAsync(cardId, ownerEmail, ct);
        if (ctx == null) return ApiResult<SpecialOfferDto>.Fail("Card not found or access denied.");
        var row = await _db.CardOffers.FirstOrDefaultAsync(
            o => o.Id == offerId && (o.CardId == ctx.Value.CardId || o.CardSlug == ctx.Value.CardSlug), ct);
        if (row == null) return ApiResult<SpecialOfferDto>.Fail("Offer not found.");
        row.OfferTitle = request.OfferTitle;
        row.OfferDescription = request.OfferDescription;
        if (request.OfferImage != null) row.OfferImage = request.OfferImage;
        row.Badge = request.Badge;
        row.DiscountPercentage = request.DiscountPercentage;
        row.StartDate = request.StartDate;
        row.StartTime = request.StartTime;
        row.EndDate = request.EndDate;
        row.EndTime = request.EndTime;
        row.Status = string.IsNullOrWhiteSpace(request.Status) ? row.Status : request.Status;
        row.DisplayOrder = request.DisplayOrder;
        row.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync(ct);
        return ApiResult<SpecialOfferDto>.Ok(MapOffer(row), "Offer updated.");
    }

    public async Task<ApiResult> DeleteOfferAsync(int cardId, string ownerEmail, int offerId, CancellationToken ct = default)
    {
        var ctx = await ResolveChildContextAsync(cardId, ownerEmail, ct);
        if (ctx == null) return ApiResult.Fail("Card not found or access denied.");
        var row = await _db.CardOffers.FirstOrDefaultAsync(
            o => o.Id == offerId && (o.CardId == ctx.Value.CardId || o.CardSlug == ctx.Value.CardSlug), ct);
        if (row == null) return ApiResult.Fail("Offer not found.");
        _db.CardOffers.Remove(row);
        await _db.SaveChangesAsync(ct);
        return ApiResult.Ok("Offer deleted.");
    }

    // ---------- helpers ----------
    private async Task<Card?> LoadOwnedCardAsync(int cardId, string ownerEmail, CancellationToken ct)
    {
        var email = ownerEmail.Trim().ToLowerInvariant();
        return await _db.Cards.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == cardId && c.UserEmail != null && c.UserEmail.ToLower() == email, ct);
    }

    private async Task<Card?> LoadOwnedCardTrackedAsync(int cardId, string ownerEmail, CancellationToken ct)
    {
        var email = ownerEmail.Trim().ToLowerInvariant();
        return await _db.Cards
            .FirstOrDefaultAsync(c => c.Id == cardId && c.UserEmail != null && c.UserEmail.ToLower() == email, ct);
    }

    private async Task<(int CardId, string CardSlug, int UserId)?> ResolveChildContextAsync(int cardId, string ownerEmail, CancellationToken ct)
    {
        var card = await LoadOwnedCardAsync(cardId, ownerEmail, ct);
        if (card == null) return null;
        var email = ownerEmail.Trim().ToLowerInvariant();
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email.ToLower() == email, ct);
        if (user == null) return null;
        var slug = card.Slug ?? card.CardId ?? "";
        return (card.Id, slug, user.Id);
    }

    private CompanyDetailsDto MapCompany(Card c) =>
        new(c.DFName, c.DLName, c.DPositionPrimary, c.DPositionSecondary, c.DPosition,
            c.DContact, c.DContact2, c.DWhatsapp, c.DGstNumber,
            c.DAddress, c.DAddress2, c.DCity, c.DState, c.DPincode, c.DCountry,
            c.DEmail, c.DWebsite, c.DLocation, c.DCompEstDate, c.DAboutUs, c.DBusinessHours,
            c.DLogoLocation, c.DHeroImageLocation,
            WebsiteAssetUrls.PublicUrl(_app, WebsiteAssetUrls.CompanyDetailsFolder, c.DLogoLocation),
            WebsiteAssetUrls.PublicUrl(_app, WebsiteAssetUrls.CompanyDetailsFolder, c.DHeroImageLocation),
            c.BusinessProfileType ?? "", c.BusinessType ?? "", c.BusinessOperationArea ?? "",
            c.BusinessOperationLocations);

    private ProductDto MapProduct(CardProduct p) =>
        new(p.Id, p.CardSlug, p.UserId, p.ProductName, p.ProductCategory, p.CategorySource, p.ProductDescription,
            p.ProductImage,
            WebsiteAssetUrls.PublicUrl(_app, WebsiteAssetUrls.ProductPricingFolder, p.ProductImage),
            p.Mrp, p.SellingPrice, p.PriceType, p.PricingUnit, p.CtaText, p.DisplayOrder);

    private ServiceItemDto MapService(CardService p) =>
        new(p.Id, p.CardSlug, p.UserId, p.ProductName, p.ProductCategory, p.CategorySource, p.ProductDescription, p.ProductImage,
            WebsiteAssetUrls.PublicUrl(_app, WebsiteAssetUrls.ProductServicesFolder, p.ProductImage),
            p.DisplayOrder);

    private GalleryItemDto MapGallery(CardGalleryImage g) =>
        new(g.Id, g.CardSlug, g.UserId, g.GalleryImage,
            WebsiteAssetUrls.PublicUrl(_app, WebsiteAssetUrls.ImageGalleryFolder, g.GalleryImage),
            g.DisplayOrder);

    private SpecialOfferDto MapOffer(CardOffer o) =>
        new(o.Id, o.CardSlug, o.UserId, o.OfferTitle, o.OfferDescription, o.OfferImage,
            WebsiteAssetUrls.PublicUrl(_app, WebsiteAssetUrls.SpecialOffersFolder, o.OfferImage),
            o.Badge, o.DiscountPercentage,
            o.StartDate, o.StartTime, o.EndDate, o.EndTime, o.Status, o.DisplayOrder);

    private static string CleanCompName(string name) =>
        CompNameCleanRegex().Replace(name.Trim(), "");

    private static string ToSlug(string cleaned) =>
        cleaned.Replace(' ', '-').Replace(".", "").Replace("&", "").Replace("/", "-").Replace("[", "").Replace("]", "");

    private static IReadOnlyList<string?> GetYoutubeList(Card c) =>
    [
        c.DYoutube1, c.DYoutube2, c.DYoutube3, c.DYoutube4, c.DYoutube5,
        c.DYoutube6, c.DYoutube7, c.DYoutube8, c.DYoutube9, c.DYoutube10,
        c.DYoutube11, c.DYoutube12, c.DYoutube13, c.DYoutube14, c.DYoutube15,
        c.DYoutube16, c.DYoutube17, c.DYoutube18, c.DYoutube19, c.DYoutube20
    ];

    private static void SetYoutubeList(Card c, IReadOnlyList<string?> urls)
    {
        string? At(int i) => urls.Count > i ? urls[i]?.Trim() : null;
        c.DYoutube1 = At(0); c.DYoutube2 = At(1); c.DYoutube3 = At(2); c.DYoutube4 = At(3); c.DYoutube5 = At(4);
        c.DYoutube6 = At(5); c.DYoutube7 = At(6); c.DYoutube8 = At(7); c.DYoutube9 = At(8); c.DYoutube10 = At(9);
        c.DYoutube11 = At(10); c.DYoutube12 = At(11); c.DYoutube13 = At(12); c.DYoutube14 = At(13); c.DYoutube15 = At(14);
        c.DYoutube16 = At(15); c.DYoutube17 = At(16); c.DYoutube18 = At(17); c.DYoutube19 = At(18); c.DYoutube20 = At(19);
    }

    [GeneratedRegex(@"[^A-Za-z0-9\s\-]")]
    private static partial Regex CompNameCleanRegex();
}
