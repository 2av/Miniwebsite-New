using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Application.Payments;
using MiniWebsite.Application.Payments.Dtos;
using MiniWebsite.Shared.Constants;

namespace MiniWebsite.Api.Controllers.V1;

[ApiController]
[Route(ApiConstants.ApiRoutePrefix + "/payments/membership")]
[AllowAnonymous]
public class MembershipCheckoutController : ControllerBase
{
    private readonly IMembershipCheckoutService _checkout;

    public MembershipCheckoutController(IMembershipCheckoutService checkout)
    {
        _checkout = checkout;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResult<MembershipCheckoutPageDto>>> Page([FromQuery] string email, CancellationToken ct)
    {
        var result = await _checkout.GetPageAsync(email, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("checkout")]
    public async Task<ActionResult<ApiResult<DbProfileCheckoutResultDto>>> Checkout(
        [FromBody] MembershipCheckoutRequest request, CancellationToken ct)
    {
        var result = await _checkout.CheckoutAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("verify")]
    public async Task<ActionResult<ApiResult>> Verify([FromBody] MembershipVerifyRequest request, CancellationToken ct)
    {
        var result = await _checkout.VerifyAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
