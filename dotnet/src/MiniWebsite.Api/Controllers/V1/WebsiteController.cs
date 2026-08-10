using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Application.Website;
using MiniWebsite.Application.Website.Dtos;
using MiniWebsite.Shared.Constants;

namespace MiniWebsite.Api.Controllers.V1;

/// <summary>APIs for <c>user/website/*</c> wizard pages.</summary>
[ApiController]
[Route(ApiConstants.ApiRoutePrefix + "/digi-cards")]
[AllowAnonymous]
public class WebsiteController : ControllerBase
{
    private readonly IWebsiteService _website;

    public WebsiteController(IWebsiteService website) => _website = website;

    // ---- business-name ----
    [HttpGet("check-name")]
    public async Task<ActionResult<ApiResult<CheckNameResponse>>> CheckName([FromQuery] string compName, [FromQuery] int? excludeId, CancellationToken ct)
        => Ok(await _website.CheckNameAsync(compName, excludeId, ct));

    [HttpPost]
    public async Task<ActionResult<ApiResult<BusinessNameDto>>> Create([FromBody] UpsertBusinessNameRequest request, CancellationToken ct)
    {
        var result = await _website.CreateBusinessNameAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{cardId:int}/business-name")]
    public async Task<ActionResult<ApiResult<BusinessNameDto>>> GetBusinessName(int cardId, [FromQuery] string ownerEmail, CancellationToken ct)
    {
        var result = await _website.GetBusinessNameAsync(cardId, ownerEmail, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPut("{cardId:int}/business-name")]
    public async Task<ActionResult<ApiResult<BusinessNameDto>>> UpdateBusinessName(int cardId, [FromBody] UpsertBusinessNameRequest request, CancellationToken ct)
    {
        var result = await _website.UpdateBusinessNameAsync(cardId, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // ---- theme ----
    [HttpGet("themes")]
    public async Task<ActionResult<ApiResult<IReadOnlyList<ThemeCatalogItemDto>>>> ListThemes(CancellationToken ct)
        => Ok(await _website.ListThemesAsync(ct));

    [HttpGet("{cardId:int}/theme")]
    public async Task<ActionResult<ApiResult<ThemeDto>>> GetTheme(int cardId, [FromQuery] string ownerEmail, CancellationToken ct)
        => Ok(await _website.GetThemeAsync(cardId, ownerEmail, ct));

    [HttpPut("{cardId:int}/theme")]
    public async Task<ActionResult<ApiResult<ThemeDto>>> PutTheme(int cardId, [FromQuery] string ownerEmail, [FromBody] UpdateThemeRequest request, CancellationToken ct)
    {
        var result = await _website.UpdateThemeAsync(cardId, ownerEmail, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // ---- company-details ----
    [HttpGet("{cardId:int}/company-details")]
    public async Task<ActionResult<ApiResult<CompanyDetailsDto>>> GetCompany(int cardId, [FromQuery] string ownerEmail, CancellationToken ct)
        => Ok(await _website.GetCompanyDetailsAsync(cardId, ownerEmail, ct));

    [HttpPut("{cardId:int}/company-details")]
    public async Task<ActionResult<ApiResult<CompanyDetailsDto>>> PutCompany(int cardId, [FromQuery] string ownerEmail, [FromBody] UpdateCompanyDetailsRequest request, CancellationToken ct)
    {
        var result = await _website.UpdateCompanyDetailsAsync(cardId, ownerEmail, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // ---- social-links ----
    [HttpGet("{cardId:int}/social-links")]
    public async Task<ActionResult<ApiResult<SocialLinksDto>>> GetSocial(int cardId, [FromQuery] string ownerEmail, CancellationToken ct)
        => Ok(await _website.GetSocialAsync(cardId, ownerEmail, ct));

    [HttpPut("{cardId:int}/social-links")]
    public async Task<ActionResult<ApiResult<SocialLinksDto>>> PutSocial(int cardId, [FromQuery] string ownerEmail, [FromBody] UpdateSocialLinksRequest request, CancellationToken ct)
    {
        var result = await _website.UpdateSocialAsync(cardId, ownerEmail, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // ---- payment-details ----
    [HttpGet("{cardId:int}/payment-details")]
    public async Task<ActionResult<ApiResult<PaymentDetailsDto>>> GetPayment(int cardId, [FromQuery] string ownerEmail, CancellationToken ct)
        => Ok(await _website.GetPaymentAsync(cardId, ownerEmail, ct));

    [HttpPut("{cardId:int}/payment-details")]
    public async Task<ActionResult<ApiResult<PaymentDetailsDto>>> PutPayment(int cardId, [FromQuery] string ownerEmail, [FromBody] UpdatePaymentDetailsRequest request, CancellationToken ct)
    {
        var result = await _website.UpdatePaymentAsync(cardId, ownerEmail, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // ---- videos ----
    [HttpGet("{cardId:int}/videos")]
    public async Task<ActionResult<ApiResult<VideosDto>>> GetVideos(int cardId, [FromQuery] string ownerEmail, CancellationToken ct)
        => Ok(await _website.GetVideosAsync(cardId, ownerEmail, ct));

    [HttpPut("{cardId:int}/videos")]
    public async Task<ActionResult<ApiResult<VideosDto>>> PutVideos(int cardId, [FromQuery] string ownerEmail, [FromBody] UpdateVideosRequest request, CancellationToken ct)
    {
        var result = await _website.UpdateVideosAsync(cardId, ownerEmail, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // ---- products ----
    [HttpGet("{cardId:int}/products")]
    public async Task<ActionResult<ApiResult<IReadOnlyList<ProductDto>>>> ListProducts(int cardId, [FromQuery] string ownerEmail, CancellationToken ct)
        => Ok(await _website.ListProductsAsync(cardId, ownerEmail, ct));

    [HttpPost("{cardId:int}/products")]
    public async Task<ActionResult<ApiResult<ProductDto>>> CreateProduct(int cardId, [FromQuery] string ownerEmail, [FromBody] UpsertProductRequest request, CancellationToken ct)
    {
        var result = await _website.CreateProductAsync(cardId, ownerEmail, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{cardId:int}/products/{productId:int}")]
    public async Task<ActionResult<ApiResult<ProductDto>>> UpdateProduct(int cardId, int productId, [FromQuery] string ownerEmail, [FromBody] UpsertProductRequest request, CancellationToken ct)
    {
        var result = await _website.UpdateProductAsync(cardId, ownerEmail, productId, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{cardId:int}/products/{productId:int}")]
    public async Task<ActionResult<ApiResult>> DeleteProduct(int cardId, int productId, [FromQuery] string ownerEmail, CancellationToken ct)
    {
        var result = await _website.DeleteProductAsync(cardId, ownerEmail, productId, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    // ---- services ----
    [HttpGet("{cardId:int}/services")]
    public async Task<ActionResult<ApiResult<IReadOnlyList<ServiceItemDto>>>> ListServices(int cardId, [FromQuery] string ownerEmail, CancellationToken ct)
        => Ok(await _website.ListServicesAsync(cardId, ownerEmail, ct));

    [HttpPost("{cardId:int}/services")]
    public async Task<ActionResult<ApiResult<ServiceItemDto>>> CreateService(int cardId, [FromQuery] string ownerEmail, [FromBody] UpsertServiceRequest request, CancellationToken ct)
    {
        var result = await _website.CreateServiceAsync(cardId, ownerEmail, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{cardId:int}/services/{serviceId:int}")]
    public async Task<ActionResult<ApiResult<ServiceItemDto>>> UpdateService(int cardId, int serviceId, [FromQuery] string ownerEmail, [FromBody] UpsertServiceRequest request, CancellationToken ct)
    {
        var result = await _website.UpdateServiceAsync(cardId, ownerEmail, serviceId, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{cardId:int}/services/{serviceId:int}")]
    public async Task<ActionResult<ApiResult>> DeleteService(int cardId, int serviceId, [FromQuery] string ownerEmail, CancellationToken ct)
    {
        var result = await _website.DeleteServiceAsync(cardId, ownerEmail, serviceId, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    // ---- gallery ----
    [HttpGet("{cardId:int}/gallery")]
    public async Task<ActionResult<ApiResult<IReadOnlyList<GalleryItemDto>>>> ListGallery(int cardId, [FromQuery] string ownerEmail, CancellationToken ct)
        => Ok(await _website.ListGalleryAsync(cardId, ownerEmail, ct));

    [HttpPost("{cardId:int}/gallery")]
    public async Task<ActionResult<ApiResult<GalleryItemDto>>> CreateGallery(int cardId, [FromQuery] string ownerEmail, [FromBody] UpsertGalleryRequest request, CancellationToken ct)
    {
        var result = await _website.CreateGalleryAsync(cardId, ownerEmail, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{cardId:int}/gallery/{imageId:int}")]
    public async Task<ActionResult<ApiResult>> DeleteGallery(int cardId, int imageId, [FromQuery] string ownerEmail, CancellationToken ct)
    {
        var result = await _website.DeleteGalleryAsync(cardId, ownerEmail, imageId, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    // ---- special-offers ----
    [HttpGet("{cardId:int}/special-offers")]
    public async Task<ActionResult<ApiResult<IReadOnlyList<SpecialOfferDto>>>> ListOffers(int cardId, [FromQuery] string ownerEmail, CancellationToken ct)
        => Ok(await _website.ListOffersAsync(cardId, ownerEmail, ct));

    [HttpPost("{cardId:int}/special-offers")]
    public async Task<ActionResult<ApiResult<SpecialOfferDto>>> CreateOffer(int cardId, [FromQuery] string ownerEmail, [FromBody] UpsertSpecialOfferRequest request, CancellationToken ct)
    {
        var result = await _website.CreateOfferAsync(cardId, ownerEmail, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{cardId:int}/special-offers/{offerId:int}")]
    public async Task<ActionResult<ApiResult<SpecialOfferDto>>> UpdateOffer(int cardId, int offerId, [FromQuery] string ownerEmail, [FromBody] UpsertSpecialOfferRequest request, CancellationToken ct)
    {
        var result = await _website.UpdateOfferAsync(cardId, ownerEmail, offerId, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{cardId:int}/special-offers/{offerId:int}")]
    public async Task<ActionResult<ApiResult>> DeleteOffer(int cardId, int offerId, [FromQuery] string ownerEmail, CancellationToken ct)
    {
        var result = await _website.DeleteOfferAsync(cardId, ownerEmail, offerId, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Documents the PHP-upload → .NET-save-filename contract.
    /// No PHP changes yet — frontend/API can use this as the agreed plan.
    /// </summary>
    [HttpGet("media/upload-contract")]
    public ActionResult<object> UploadContract([FromServices] Microsoft.Extensions.Options.IOptions<MiniWebsite.Application.Common.Options.AppOptions> app)
        => Ok(new { success = true, data = WebsiteAssetUrls.DescribeUploadContract(app.Value) });
}
