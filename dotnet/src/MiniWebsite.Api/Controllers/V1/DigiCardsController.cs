using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Application.DigiCards;
using MiniWebsite.Application.DigiCards.Dtos;
using MiniWebsite.Shared.Constants;

namespace MiniWebsite.Api.Controllers.V1;

[ApiController]
[Route(ApiConstants.ApiRoutePrefix + "/digi-cards")]
[AllowAnonymous]
public class DigiCardsController : ControllerBase
{
    private readonly IDigiCardService _cards;

    public DigiCardsController(IDigiCardService cards)
    {
        _cards = cards;
    }

    /// <summary>List Mini Websites owned by a customer email (dashboard table).</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResult<IReadOnlyList<DigiCardListItemDto>>>> List(
        [FromQuery] string? userEmail = null,
        [FromQuery] string? franchiseeEmail = null,
        CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(userEmail))
        {
            var byUser = await _cards.ListByUserEmailAsync(userEmail, ct);
            return Ok(byUser);
        }

        if (!string.IsNullOrWhiteSpace(franchiseeEmail))
        {
            var byFr = await _cards.ListByFranchiseeEmailAsync(franchiseeEmail, ct);
            return Ok(byFr);
        }

        return BadRequest(ApiResult<IReadOnlyList<DigiCardListItemDto>>.Fail(
            "Provide userEmail or franchiseeEmail query parameter."));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResult<DigiCardDetailDto>>> GetById(int id, CancellationToken ct)
    {
        var result = await _cards.GetByIdAsync(id, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Resolve locked old MiniWebsite URL slug → digi_card id (from digi_card_previous_slug).</summary>
    [HttpGet("by-previous-slug/{slug}")]
    public async Task<ActionResult<ApiResult<DigiCardSlugLookupDto>>> ByPreviousSlug(string slug, CancellationToken ct)
    {
        var result = await _cards.ResolvePreviousSlugAsync(slug, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
