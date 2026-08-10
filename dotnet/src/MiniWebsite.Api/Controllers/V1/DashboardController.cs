using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Application.DigiCards;
using MiniWebsite.Application.DigiCards.Dtos;
using MiniWebsite.Shared.Constants;

namespace MiniWebsite.Api.Controllers.V1;

/// <summary>Dashboard payload matching <c>user/dashboard/index.php</c> needs.</summary>
[ApiController]
[Route(ApiConstants.ApiRoutePrefix + "/dashboard")]
[AllowAnonymous]
public class DashboardController : ControllerBase
{
    private readonly IDigiCardService _cards;

    public DashboardController(IDigiCardService cards)
    {
        _cards = cards;
    }

    /// <summary>Customer / Team dashboard (cards where user_email = email).</summary>
    [HttpGet("customer")]
    public async Task<ActionResult<ApiResult<CustomerDashboardDto>>> Customer(
        [FromQuery] string email, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(ApiResult<CustomerDashboardDto>.Fail("email is required."));

        var result = await _cards.GetCustomerDashboardAsync(email, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Franchisee dashboard (MW Created count + Manage Users list).</summary>
    [HttpGet("franchisee")]
    public async Task<ActionResult<ApiResult<FranchiseeDashboardDto>>> Franchisee(
        [FromQuery] string email, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(ApiResult<FranchiseeDashboardDto>.Fail("email is required."));

        var result = await _cards.GetFranchiseeDashboardAsync(email, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
