using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MiniWebsite.Application.Common.Extensions;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Application.Common.Options;
using MiniWebsite.Application.Uploads;
using MiniWebsite.Application.Website.Dtos;
using MiniWebsite.Domain.Entities;
using MiniWebsite.Domain.Common;

namespace MiniWebsite.Application.Website;

public partial class WebsiteService : IWebsiteService
{
    private readonly IApplicationDbContext _db;
    private readonly IObjectStorage _storage;
    private readonly IUploadedFileTracker _files;
    private readonly AppOptions _app;

    public WebsiteService(IApplicationDbContext db, IObjectStorage storage, IUploadedFileTracker files, IOptions<AppOptions> app)
    {
        _db = db;
        _storage = storage;
        _files = files;
        _app = app.Value;
    }

    public async Task<ApiResult<BusinessNameDto>> GetBusinessNameAsync(int cardId, string ownerEmail, CancellationToken ct = default)
    {
        var card = await LoadOwnedCardAsync(cardId, ownerEmail, ct);
        if (card == null) return ApiResult<BusinessNameDto>.Fail("Card not found or access denied.");
        var meta = await _db.CardSlugHistories.AsNoTracking().FirstOrDefaultAsync(m => m.CardId == cardId, ct);
        return ApiResult<BusinessNameDto>.Ok(new BusinessNameDto(card.Id, card.DCompName, card.DDisplayName, card.CardId, card.DNameChangeCount, meta?.PreviousSlug));
    }

    public async Task<ApiResult<CheckNameResponse>> CheckNameAsync(string compName, int? excludeId, CancellationToken ct = default, string? ownerEmail = null)
    {
        var cleaned = CleanCompName(compName);
        if (string.IsNullOrWhiteSpace(cleaned))
            return ApiResult<CheckNameResponse>.Ok(new CheckNameResponse(false, "", "Business name is required."));

        var slug = ToSlug(cleaned);
        var nameTaken = await _db.Cards.AnyAsync(c => c.CompanyName == cleaned && (!excludeId.HasValue || c.Id != excludeId), ct);
        if (nameTaken)
            return await OwnUnpaidOrTakenAsync(slug, ownerEmail, "Business name already exists.", ct);

        var slugTaken = await _db.Cards.AnyAsync(c => c.Slug == slug && (!excludeId.HasValue || c.Id != excludeId), ct);
        if (slugTaken)
            return await OwnUnpaidOrTakenAsync(slug, ownerEmail, "URL slug already taken.", ct);

        var prevTaken = await _db.CardSlugHistories.AnyAsync(p => p.PreviousSlug == slug && (!excludeId.HasValue || p.CardId != excludeId.Value), ct);
        if (prevTaken)
            return ApiResult<CheckNameResponse>.Ok(new CheckNameResponse(false, slug, "URL slug is reserved (previous)."));

        return ApiResult<CheckNameResponse>.Ok(new CheckNameResponse(true, slug, null));
    }

    /// <summary>The same user can continue payment for a profile that was saved but not paid.</summary>
    private async Task<ApiResult<CheckNameResponse>> OwnUnpaidOrTakenAsync(string slug, string? ownerEmail, string takenMessage, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(ownerEmail))
        {
            var email = ownerEmail.Trim().ToLowerInvariant();
            var own = await _db.Cards.AsNoTracking()
                .Where(c => c.Slug == slug && c.UserEmail != null && c.UserEmail.ToLower() == email)
                .Select(c => (int?)c.Id)
                .FirstOrDefaultAsync(ct);
            if (own is int cardId)
            {
                var paid = await _db.Invoices.AnyAsync(i =>
                    i.CardId == cardId && (i.PaymentStatus == "Success" || i.PaymentStatus == "Paid"), ct);
                if (!paid)
                    return ApiResult<CheckNameResponse>.Ok(new CheckNameResponse(true, slug, null));
            }
        }

        return ApiResult<CheckNameResponse>.Ok(new CheckNameResponse(false, slug, takenMessage));
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

