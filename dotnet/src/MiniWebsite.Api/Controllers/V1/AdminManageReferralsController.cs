using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniWebsite.Application.Admin.ManageReferrals;
using MiniWebsite.Application.Admin.ManageReferrals.Dtos;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Shared.Constants;

namespace MiniWebsite.Api.Controllers.V1;

[ApiController]
[Route(ApiConstants.ApiRoutePrefix + "/admin/manage-referrals")]
[AllowAnonymous]
public class AdminManageReferralsController : ControllerBase
{
    private readonly IAdminManageReferralsService _service;

    public AdminManageReferralsController(IAdminManageReferralsService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResult<ManageReferralsPageDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var result = await _service.ListAsync(new ManageReferralsQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search
        }, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("referrer-details")]
    public async Task<ActionResult<ApiResult<ReferrerPaymentDetailsDto>>> ReferrerDetails(
        [FromQuery] string referrerEmail,
        CancellationToken ct = default)
    {
        var result = await _service.GetReferrerDetailsAsync(referrerEmail, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{referralId:int}/payment-history")]
    public async Task<ActionResult<ApiResult<ReferralPaymentHistoryDto>>> PaymentHistory(
        int referralId,
        CancellationToken ct = default)
    {
        var result = await _service.GetPaymentHistoryAsync(referralId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("payments")]
    public async Task<ActionResult<ApiResult>> ProcessPayment(
        [FromBody] ProcessReferralPaymentRequest request,
        CancellationToken ct = default)
    {
        var result = await _service.ProcessPaymentAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("bank-details")]
    public async Task<ActionResult<ApiResult<ManageReferralBankDetailsDto>>> BankDetails(
        [FromQuery] string userEmail,
        CancellationToken ct = default)
    {
        var result = await _service.GetBankDetailsAsync(userEmail, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
