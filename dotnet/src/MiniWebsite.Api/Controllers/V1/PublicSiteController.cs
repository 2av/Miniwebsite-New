using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Application.PublicSite;
using MiniWebsite.Application.PublicSite.Dtos;
using MiniWebsite.Shared.Constants;

namespace MiniWebsite.Api.Controllers.V1;

[ApiController]
[Route(ApiConstants.ApiRoutePrefix + "/public")]
[AllowAnonymous]
public class PublicSiteController : ControllerBase
{
    private readonly IPublicSiteService _public;
    private readonly IPublicCardService _cards;

    public PublicSiteController(IPublicSiteService publicSite, IPublicCardService cards)
    {
        _public = publicSite;
        _cards = cards;
    }

    /// <summary>Public digi-card viewer payload (n.php by slug or numeric id).</summary>
    [HttpGet("cards/{slug}")]
    public async Task<ActionResult<ApiResult<PublicCardViewerDto>>> Card(string slug, CancellationToken ct)
    {
        var result = await _cards.GetBySlugAsync(slug, ct);
        if (!result.Success) return BadRequest(result);
        var status = result.Data?.Status;
        if (status == "not_found") return NotFound(result);
        return Ok(result);
    }

    [HttpGet("faqs")]
    public async Task<ActionResult<ApiResult<IReadOnlyList<PublicFaqItemDto>>>> Faqs(
        [FromQuery] string pageType, CancellationToken ct)
    {
        var result = await _public.GetFaqsAsync(pageType, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("content/{contentType}")]
    public async Task<ActionResult<ApiResult<PublicContentDto>>> Content(
        string contentType, CancellationToken ct)
    {
        var result = await _public.GetContentAsync(contentType, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("captcha")]
    public ActionResult<ApiResult<CaptchaChallengeDto>> Captcha()
    {
        return Ok(ApiResult<CaptchaChallengeDto>.Ok(_public.CreateCaptcha()));
    }

    [HttpPost("franchise-enquiry")]
    public async Task<ActionResult<ApiResult>> FranchiseEnquiry(
        [FromBody] FranchiseEnquiryRequest request, CancellationToken ct)
    {
        var result = await _public.SubmitFranchiseEnquiryAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