    public async Task<ApiResult<CompanyDetailsDto>> UpdateAppointmentBookingAsync(int cardId, string ownerEmail, UpdateAppointmentBookingRequest request, CancellationToken ct = default)
    {
        var card = await LoadOwnedCardTrackedAsync(cardId, ownerEmail, ct);
        if (card == null) return ApiResult<CompanyDetailsDto>.Fail("Card not found or access denied.");
        card.AppointmentBookingEnabled = request.Enabled;
        await _db.SaveChangesAsync(ct);
        return ApiResult<CompanyDetailsDto>.Ok(
            MapCompany(card),
            request.Enabled ? "Appointment booking enabled." : "Appointment booking disabled.");
    }

    public async Task<ApiResult<SocialLinksDto>> GetSocialAsync(int cardId, string ownerEmail, CancellationToken ct = default)
    {
        var card = await LoadOwnedCardAsync(cardId, ownerEmail, ct);
        return card == null
            ? ApiResult<SocialLinksDto>.Fail("Card not found or access denied.")
            : ApiResult<SocialLinksDto>.Ok(new SocialLinksDto(card.DFb, card.DTwitter, card.DInstagram, card.DLinkedin, card.DYoutube, card.DPinterest, card.DWebsite2));
    }

    public async Task<ApiResult<SocialLinksDto>> UpdateSocialAsync(int cardId, string ownerEmail, UpdateSocialLinksRequest request, CancellationToken ct = default)
    {
        var card = await LoadOwnedCardTrackedAsync(cardId, ownerEmail, ct);
        if (card == null) return ApiResult<SocialLinksDto>.Fail("Card not found or access denied.");
        if (NormalizeSocialLink(request.Fb, out var fb) is { } fbError) return ApiResult<SocialLinksDto>.Fail(fbError);
        if (NormalizeSocialLink(request.Twitter, out var twitter) is { } twitterError) return ApiResult<SocialLinksDto>.Fail(twitterError);
        if (NormalizeSocialLink(request.Instagram, out var instagram) is { } instagramError) return ApiResult<SocialLinksDto>.Fail(instagramError);
        if (NormalizeSocialLink(request.Linkedin, out var linkedin) is { } linkedinError) return ApiResult<SocialLinksDto>.Fail(linkedinError);
        if (NormalizeSocialLink(request.Youtube, out var youtube) is { } youtubeError) return ApiResult<SocialLinksDto>.Fail(youtubeError);
        if (NormalizeSocialLink(request.Pinterest, out var pinterest) is { } pinterestError) return ApiResult<SocialLinksDto>.Fail(pinterestError);
        if (NormalizeSocialLink(request.Google, out var google) is { } googleError) return ApiResult<SocialLinksDto>.Fail(googleError);
        card.DFb = fb;
        card.DTwitter = twitter;
        card.DInstagram = instagram;
        card.DLinkedin = linkedin;
        card.DYoutube = youtube;
        card.DPinterest = pinterest;
        card.DWebsite2 = google;
        await _db.SaveChangesAsync(ct);
        return ApiResult<SocialLinksDto>.Ok(new SocialLinksDto(card.DFb, card.DTwitter, card.DInstagram, card.DLinkedin, card.DYoutube, card.DPinterest, card.DWebsite2), "Social links updated.");
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
        var upiError = UpiFormatError(request.Paytm) ?? UpiFormatError(request.GooglePay) ?? UpiFormatError(request.PhonePay);
        if (upiError != null) return ApiResult<PaymentDetailsDto>.Fail(upiError);

        var paytmQrError = ApplyQr(card.Id, request.PaytmQr, owner.Payment.PaytmQr, request.Paytm, out var paytmQr);
        var phoneQrError = ApplyQr(card.Id, request.PhonePayQr, owner.Payment.PhonePayQr, request.PhonePay, out var phoneQr);
        var googleQrError = ApplyQr(card.Id, request.GooglePayQr, owner.Payment.GooglePayQr, request.GooglePay, out var googleQr);
        var qrError = paytmQrError ?? phoneQrError ?? googleQrError;
        if (qrError != null) return ApiResult<PaymentDetailsDto>.Fail(qrError);
        var previousPaytmQr = owner.Payment.PaytmQr;
        var previousPhoneQr = owner.Payment.PhonePayQr;
        var previousGoogleQr = owner.Payment.GooglePayQr;
        owner.Payment.PaytmQr = paytmQr;
        owner.Payment.PhonePayQr = phoneQr;
        owner.Payment.GooglePayQr = googleQr;

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
        await DeleteReplacedQrAsync(userId, previousPaytmQr, owner.Payment.PaytmQr, ct);
        await DeleteReplacedQrAsync(userId, previousPhoneQr, owner.Payment.PhonePayQr, ct);
        await DeleteReplacedQrAsync(userId, previousGoogleQr, owner.Payment.GooglePayQr, ct);
        return ApiResult<PaymentDetailsDto>.Ok(MapPayment(owner.Payment, bank), "Payment details updated.");
    }

