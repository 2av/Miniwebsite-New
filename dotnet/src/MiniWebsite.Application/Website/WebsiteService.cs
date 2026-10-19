using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MiniWebsite.Application.Common.Extensions;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Application.Common.Options;
using MiniWebsite.Application.Website.Dtos;
using MiniWebsite.Domain.Entities;
using MiniWebsite.Domain.Common;

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
        var nameTaken = await _db.Cards.AnyAsync(c => c.CompanyName == cleaned && (!excludeId.HasValue || c.Id != excludeId), ct);
        if (nameTaken)
            return ApiResult<CheckNameResponse>.Ok(new CheckNameResponse(false, slug, "Business name already exists."));

        var slugTaken = await _db.Cards.AnyAsync(c => c.Slug == slug && (!excludeId.HasValue || c.Id != excludeId), ct);
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
        var now = AppClock.Now;
        var ownerId = await _db.Users.AsNoTracking()
            .Where(u => u.Email == email)
            .Select(u => (int?)u.Id)
            .FirstOrDefaultAsync(ct);
        var franchiseeEmail = string.IsNullOrWhiteSpace(request.FranchiseeEmail) ? "" : request.FranchiseeEmail.Trim();
        var franchiseeId = franchiseeEmail.Length == 0
            ? null
            : await _db.Users.AsNoTracking()
                .Where(u => u.Email == franchiseeEmail)
                .Select(u => (int?)u.Id)
                .FirstOrDefaultAsync(ct);

        var card = new Card
        {
            UserId = ownerId,
            FranchiseeUserId = franchiseeId,
            DCompName = cleaned,
            DDisplayName = display,
            CardId = slug,
            UserEmail = email,
            FUserEmail = franchiseeEmail,
            DCardStatus = "Active",
            UploadedDate = now,
            ValidityDate = now.AddYears(1),
            ComplimentaryEnabled = request.Complimentary ? "Yes" : "No",
            DPositionPrimary = "",
            DPositionSecondary = ""
        };
        _db.Cards.Add(card);
        await _db.SaveChangesAsync(ct);

        return ApiResult<BusinessNameDto>.Ok(
            new BusinessNameDto(card.Id, card.DCompName, card.DDisplayName, card.CardId, card.DNameChangeCount),
            "ShareBizz created.");
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
        var baseUrl = (_app.SiteBaseUrl ?? "").TrimEnd('/');
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
        User? owner = null;
        if (card.UserId is int ownerId)
            owner = await _db.Users.FirstOrDefaultAsync(u => u.Id == ownerId, ct);
        if (owner != null)
        {
            owner.Address.AddressLine = BlankToNull(request.Address);
            owner.Address.Address2 = BlankToNull(request.Address2);
            owner.Address.City = BlankToNull(request.City);
            owner.Address.State = BlankToNull(request.State);
            owner.Address.Pincode = BlankToNull(request.Pincode);
            owner.Address.Country = BlankToNull(request.Country);
            owner.Address.Gst = TrimGst(request.GstNumber);
        }
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
        return ApiResult<CompanyDetailsDto>.Ok(MapCompany(card, owner?.Address), "Company details updated.");
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
        if (card == null) return ApiResult<PaymentDetailsDto>.Fail("Card not found or access denied.");
        var bank = card.UserId is int userId
            ? await _db.UserBankDetails.AsNoTracking().FirstOrDefaultAsync(b => b.UserId == userId, ct)
            : null;
        return ApiResult<PaymentDetailsDto>.Ok(MapPayment(card.User?.Payment, bank));
    }

    public async Task<ApiResult<PaymentDetailsDto>> UpdatePaymentAsync(int cardId, string ownerEmail, UpdatePaymentDetailsRequest request, CancellationToken ct = default)
    {
        var card = await LoadOwnedCardTrackedAsync(cardId, ownerEmail, ct);
        if (card?.UserId is not int userId) return ApiResult<PaymentDetailsDto>.Fail("Card not found or access denied.");

        var owner = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (owner == null) return ApiResult<PaymentDetailsDto>.Fail("Card not found or access denied.");
        owner.Payment.Paytm = BlankToNull(request.Paytm);
        owner.Payment.GooglePay = BlankToNull(request.GooglePay);
        owner.Payment.PhonePay = BlankToNull(request.PhonePay);

        var now = AppClock.Now;
        var bank = await _db.UserBankDetails.FirstOrDefaultAsync(b => b.UserId == userId, ct);
        if (bank == null)
        {
            bank = new UserBankDetails { UserId = userId, CreatedAt = now };
            _db.UserBankDetails.Add(bank);
        }
        bank.BankName = BlankToNull(request.BankName);
        bank.AccountHolderName = BlankToNull(request.AcName);
        bank.AccountNumber = BlankToNull(request.AccountNo);
        bank.IfscCode = BlankToNull(request.Ifsc);
        bank.AccountType = BlankToNull(request.AcType);
        bank.UpdatedAt = now;

        await _db.SaveChangesAsync(ct);
        return ApiResult<PaymentDetailsDto>.Ok(MapPayment(owner.Payment, bank), "Payment details updated.");
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
            .Where(p => p.CardId == ctx.Value.CardId)
            .OrderBy(p => p.DisplayOrder).ThenBy(p => p.Id)
            .ToListAsync(ct);
        return ApiResult<IReadOnlyList<ProductDto>>.Ok(items.Select(p => MapProduct(p, ctx.Value)).ToList());
    }

    public async Task<ApiResult<ProductDto>> CreateProductAsync(int cardId, string ownerEmail, UpsertProductRequest request, CancellationToken ct = default)
    {
        var ctx = await ResolveChildContextAsync(cardId, ownerEmail, ct);
        if (ctx == null) return ApiResult<ProductDto>.Fail("Card not found or access denied.");
        var row = new CardProduct
        {
            CardId = ctx.Value.CardId,
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
            CreatedAt = AppClock.Now,
            UpdatedAt = AppClock.Now
        };
        _db.CardProducts.Add(row);
        await _db.SaveChangesAsync(ct);
        return ApiResult<ProductDto>.Ok(MapProduct(row, ctx.Value), "Product created.");
    }

    public async Task<ApiResult<ProductDto>> UpdateProductAsync(int cardId, string ownerEmail, int productId, UpsertProductRequest request, CancellationToken ct = default)
    {
        var ctx = await ResolveChildContextAsync(cardId, ownerEmail, ct);
        if (ctx == null) return ApiResult<ProductDto>.Fail("Card not found or access denied.");
        var row = await _db.CardProducts.FirstOrDefaultAsync(
            p => p.Id == productId && (p.CardId == ctx.Value.CardId), ct);
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
        row.UpdatedAt = AppClock.Now;
        await _db.SaveChangesAsync(ct);
        return ApiResult<ProductDto>.Ok(MapProduct(row, ctx.Value), "Product updated.");
    }

    public async Task<ApiResult> DeleteProductAsync(int cardId, string ownerEmail, int productId, CancellationToken ct = default)
    {
        var ctx = await ResolveChildContextAsync(cardId, ownerEmail, ct);
        if (ctx == null) return ApiResult.Fail("Card not found or access denied.");
        var row = await _db.CardProducts.FirstOrDefaultAsync(
            p => p.Id == productId && (p.CardId == ctx.Value.CardId), ct);
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
            .Where(p => p.CardId == ctx.Value.CardId)
            .OrderBy(p => p.DisplayOrder).ThenBy(p => p.Id)
            .ToListAsync(ct);
        return ApiResult<IReadOnlyList<ServiceItemDto>>.Ok(items.Select(p => MapService(p, ctx.Value)).ToList());
    }

    public async Task<ApiResult<ServiceItemDto>> CreateServiceAsync(int cardId, string ownerEmail, UpsertServiceRequest request, CancellationToken ct = default)
    {
        var ctx = await ResolveChildContextAsync(cardId, ownerEmail, ct);
        if (ctx == null) return ApiResult<ServiceItemDto>.Fail("Card not found or access denied.");
        var row = new CardService
        {
            CardId = ctx.Value.CardId,
            ProductName = request.ProductName,
            ProductCategory = request.ProductCategory,
            CategorySource = request.CategorySource ?? "system",
            ProductDescription = request.ProductDescription ?? "",
            ProductImage = request.ProductImage,
            DisplayOrder = request.DisplayOrder,
            CreatedAt = AppClock.Now,
            UpdatedAt = AppClock.Now
        };
        _db.CardServices.Add(row);
        await _db.SaveChangesAsync(ct);
        return ApiResult<ServiceItemDto>.Ok(MapService(row, ctx.Value), "Service created.");
    }

    public async Task<ApiResult<ServiceItemDto>> UpdateServiceAsync(int cardId, string ownerEmail, int serviceId, UpsertServiceRequest request, CancellationToken ct = default)
    {
        var ctx = await ResolveChildContextAsync(cardId, ownerEmail, ct);
        if (ctx == null) return ApiResult<ServiceItemDto>.Fail("Card not found or access denied.");
        var row = await _db.CardServices.FirstOrDefaultAsync(
            p => p.Id == serviceId && (p.CardId == ctx.Value.CardId), ct);
        if (row == null) return ApiResult<ServiceItemDto>.Fail("Service not found.");
        row.ProductName = request.ProductName;
        row.ProductCategory = request.ProductCategory;
        row.CategorySource = request.CategorySource ?? row.CategorySource;
        row.ProductDescription = request.ProductDescription ?? "";
        if (request.ProductImage != null) row.ProductImage = request.ProductImage;
        row.DisplayOrder = request.DisplayOrder;
        row.UpdatedAt = AppClock.Now;
        await _db.SaveChangesAsync(ct);
        return ApiResult<ServiceItemDto>.Ok(MapService(row, ctx.Value), "Service updated.");
    }

    public async Task<ApiResult> DeleteServiceAsync(int cardId, string ownerEmail, int serviceId, CancellationToken ct = default)
    {
        var ctx = await ResolveChildContextAsync(cardId, ownerEmail, ct);
        if (ctx == null) return ApiResult.Fail("Card not found or access denied.");
        var row = await _db.CardServices.FirstOrDefaultAsync(
            p => p.Id == serviceId && (p.CardId == ctx.Value.CardId), ct);
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
            .Where(p => p.CardId == ctx.Value.CardId)
            .OrderBy(p => p.DisplayOrder).ThenBy(p => p.Id)
            .ToListAsync(ct);
        return ApiResult<IReadOnlyList<GalleryItemDto>>.Ok(items.Select(g => MapGallery(g, ctx.Value)).ToList());
    }

    public async Task<ApiResult<GalleryItemDto>> CreateGalleryAsync(int cardId, string ownerEmail, UpsertGalleryRequest request, CancellationToken ct = default)
    {
        var ctx = await ResolveChildContextAsync(cardId, ownerEmail, ct);
        if (ctx == null) return ApiResult<GalleryItemDto>.Fail("Card not found or access denied.");
        var maxOrder = await _db.CardGallery
            .Where(g => g.CardId == ctx.Value.CardId)
            .MaxAsync(g => (int?)g.DisplayOrder, ct) ?? 0;
        var row = new CardGalleryImage
        {
            CardId = ctx.Value.CardId,
            GalleryImage = request.GalleryImage,
            DisplayOrder = request.DisplayOrder > 0 ? request.DisplayOrder : maxOrder + 1,
            CreatedAt = AppClock.Now,
            UpdatedAt = AppClock.Now
        };
        _db.CardGallery.Add(row);
        await _db.SaveChangesAsync(ct);
        return ApiResult<GalleryItemDto>.Ok(MapGallery(row, ctx.Value), "Gallery image saved.");
    }

    public async Task<ApiResult> DeleteGalleryAsync(int cardId, string ownerEmail, int imageId, CancellationToken ct = default)
    {
        var ctx = await ResolveChildContextAsync(cardId, ownerEmail, ct);
        if (ctx == null) return ApiResult.Fail("Card not found or access denied.");
        var row = await _db.CardGallery.FirstOrDefaultAsync(
            g => g.Id == imageId && (g.CardId == ctx.Value.CardId), ct);
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
            .Where(p => p.CardId == ctx.Value.CardId)
            .OrderBy(p => p.DisplayOrder).ThenBy(p => p.Id)
            .ToListAsync(ct);
        return ApiResult<IReadOnlyList<SpecialOfferDto>>.Ok(items.Select(o => MapOffer(o, ctx.Value)).ToList());
    }

    public async Task<ApiResult<SpecialOfferDto>> CreateOfferAsync(int cardId, string ownerEmail, UpsertSpecialOfferRequest request, CancellationToken ct = default)
    {
        var ctx = await ResolveChildContextAsync(cardId, ownerEmail, ct);
        if (ctx == null) return ApiResult<SpecialOfferDto>.Fail("Card not found or access denied.");
        var row = new CardOffer
        {
            CardId = ctx.Value.CardId,
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
            CreatedAt = AppClock.Now,
            UpdatedAt = AppClock.Now
        };
        _db.CardOffers.Add(row);
        await _db.SaveChangesAsync(ct);
        return ApiResult<SpecialOfferDto>.Ok(MapOffer(row, ctx.Value), "Offer created.");
    }

    public async Task<ApiResult<SpecialOfferDto>> UpdateOfferAsync(int cardId, string ownerEmail, int offerId, UpsertSpecialOfferRequest request, CancellationToken ct = default)
    {
        var ctx = await ResolveChildContextAsync(cardId, ownerEmail, ct);
        if (ctx == null) return ApiResult<SpecialOfferDto>.Fail("Card not found or access denied.");
        var row = await _db.CardOffers.FirstOrDefaultAsync(
            o => o.Id == offerId && (o.CardId == ctx.Value.CardId), ct);
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
        row.UpdatedAt = AppClock.Now;
        await _db.SaveChangesAsync(ct);
        return ApiResult<SpecialOfferDto>.Ok(MapOffer(row, ctx.Value), "Offer updated.");
    }

    public async Task<ApiResult> DeleteOfferAsync(int cardId, string ownerEmail, int offerId, CancellationToken ct = default)
    {
        var ctx = await ResolveChildContextAsync(cardId, ownerEmail, ct);
        if (ctx == null) return ApiResult.Fail("Card not found or access denied.");
        var row = await _db.CardOffers.FirstOrDefaultAsync(
            o => o.Id == offerId && (o.CardId == ctx.Value.CardId), ct);
        if (row == null) return ApiResult.Fail("Offer not found.");
        _db.CardOffers.Remove(row);
        await _db.SaveChangesAsync(ct);
        return ApiResult.Ok("Offer deleted.");
    }

    // ---------- helpers ----------
    private async Task<Card?> LoadOwnedCardAsync(int cardId, string ownerEmail, CancellationToken ct)
    {
        var userId = await _db.FindUserIdAsync(ownerEmail, ct);
        if (userId == null) return null;
        return await _db.Cards.AsNoTracking()
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == cardId && c.UserId == userId, ct);
    }

    private async Task<Card?> LoadOwnedCardTrackedAsync(int cardId, string ownerEmail, CancellationToken ct)
    {
        var userId = await _db.FindUserIdAsync(ownerEmail, ct);
        if (userId == null) return null;
        return await _db.Cards
            .FirstOrDefaultAsync(c => c.Id == cardId && c.UserId == userId, ct);
    }

    private async Task<(int CardId, string CardSlug, int UserId)?> ResolveChildContextAsync(int cardId, string ownerEmail, CancellationToken ct)
    {
        var email = ownerEmail.Trim();
        var userId = await _db.Users.AsNoTracking()
            .Where(u => u.Email == email)
            .Select(u => (int?)u.Id)
            .FirstOrDefaultAsync(ct);
        if (userId == null) return null;
        var card = await _db.Cards.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == cardId && c.UserId == userId, ct);
        if (card == null) return null;
        var slug = card.Slug ?? card.CardId ?? "";
        return (card.Id, slug, userId.Value);
    }

    private static PaymentDetailsDto MapPayment(UserPaymentDetails? pay, UserBankDetails? bank) =>
        new(pay?.Paytm, pay?.GooglePay, pay?.PhonePay, bank?.AccountNumber, bank?.IfscCode, bank?.AccountHolderName, bank?.BankName, bank?.AccountType);

    private static string? TrimGst(string? value)
    {
        var trimmed = BlankToNull(value);
        return trimmed == null || trimmed.Length <= 50 ? trimmed : trimmed[..50];
    }

    private static string? BlankToNull(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private CompanyDetailsDto MapCompany(Card c, UserAddress? address = null)
    {
        address ??= c.User?.Address;
        return new(c.DFName, c.DLName, c.DPositionPrimary, c.DPositionSecondary, c.DPosition,
            c.DContact, c.DContact2, c.DWhatsapp, address?.Gst,
            address?.AddressLine, address?.Address2, address?.City, address?.State, address?.Pincode, address?.Country,
            c.DEmail, c.DWebsite, c.DLocation, c.DCompEstDate, c.DAboutUs, c.DBusinessHours,
            c.DLogoLocation, c.DHeroImageLocation,
            WebsiteAssetUrls.PublicUrl(_app, WebsiteAssetUrls.CompanyDetailsFolder, c.DLogoLocation),
            WebsiteAssetUrls.PublicUrl(_app, WebsiteAssetUrls.CompanyDetailsFolder, c.DHeroImageLocation),
            c.BusinessProfileType ?? "", c.BusinessType ?? "", c.BusinessOperationArea ?? "",
            c.BusinessOperationLocations);
    }

    private ProductDto MapProduct(CardProduct p, (int CardId, string CardSlug, int UserId) ctx) =>
        new(p.Id, ctx.CardSlug, ctx.UserId, p.ProductName, p.ProductCategory, p.CategorySource, p.ProductDescription,
            p.ProductImage,
            WebsiteAssetUrls.PublicUrl(_app, WebsiteAssetUrls.ProductPricingFolder, p.ProductImage),
            p.Mrp, p.SellingPrice, p.PriceType, p.PricingUnit, p.CtaText, p.DisplayOrder);

    private ServiceItemDto MapService(CardService p, (int CardId, string CardSlug, int UserId) ctx) =>
        new(p.Id, ctx.CardSlug, ctx.UserId, p.ProductName, p.ProductCategory, p.CategorySource, p.ProductDescription, p.ProductImage,
            WebsiteAssetUrls.PublicUrl(_app, WebsiteAssetUrls.ProductServicesFolder, p.ProductImage),
            p.DisplayOrder);

    private GalleryItemDto MapGallery(CardGalleryImage g, (int CardId, string CardSlug, int UserId) ctx) =>
        new(g.Id, ctx.CardSlug, ctx.UserId, g.GalleryImage,
            WebsiteAssetUrls.PublicUrl(_app, WebsiteAssetUrls.ImageGalleryFolder, g.GalleryImage),
            g.DisplayOrder);

    private SpecialOfferDto MapOffer(CardOffer o, (int CardId, string CardSlug, int UserId) ctx) =>
        new(o.Id, ctx.CardSlug, ctx.UserId, o.OfferTitle, o.OfferDescription, o.OfferImage,
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
