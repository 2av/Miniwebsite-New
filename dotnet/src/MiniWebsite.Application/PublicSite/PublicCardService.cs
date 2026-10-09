using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Application.Common.Options;
using MiniWebsite.Application.PublicSite.Dtos;
using MiniWebsite.Application.Website;
using MiniWebsite.Domain.Entities;

namespace MiniWebsite.Application.PublicSite;

public class PublicCardService : IPublicCardService
{
    private static readonly string DefaultImagePath = "assets/img/default.jpg";

    private readonly IApplicationDbContext _db;
    private readonly AppOptions _app;

    public PublicCardService(IApplicationDbContext db, IOptions<AppOptions> app)
    {
        _db = db;
        _app = app.Value;
    }

    public async Task<ApiResult<PublicCardViewerDto>> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var key = (slug ?? "").Trim();
        if (string.IsNullOrEmpty(key))
            return ApiResult<PublicCardViewerDto>.Fail("Slug is required.");

        Card? card = null;
        if (int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numericId) && numericId > 0)
        {
            card = await _db.Cards.AsNoTracking().FirstOrDefaultAsync(c => c.Id == numericId, ct);
        }
        else
        {
            card = await _db.Cards.AsNoTracking()
                .FirstOrDefaultAsync(c => c.CardId != null && c.CardId == key, ct);

            if (card == null)
            {
                var prev = await _db.CardSlugHistories.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PreviousSlug == key, ct);
                if (prev != null)
                {
                    var redirected = await _db.Cards.AsNoTracking()
                        .FirstOrDefaultAsync(c => c.Id == prev.CardId, ct);
                    if (redirected != null)
                    {
                        var label = FirstNonEmpty(redirected.DDisplayName, redirected.DCompName, "Company");
                        var newSlug = redirected.CardId ?? redirected.Id.ToString(CultureInfo.InvariantCulture);
                        var redirectUrl = BuildShareUrl(newSlug);
                        return ApiResult<PublicCardViewerDto>.Ok(new PublicCardViewerDto(
                            "redirect",
                            "ShareBizz URL updated.",
                            label,
                            newSlug,
                            redirectUrl,
                            null,
                            null));
                    }
                }
            }
        }

        if (card == null)
        {
            return ApiResult<PublicCardViewerDto>.Ok(new PublicCardViewerDto(
                "not_found",
                "This digital card is not available.",
                null, null, null, null, null));
        }

        if (IsExpired(card.ValidityDate, out var expiryDisplay))
        {
            return ApiResult<PublicCardViewerDto>.Ok(new PublicCardViewerDto(
                "expired",
                "This miniwebsite has reached its validity end date and is no longer available.",
                FirstNonEmpty(card.DCompName, card.DDisplayName),
                null, null,
                expiryDisplay,
                null));
        }

        var payload = await BuildPayloadAsync(card, ct);
        return ApiResult<PublicCardViewerDto>.Ok(new PublicCardViewerDto(
            "ok", null, null, null, null, null, payload));
    }

    private async Task<PublicCardPayloadDto> BuildPayloadAsync(Card card, CancellationToken ct)
    {
        var cardKey = card.Id.ToString(CultureInfo.InvariantCulture);
        var slug = FirstNonEmpty(card.CardId, cardKey)!;
        var themeNumber = ResolveThemeNumber(card.DCss);
        var themeCss = $"theme/css/theme{themeNumber}.css";
        var layoutCss = $"theme/css/layout{themeNumber}.css";
        var defaultUrl = SiteAsset(DefaultImagePath);

        var heroCover = WebsiteAssetUrls.PublicUrl(_app, WebsiteAssetUrls.CompanyDetailsFolder, card.DHeroImageLocation);
        if (string.IsNullOrEmpty(heroCover)) heroCover = defaultUrl;
        var logo = WebsiteAssetUrls.PublicUrl(_app, WebsiteAssetUrls.CompanyDetailsFolder, card.DLogoLocation);
        if (string.IsNullOrEmpty(logo)) logo = heroCover;

        var heroName = FirstNonEmpty(
            card.DCompName,
            $"{card.DFName} {card.DLName}".Trim(),
            "Your Name")!;

        var primaryCategory = await ResolveCategoryNameAsync(card.DPositionPrimary, ct);

        var phone = DigitsPlus(card.DContact);
        var whatsapp = DigitsPlus(card.DWhatsapp);
        var locParts = new[] { card.DAddress2, card.DCity, card.DState }
            .Select(s => (s ?? "").Trim())
            .Where(s => s.Length > 0);
        var location = string.Join(", ", locParts);

        var addrParts = new[] { card.DAddress, card.DAddress2, card.DCity, card.DState, card.DPincode }
            .Select(s => (s ?? "").Trim())
            .Where(s => s.Length > 0);
        var fullAddress = string.Join(" ", addrParts);

        var social = new List<PublicSocialLinkDto>();
        AddSocial(social, "facebook-f", card.DFb);
        AddSocial(social, "instagram", card.DInstagram);
        AddSocial(social, "linkedin-in", card.DLinkedin);
        AddSocial(social, "x-twitter", card.DTwitter);
        AddSocial(social, "youtube", card.DYoutube);
        AddSocial(social, "pinterest", card.DPinterest);

        var services = await _db.CardServices.AsNoTracking()
            .Where(s => s.CardId == card.Id || s.CardSlug == slug)
            .OrderBy(s => s.DisplayOrder).ThenBy(s => s.Id)
            .ToListAsync(ct);
        var serviceDtos = services
            .Where(s => !string.IsNullOrWhiteSpace(s.ProductName))
            .Select(s => new PublicServiceDto(
                s.ProductName!.Trim(),
                s.ProductDescription,
                FirstNonEmpty(
                    WebsiteAssetUrls.PublicUrl(_app, WebsiteAssetUrls.ProductServicesFolder, s.ProductImage),
                    defaultUrl)!))
            .ToList();

        var offers = await _db.CardOffers.AsNoTracking()
            .Where(o => (o.CardId == card.Id || o.CardSlug == slug) && (o.Status == "Active" || o.Status == "Inactive"))
            .OrderBy(o => o.DisplayOrder).ThenBy(o => o.Id)
            .ToListAsync(ct);
        var offerDtos = offers
            .Where(IsOfferVisible)
            .Select(o => new PublicOfferDto(
                o.OfferTitle,
                o.OfferDescription,
                FirstNonEmpty(
                    WebsiteAssetUrls.PublicUrl(_app, WebsiteAssetUrls.SpecialOffersFolder, o.OfferImage),
                    defaultUrl)!,
                o.Badge,
                o.DiscountPercentage,
                o.Status))
            .ToList();

        var products = await _db.CardProducts.AsNoTracking()
            .Where(p => p.CardId == card.Id || p.CardSlug == slug)
            .OrderBy(p => p.DisplayOrder).ThenBy(p => p.Id)
            .ToListAsync(ct);
        var productDtos = products
            .Where(p => !string.IsNullOrWhiteSpace(p.ProductName))
            .Select(p => new PublicProductDto(
                p.ProductName!.Trim(),
                p.ProductDescription,
                FirstNonEmpty(
                    WebsiteAssetUrls.PublicUrl(_app, WebsiteAssetUrls.ProductPricingFolder, p.ProductImage),
                    defaultUrl)!,
                p.Mrp,
                p.SellingPrice,
                p.PriceType,
                p.PricingUnit,
                p.CtaText,
                null))
            .ToList();

        var gallery = await _db.CardGallery.AsNoTracking()
            .Where(g => g.CardId == card.Id || g.CardSlug == slug)
            .OrderBy(g => g.DisplayOrder).ThenBy(g => g.Id)
            .ToListAsync(ct);
        var galleryUrls = gallery
            .Select(g => WebsiteAssetUrls.PublicUrl(_app, WebsiteAssetUrls.ImageGalleryFolder, g.GalleryImage))
            .Where(u => !string.IsNullOrEmpty(u))
            .Cast<string>()
            .ToList();

        var videoUrls = CollectVideos(card);

        var paymentQrs = await LoadPaymentQrsAsync(card, ct);

        return new PublicCardPayloadDto(
            card.Id,
            slug,
            themeNumber,
            themeCss,
            layoutCss,
            heroName,
            primaryCategory,
            heroCover,
            logo,
            defaultUrl,
            card.DAboutUs,
            phone,
            whatsapp,
            card.DEmail,
            location,
            card.DLocation,
            card.DWebsite,
            BuildShareUrl(slug),
            card.DGstNumber,
            fullAddress,
            ParseBusinessHours(card.DBusinessHours),
            social,
            serviceDtos,
            offerDtos,
            productDtos,
            galleryUrls,
            videoUrls,
            paymentQrs);
    }

    private Task<IReadOnlyList<PublicPaymentQrDto>> LoadPaymentQrsAsync(Card card, CancellationToken ct)
    {
        // Greenfield cards table has no QR image blobs (legacy digi_card columns).
        var list = new List<PublicPaymentQrDto>();
        AddQr(list, "Paytm", null, card.DPaytm);
        AddQr(list, "Google Pay", null, card.DGooglePay);
        AddQr(list, "PhonePe", null, card.DPhonePay);
        return Task.FromResult<IReadOnlyList<PublicPaymentQrDto>>(list);
    }

    private static void AddQr(List<PublicPaymentQrDto> list, string label, byte[]? blob, string? upi)
    {
        var upiTrim = (upi ?? "").Trim();
        string? dataUrl = null;
        if (blob is { Length: > 0 })
            dataUrl = "data:image/*;base64," + Convert.ToBase64String(blob);
        if (dataUrl == null && upiTrim.Length == 0) return;
        list.Add(new PublicPaymentQrDto(label, dataUrl, string.IsNullOrEmpty(upiTrim) ? null : upiTrim));
    }

    private async Task<string?> ResolveCategoryNameAsync(string? primaryId, CancellationToken ct)
    {
        if (!int.TryParse((primaryId ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) || id <= 0)
            return null;
        try
        {
            var name = await _db.ProductCategories.AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => c.BusinessCategory)
                .FirstOrDefaultAsync(ct);
            name = name?.Trim();
            return string.IsNullOrEmpty(name) ? null : name;
        }
        catch
        {
            return null;
        }
    }

    private static IReadOnlyList<string> CollectVideos(Card card)
    {
        string?[] slots =
        [
            card.DYoutube1, card.DYoutube2, card.DYoutube3, card.DYoutube4, card.DYoutube5,
            card.DYoutube6, card.DYoutube7, card.DYoutube8, card.DYoutube9, card.DYoutube10,
            card.DYoutube11, card.DYoutube12, card.DYoutube13, card.DYoutube14, card.DYoutube15,
            card.DYoutube16, card.DYoutube17, card.DYoutube18, card.DYoutube19, card.DYoutube20
        ];
        return slots
            .Select(s => (s ?? "").Trim())
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<PublicBusinessHourDto> ParseBusinessHours(string? raw)
    {
        var text = (raw ?? "").Trim();
        if (string.IsNullOrEmpty(text))
        {
            return
            [
                new PublicBusinessHourDto("Monday - Friday", "9:00 AM - 6:00 PM"),
                new PublicBusinessHourDto("Saturday", "10:00 AM - 4:00 PM"),
                new PublicBusinessHourDto("Sunday", "Closed")
            ];
        }

        try
        {
            if (text.StartsWith('[') || text.StartsWith('{'))
            {
                using var doc = JsonDocument.Parse(text);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    var list = new List<PublicBusinessHourDto>();
                    foreach (var el in doc.RootElement.EnumerateArray())
                    {
                        var days = el.TryGetProperty("days", out var d) ? d.GetString()
                            : el.TryGetProperty("day", out var d2) ? d2.GetString() : null;
                        var hours = el.TryGetProperty("hours", out var h) ? h.GetString()
                            : el.TryGetProperty("time", out var h2) ? h2.GetString() : null;
                        if (!string.IsNullOrWhiteSpace(days))
                            list.Add(new PublicBusinessHourDto(days!.Trim(), (hours ?? "").Trim()));
                    }
                    if (list.Count > 0) return list;
                }
            }
        }
        catch
        {
            // fall through to line parse
        }

        var lines = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var parsed = new List<PublicBusinessHourDto>();
        foreach (var line in lines)
        {
            var parts = line.Split(':', 2);
            if (parts.Length == 2)
                parsed.Add(new PublicBusinessHourDto(parts[0].Trim(), parts[1].Trim()));
            else
                parsed.Add(new PublicBusinessHourDto(line, ""));
        }
        return parsed.Count > 0 ? parsed : [new PublicBusinessHourDto("Hours", text)];
    }

    private static bool IsOfferVisible(CardOffer o)
    {
        var status = (o.Status ?? "").Trim();
        if (!status.Equals("Active", StringComparison.OrdinalIgnoreCase)
            && !status.Equals("Inactive", StringComparison.OrdinalIgnoreCase))
            return false;

        if (o.EndDate is null) return true;
        var end = o.EndDate.Value.ToDateTime(o.EndTime ?? new TimeOnly(23, 59, 59));
        return end >= DateTime.Now;
    }

    private static int ResolveThemeNumber(string? dCss)
    {
        var selected = (dCss ?? "").Trim();
        var theme = 1;
        var m = Regex.Match(selected, @"card_css(\d+)\.css$", RegexOptions.IgnoreCase);
        if (m.Success && int.TryParse(m.Groups[1].Value, out var cssNo) && cssNo > 1)
            theme = cssNo - 1;
        else
        {
            m = Regex.Match(selected, @"theme(\d+)\.css$", RegexOptions.IgnoreCase);
            if (m.Success && int.TryParse(m.Groups[1].Value, out var themeNo) && themeNo > 0)
                theme = themeNo;
        }
        return theme <= 0 ? 1 : theme;
    }

    private static bool IsExpired(DateTime? validity, out string? display)
    {
        display = null;
        if (validity is null) return false;
        var v = validity.Value;
        if (v.Year < 1971) return false;
        if (v >= DateTime.Now) return false;
        display = v.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
        return true;
    }

    private string BuildShareUrl(string slug)
    {
        var baseUrl = (_app.PhpSiteBaseUrl ?? "").TrimEnd('/');
        if (string.IsNullOrEmpty(baseUrl))
            return "/" + slug.TrimStart('/');
        return $"{baseUrl}/{slug.TrimStart('/')}";
    }

    private string SiteAsset(string relativePath)
    {
        var baseUrl = (_app.PhpSiteBaseUrl ?? "").TrimEnd('/');
        return $"{baseUrl}/{relativePath.TrimStart('/')}";
    }

    private static void AddSocial(List<PublicSocialLinkDto> list, string icon, string? url)
    {
        var u = (url ?? "").Trim();
        if (u.Length == 0) return;
        if (!u.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !u.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            u = "https://" + u;
        list.Add(new PublicSocialLinkDto(icon, u));
    }

    private static string DigitsPlus(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        return Regex.Replace(raw, @"[^0-9+]", "");
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var v in values)
        {
            var t = (v ?? "").Trim();
            if (t.Length > 0) return t;
        }
        return null;
    }
}