    public async Task<ApiResult<VideosDto>> GetVideosAsync(int cardId, string ownerEmail, CancellationToken ct = default)
    {
        var card = await LoadOwnedCardAsync(cardId, ownerEmail, ct);
        if (card == null) return ApiResult<VideosDto>.Fail("Card not found or access denied.");
        return ApiResult<VideosDto>.Ok(new VideosDto(await ReadVideoSlotsAsync(card.Id, ct)));
    }

    public async Task<ApiResult<VideosDto>> UpdateVideosAsync(int cardId, string ownerEmail, UpdateVideosRequest request, CancellationToken ct = default)
    {
        var card = await LoadOwnedCardTrackedAsync(cardId, ownerEmail, ct);
        if (card == null) return ApiResult<VideosDto>.Fail("Card not found or access denied.");
        var urls = request.Urls ?? [];
        var textError = VideoLinksError(urls);
        if (textError != null) return ApiResult<VideosDto>.Fail(textError);
        await ReplaceVideoSlotsAsync(card.Id, urls, ct);
        return ApiResult<VideosDto>.Ok(new VideosDto(await ReadVideoSlotsAsync(card.Id, ct)), "Videos updated.");
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
        var textError = ProductTextError(request, creating: true);
        if (textError != null) return ApiResult<ProductDto>.Fail(textError);
        var count = await _db.CardProducts.CountAsync(p => p.CardId == ctx.Value.CardId, ct);
        if (count >= 60) return ApiResult<ProductDto>.Fail("You can add up to 60 products or services.");
        var onRequest = IsPriceOnRequest(request.PriceType);
        var row = new CardProduct
        {
            CardId = ctx.Value.CardId,
            ProductName = request.ProductName.Trim(),
            ProductCategory = request.ProductCategory,
            CategorySource = (request.CategorySource ?? "").Trim(),
            ProductDescription = (request.ProductDescription ?? "").Trim(),
            ProductImage = request.ProductImage,
            Mrp = request.Mrp,
            SellingPrice = onRequest ? 0 : request.SellingPrice,
            PriceType = NormalizePriceType(request.PriceType),
            PricingUnit = (request.PricingUnit ?? "").Trim(),
            CtaText = (request.CtaText ?? "").Trim(),
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
        var textError = ProductTextError(request, creating: false);
        if (textError != null) return ApiResult<ProductDto>.Fail(textError);
        var onRequest = IsPriceOnRequest(request.PriceType);
        row.ProductName = request.ProductName.Trim();
        row.ProductCategory = request.ProductCategory;
        row.CategorySource = string.IsNullOrWhiteSpace(request.CategorySource) ? row.CategorySource : request.CategorySource.Trim();
        row.ProductDescription = (request.ProductDescription ?? "").Trim();
        if (request.ProductImage != null) row.ProductImage = request.ProductImage;
        row.Mrp = request.Mrp;
        row.SellingPrice = onRequest ? 0 : request.SellingPrice;
        row.PriceType = string.IsNullOrWhiteSpace(request.PriceType) ? row.PriceType : NormalizePriceType(request.PriceType);
        row.PricingUnit = (request.PricingUnit ?? "").Trim();
        row.CtaText = (request.CtaText ?? "").Trim();
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
        var imageKey = row.ProductImage;
        _db.CardProducts.Remove(row);
        await RemoveTrackedWebsiteImageAsync(ctx.Value.UserId, imageKey, ct);
        await _db.SaveChangesAsync(ct);
        await DeleteWebsiteObjectAsync(imageKey, ct);
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
        var textError = ServiceTextError(request.ProductName, request.ProductDescription);
        if (textError != null) return ApiResult<ServiceItemDto>.Fail(textError);
        var count = await _db.CardServices.CountAsync(p => p.CardId == ctx.Value.CardId, ct);
        if (count >= 10) return ApiResult<ServiceItemDto>.Fail("You can add up to 10 services.");
        var row = new CardService
        {
            CardId = ctx.Value.CardId,
            ProductName = (request.ProductName ?? "").Trim(),
            ProductCategory = request.ProductCategory,
            CategorySource = request.CategorySource ?? "system",
            ProductDescription = (request.ProductDescription ?? "").Trim(),
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
        var textError = ServiceTextError(request.ProductName, request.ProductDescription);
        if (textError != null) return ApiResult<ServiceItemDto>.Fail(textError);
        row.ProductName = (request.ProductName ?? "").Trim();
        row.ProductCategory = request.ProductCategory;
        row.CategorySource = request.CategorySource ?? row.CategorySource;
        row.ProductDescription = (request.ProductDescription ?? "").Trim();
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
        var imageKey = row.ProductImage;
        _db.CardServices.Remove(row);
        await RemoveTrackedWebsiteImageAsync(ctx.Value.UserId, imageKey, ct);
        await _db.SaveChangesAsync(ct);
        await DeleteWebsiteObjectAsync(imageKey, ct);
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
        var count = await _db.CardGallery.CountAsync(g => g.CardId == ctx.Value.CardId, ct);
        if (count >= 10) return ApiResult<GalleryItemDto>.Fail("You can add up to 10 gallery images.");
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
        var imageKey = row.GalleryImage;
        _db.CardGallery.Remove(row);
        await RemoveTrackedWebsiteImageAsync(ctx.Value.UserId, imageKey, ct);
        await _db.SaveChangesAsync(ct);
        await DeleteWebsiteObjectAsync(imageKey, ct);
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
        var textError = OfferTextError(request.OfferTitle, request.Badge, request.OfferDescription, request.StartDate, request.StartTime, request.EndDate, request.EndTime);
        if (textError != null) return ApiResult<SpecialOfferDto>.Fail(textError);
        var count = await _db.CardOffers.CountAsync(o => o.CardId == ctx.Value.CardId, ct);
        if (count >= 5) return ApiResult<SpecialOfferDto>.Fail("You can add up to 5 special offers.");
        var row = new CardOffer
        {
            CardId = ctx.Value.CardId,
            OfferTitle = (request.OfferTitle ?? "").Trim(),
            OfferDescription = (request.OfferDescription ?? "").Trim(),
            OfferImage = request.OfferImage,
            Badge = string.IsNullOrWhiteSpace(request.Badge) ? null : request.Badge.Trim(),
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
        var textError = OfferTextError(request.OfferTitle, request.Badge, request.OfferDescription, request.StartDate, request.StartTime, request.EndDate, request.EndTime);
        if (textError != null) return ApiResult<SpecialOfferDto>.Fail(textError);
        row.OfferTitle = (request.OfferTitle ?? "").Trim();
        row.OfferDescription = (request.OfferDescription ?? "").Trim();
        if (request.OfferImage != null) row.OfferImage = request.OfferImage;
        row.Badge = string.IsNullOrWhiteSpace(request.Badge) ? null : request.Badge.Trim();
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
        var imageKey = row.OfferImage;
        _db.CardOffers.Remove(row);
        await RemoveTrackedWebsiteImageAsync(ctx.Value.UserId, imageKey, ct);
        await _db.SaveChangesAsync(ct);
        await DeleteWebsiteObjectAsync(imageKey, ct);
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

    private PaymentDetailsDto MapPayment(UserPaymentDetails? pay, UserBankDetails? bank) =>
        new(pay?.Paytm, pay?.GooglePay, pay?.PhonePay, bank?.AccountNumber, bank?.IfscCode, bank?.AccountHolderName, bank?.BankName, bank?.AccountType,
            pay?.PaytmQr, pay?.GooglePayQr, pay?.PhonePayQr,
            QrUrl(pay?.PaytmQr), QrUrl(pay?.GooglePayQr), QrUrl(pay?.PhonePayQr));

    private string QrUrl(string? key) =>
        WebsiteAssetUrls.Resolve(_storage, _app, WebsiteAssetUrls.PaymentDetailsFolder, key);

    private string? ApplyQr(int cardId, string? requested, string? current, string? upi, out string? next)
    {
        next = requested == null ? current : BlankToNull(requested);
        if (next != null && !string.Equals(next, current, StringComparison.Ordinal) && !IsOwnedCardImageKey(cardId, next))
        {
            next = current;
            return "Unknown image.";
        }
        if (next != null && string.IsNullOrWhiteSpace(upi))
            return "Enter UPI ID";
        return null;
    }

    private async Task DeleteReplacedQrAsync(int userId, string? previous, string? current, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(previous) || string.Equals(previous, current, StringComparison.Ordinal)) return;
        await RemoveTrackedWebsiteImageAsync(userId, previous, ct);
        await _db.SaveChangesAsync(ct);
        await DeleteWebsiteObjectAsync(previous, ct);
    }

    private static string? UpiFormatError(string? value)
    {
        var text = (value ?? "").Trim();
        if (text.Length == 0) return null;
        if (text.Length > 200 || !UpiPattern().IsMatch(text)) return "Invalid UPI ID format";
        return null;
    }

    private static string? NormalizeSocialLink(string? value, out string? stored)
    {
        var text = (value ?? "").Trim();
        if (text.Length == 0)
        {
            stored = null;
            return null;
        }
        if (text.Any(char.IsWhiteSpace))
        {
            stored = null;
            return "Invalid link format";
        }
        var candidate = text.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || text.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? text
            : "https://" + text;
        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || string.IsNullOrWhiteSpace(uri.Host)
            || !uri.Host.Contains('.'))
        {
            stored = null;
            return "Invalid link format";
        }
        stored = candidate;
        return null;
    }

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
            WebsiteAssetUrls.Resolve(_storage, _app, WebsiteAssetUrls.CompanyDetailsFolder, c.DLogoLocation),
            WebsiteAssetUrls.Resolve(_storage, _app, WebsiteAssetUrls.CompanyDetailsFolder, c.DHeroImageLocation),
            c.BusinessProfileType ?? "", c.BusinessType ?? "", c.BusinessOperationArea ?? "",
            c.BusinessOperationLocations, c.AppointmentBookingEnabled);
    }

