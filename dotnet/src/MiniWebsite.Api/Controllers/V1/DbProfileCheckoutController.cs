using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Application.Payments;
using MiniWebsite.Application.Payments.Dtos;
using MiniWebsite.Shared.Constants;

namespace MiniWebsite.Api.Controllers.V1;

/// <summary>DB Profile checkout for cards the signed-in user owns.</summary>
[ApiController]
[Route(ApiConstants.ApiRoutePrefix + "/payments/db-profile")]
[AllowAnonymous]
public class DbProfileCheckoutController : ControllerBase
{
    private readonly IDbProfileCheckoutService _checkout;

    public DbProfileCheckoutController(IDbProfileCheckoutService checkout)
    {
        _checkout = checkout;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResult<DbProfileCheckoutPageDto>>> Page(
        [FromQuery] string email,
        [FromQuery] int cardId,
        CancellationToken ct)
    {
        var result = await _checkout.GetPageAsync(email, cardId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("promo")]
    public async Task<ActionResult<ApiResult<DbProfilePromoResultDto>>> Promo(
        [FromBody] DbProfilePromoRequest request,
        CancellationToken ct)
    {
        var result = await _checkout.ApplyPromoAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("checkout")]
    public async Task<ActionResult<ApiResult<DbProfileCheckoutResultDto>>> Checkout(
        [FromBody] DbProfileCheckoutRequest request,
        CancellationToken ct)
    {
        var result = await _checkout.CheckoutAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("verify")]
    public async Task<ActionResult<ApiResult>> Verify(
        [FromBody] DbProfileVerifyRequest request,
        CancellationToken ct)
    {
        var result = await _checkout.VerifyAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
