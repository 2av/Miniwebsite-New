using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniWebsite.Application.Admin.WalletRecharge;
using MiniWebsite.Application.Admin.WalletRecharge.Dtos;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Shared.Constants;

namespace MiniWebsite.Api.Controllers.V1;

[ApiController]
[Route(ApiConstants.ApiRoutePrefix + "/admin/wallet-recharge")]
[AllowAnonymous]
public class AdminWalletRechargeController : ControllerBase
{
    private readonly IAdminWalletRechargeService _service;

    public AdminWalletRechargeController(IAdminWalletRechargeService service)
    {
        _service = service;
    }

    [HttpGet("lookup")]
    public async Task<ActionResult<ApiResult<FranchiseeWalletLookupDto>>> Lookup(
        [FromQuery] string? email = null,
        [FromQuery] int? userId = null,
        CancellationToken ct = default)
    {
        var result = await _service.LookupAsync(email, userId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResult<WalletRechargeResultDto>>> Recharge(
        [FromBody] WalletRechargeRequest request,
        CancellationToken ct = default)
    {
        var result = await _service.RechargeAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