    private ProductDto MapProduct(CardProduct p, (int CardId, string CardSlug, int UserId) ctx) =>
        new(p.Id, ctx.CardSlug, ctx.UserId, p.ProductName, p.ProductCategory, p.CategorySource, p.ProductDescription,
            p.ProductImage,
            WebsiteAssetUrls.Resolve(_storage, _app, WebsiteAssetUrls.ProductPricingFolder, p.ProductImage),
            p.Mrp, p.SellingPrice, p.PriceType, p.PricingUnit, p.CtaText, p.DisplayOrder);

    private ServiceItemDto MapService(CardService p, (int CardId, string CardSlug, int UserId) ctx) =>
        new(p.Id, ctx.CardSlug, ctx.UserId, p.ProductName, p.ProductCategory, p.CategorySource, p.ProductDescription, p.ProductImage,
            WebsiteAssetUrls.Resolve(_storage, _app, WebsiteAssetUrls.ProductServicesFolder, p.ProductImage),
            p.DisplayOrder);

    private GalleryItemDto MapGallery(CardGalleryImage g, (int CardId, string CardSlug, int UserId) ctx) =>
        new(g.Id, ctx.CardSlug, ctx.UserId, g.GalleryImage,
            WebsiteAssetUrls.Resolve(_storage, _app, WebsiteAssetUrls.ImageGalleryFolder, g.GalleryImage),
            g.DisplayOrder);

