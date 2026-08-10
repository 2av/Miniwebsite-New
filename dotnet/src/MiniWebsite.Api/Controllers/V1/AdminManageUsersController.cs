using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniWebsite.Application.Admin.ManageUsers;
using MiniWebsite.Application.Admin.ManageUsers.Dtos;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Shared.Constants;

namespace MiniWebsite.Api.Controllers.V1;

[ApiController]
[Route(ApiConstants.ApiRoutePrefix + "/admin/manage-users")]
[AllowAnonymous]
public class AdminManageUsersController : ControllerBase
{
    private readonly IAdminManageUsersService _service;

    public AdminManageUsersController(IAdminManageUsersService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResult<ManageUsersPageDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? statusFilter = null,
        [FromQuery] string? dealFilter = null,
        [FromQuery] string? websiteFilter = null,
        [FromQuery] string? dateFilter = null,
        CancellationToken ct = default)
    {
        var result = await _service.ListAsync(new ManageUsersQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search,
            StatusFilter = statusFilter,
            DealFilter = dealFilter,
            WebsiteFilter = websiteFilter,
            DateFilter = dateFilter
        }, ct);
        return Ok(result);
    }

    [HttpPost("deals/map")]
    public async Task<ActionResult<ApiResult>> MapDeal([FromBody] MapDealRequest request, CancellationToken ct)
    {
        var result = await _service.MapDealAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("deals/{mappingId:int}")]
    public async Task<ActionResult<ApiResult>> RemoveDeal(int mappingId, CancellationToken ct)
    {
        var result = await _service.RemoveDealAsync(mappingId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("collaboration")]
    public async Task<ActionResult<ApiResult>> Collaboration([FromBody] ToggleStatusRequest request, CancellationToken ct)
    {
        var result = await _service.SetCollaborationAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("saleskit")]
    public async Task<ActionResult<ApiResult>> Saleskit([FromBody] ToggleStatusRequest request, CancellationToken ct)
    {
        var result = await _service.SetSaleskitAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("refund")]
    public async Task<ActionResult<ApiResult>> Refund([FromBody] SetRefundRequest request, CancellationToken ct)
    {
        var result = await _service.SetRefundAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult<ApiResult>> ResetPassword([FromBody] AdminResetPasswordRequest request, CancellationToken ct)
    {
        var result = await _service.ResetPasswordAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("dashboard-details")]
    public async Task<ActionResult<ApiResult<DashboardDetailsDto>>> DashboardDetails(
        [FromQuery] string userEmail, CancellationToken ct)
    {
        var result = await _service.GetDashboardDetailsAsync(userEmail, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("referral-details")]
    public async Task<ActionResult<ApiResult<ReferralDetailsDto>>> ReferralDetails(
        [FromQuery] string referrerEmail, CancellationToken ct)
    {
        var result = await _service.GetReferralDetailsAsync(referrerEmail, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("bank-details")]
    public async Task<ActionResult<ApiResult>> BankDetails([FromBody] UpsertBankDetailsRequest request, CancellationToken ct)
    {
        var result = await _service.UpsertBankDetailsAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string? search = null,
        [FromQuery] string? statusFilter = null,
        [FromQuery] string? dealFilter = null,
        [FromQuery] string? websiteFilter = null,
        [FromQuery] string? dateFilter = null,
        CancellationToken ct = default)
    {
        var (content, fileName) = await _service.ExportCsvAsync(new ManageUsersQuery
        {
            Search = search,
            StatusFilter = statusFilter,
            DealFilter = dealFilter,
            WebsiteFilter = websiteFilter,
            DateFilter = dateFilter
        }, ct);
        return File(content, "text/csv", fileName);
    }
}
