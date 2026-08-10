using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniWebsite.Application.Admin.ManageCards;
using MiniWebsite.Application.Admin.ManageCards.Dtos;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Shared.Constants;

namespace MiniWebsite.Api.Controllers.V1;

[ApiController]
[Route(ApiConstants.ApiRoutePrefix + "/admin/manage-cards")]
[AllowAnonymous]
public class AdminManageCardsController : ControllerBase
{
    private readonly IAdminManageCardsService _service;

    public AdminManageCardsController(IAdminManageCardsService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResult<ManageCardsPageDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? paymentFilter = null,
        CancellationToken ct = default)
    {
        var result = await _service.ListAsync(new ManageCardsQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search,
            PaymentFilter = paymentFilter
        }, ct);
        return Ok(result);
    }

    [HttpPatch("{cardId:int}/complimentary")]
    public async Task<ActionResult<ApiResult>> Complimentary(
        int cardId,
        [FromBody] ComplimentaryRequest body,
        CancellationToken ct)
    {
        var result = await _service.SetComplimentaryAsync(cardId, body.Status, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}

public record ComplimentaryRequest(string Status);