    private SpecialOfferDto MapOffer(CardOffer o, (int CardId, string CardSlug, int UserId) ctx) =>
        new(o.Id, ctx.CardSlug, ctx.UserId, o.OfferTitle, o.OfferDescription, o.OfferImage,
            WebsiteAssetUrls.Resolve(_storage, _app, WebsiteAssetUrls.SpecialOffersFolder, o.OfferImage),
            o.Badge, o.DiscountPercentage,
            o.StartDate, o.StartTime, o.EndDate, o.EndTime, o.Status, o.DisplayOrder);

    private static bool IsPriceOnRequest(string? priceType) =>
        NormalizePriceType(priceType) == "Price on Request";

    private static string NormalizePriceType(string? priceType)
    {
        var text = (priceType ?? "").Trim().ToLowerInvariant().Replace(" ", "").Replace("_", "");
        if (text.Contains("request")) return "Price on Request";
        if (text.Contains("start")) return "Starting From";
        if (text is "fixed" or "fixedprice") return "Fixed Price";
        return (priceType ?? "").Trim();
    }

    private static string? ProductTextError(UpsertProductRequest request, bool creating)
    {
        var name = (request.ProductName ?? "").Trim();
        if (name.Length == 0) return "Title is required.";
        if (name.Length > 30) return "Title can be at most 30 characters.";
        if ((request.ProductDescription ?? "").Trim().Length > 400) return "Description can be at most 400 characters.";
        var category = (request.CategorySource ?? "").Trim();
        if (category.Equals("system", StringComparison.OrdinalIgnoreCase)) category = "";
        if (creating && category.Length == 0) return "Product/Service category is required.";
        if (category.Length > 30) return "Category can be at most 30 characters.";
        var type = NormalizePriceType(request.PriceType);
        if (type.Length > 0 && type is not ("Fixed Price" or "Starting From" or "Price on Request")) return "Select a price type.";
        if (creating && type.Length == 0) return "Select a price type.";
        if (type != "Price on Request")
        {
            if (creating && (request.SellingPrice < 0 || request.SellingPrice != decimal.Truncate(request.SellingPrice) || request.SellingPrice > 9_999_999m))
                return "Only numbers allowed";
            if (!creating && request.SellingPrice > 9_999_999m) return "Only numbers allowed";
        }
        var unit = (request.PricingUnit ?? "").Trim();
        if (creating && unit.Length == 0) return "Pricing unit is required.";
        if (unit.Length > 20) return "Pricing unit can be at most 20 characters.";
        var cta = (request.CtaText ?? "").Trim();
        if (creating && cta.Length == 0) return "Select button text.";
        if (cta.Length > 20) return "Button text can be at most 20 characters.";
        if (cta.Length > 0 && !System.Text.RegularExpressions.Regex.IsMatch(cta, "^[A-Za-z0-9 &]+$"))
            return "Button text can only use letters, numbers, spaces, and &.";
        return null;
    }

    private static string? OfferTextError(
        string? title, string? badge, string? description,
        DateOnly? startDate, TimeOnly? startTime, DateOnly? endDate, TimeOnly? endTime)
    {
        var name = (title ?? "").Trim();
        if (name.Length == 0) return "Offer title is required.";
        if (name.Length > 255) return "Offer title is too long.";
        if ((badge ?? "").Trim().Length > 20) return "Offer badge can be at most 20 characters.";
        if ((description ?? "").Trim().Length > 500) return "Offer description can be at most 500 characters.";
        if (startDate is DateOnly start && endDate is DateOnly end)
        {
            var startAt = start.ToDateTime(startTime ?? new TimeOnly(0, 0));
            var endAt = end.ToDateTime(endTime ?? new TimeOnly(23, 59));
            if (endAt < startAt) return "End date and time must be after the start.";
        }
        return null;
    }

    private static string? ServiceTextError(string? name, string? description)
    {
        var serviceName = (name ?? "").Trim();
        if (serviceName.Length == 0) return "Service name is required.";
        if (serviceName.Length > 30) return "Service name can be at most 30 characters.";
        if ((description ?? "").Trim().Length > 400) return "Service description can be at most 400 characters.";
        return null;
    }

    private static string CleanCompName(string name) =>
        CompNameCleanRegex().Replace(name.Trim(), "");

    private static string ToSlug(string cleaned) =>
        cleaned.Replace(' ', '-').Replace(".", "").Replace("&", "").Replace("/", "-").Replace("[", "").Replace("]", "");

    private async Task<IReadOnlyList<string?>> ReadVideoSlotsAsync(int cardId, CancellationToken ct)
    {
        var rows = await _db.CardVideos.AsNoTracking()
            .Where(v => v.CardId == cardId)
            .OrderBy(v => v.DisplayOrder)
            .ToListAsync(ct);
        var slots = new string?[20];
        foreach (var row in rows)
        {
            if (row.DisplayOrder is < 0 or > 19 || string.IsNullOrWhiteSpace(row.Url)) continue;
            slots[row.DisplayOrder] = row.Url.Trim();
        }
        return slots;
    }

    private async Task ReplaceVideoSlotsAsync(int cardId, IReadOnlyList<string?> urls, CancellationToken ct)
    {
        var existing = await _db.CardVideos.Where(v => v.CardId == cardId).ToListAsync(ct);
        _db.CardVideos.RemoveRange(existing);
        var now = AppClock.Now;
        var limit = Math.Min(20, urls.Count);
        for (var i = 0; i < limit; i++)
        {
            var text = (urls[i] ?? "").Trim();
            if (text.Length == 0) continue;
            _db.CardVideos.Add(new CardVideo
            {
                CardId = cardId,
                Url = text,
                VideoType = VideoHostKind(text),
                DisplayOrder = i,
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        await _db.SaveChangesAsync(ct);
    }

    private static string? VideoLinksError(IReadOnlyList<string?> urls)
    {
        var filled = urls.Count(url => !string.IsNullOrWhiteSpace(url));
        if (filled > 20) return "You can add up to 20 video links.";
        foreach (var url in urls)
        {
            var text = (url ?? "").Trim();
            if (text.Length == 0) continue;
            if (!IsValidVideoLink(text)) return "Please enter valid video link";
        }
        return null;
    }

    private static bool IsValidVideoLink(string text)
    {
        if (text.Length > 500) return false;
        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return false;
        return !string.IsNullOrWhiteSpace(uri.Host) && uri.Host.Contains('.');
    }

    private static string VideoHostKind(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return "link";
        var host = uri.Host.ToLowerInvariant();
        if (host.Contains("youtu")) return "youtube";
        if (host.Contains("instagram") || host.Contains("instagr.am")) return "instagram";
        if (host.Contains("facebook") || host.Contains("fb.watch") || host.Equals("fb.com")) return "facebook";
        return "link";
    }

    [GeneratedRegex(@"[^A-Za-z0-9\s\-]")]
    private static partial Regex CompNameCleanRegex();

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._-]{1,255}@[A-Za-z][A-Za-z0-9]{1,63}$")]
    private static partial Regex UpiPattern();
}
