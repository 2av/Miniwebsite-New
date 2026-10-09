using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniWebsite.Application.Admin.SalesChannelPartners;
using MiniWebsite.Application.Admin.SalesChannelPartners.Dtos;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Shared.Constants;

namespace MiniWebsite.Api.Controllers.V1;

[ApiController]
[Route(ApiConstants.ApiRoutePrefix + "/admin/sales-channel-partners")]
[AllowAnonymous]
public class AdminSalesChannelPartnersController : ControllerBase
{
    private readonly IAdminSalesChannelPartnersService _service;

    public AdminSalesChannelPartnersController(IAdminSalesChannelPartnersService service)
    {
        _service = service;
    }

    [HttpGet("roles")]
    public async Task<ActionResult<ApiResult<IReadOnlyList<SalesChannelPartnerRoleDto>>>> Roles(CancellationToken ct = default)
    {
        var result = await _service.ListRolesAsync(ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<ApiResult<SalesChannelPartnersPageDto>>> List(
        string slug,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _service.ListAsync(slug, new SalesChannelPartnersQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search,
            Status = status
        }, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{slug}")]
    public async Task<ActionResult<ApiResult<SalesChannelPartnerRowDto>>> Create(
        string slug,
        [FromBody] CreateSalesChannelPartnerRequest request,
        CancellationToken ct = default)
    {
        var result = await _service.CreateAsync(slug, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{slug}/{id:int}")]
    public async Task<ActionResult<ApiResult<SalesChannelPartnerRowDto>>> Update(
        string slug,
        int id,
        [FromBody] UpdateSalesChannelPartnerRequest request,
        CancellationToken ct = default)
    {
        var result = await _service.UpdateAsync(slug, id, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{slug}/{id:int}/toggle-status")]
    public async Task<ActionResult<ApiResult<SalesChannelPartnerRowDto>>> ToggleStatus(
        string slug,
        int id,
        [FromBody] ToggleSalesChannelPartnerStatusRequest request,
        CancellationToken ct = default)
    {
        var result = await _service.ToggleStatusAsync(slug, id, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{slug}/{id:int}/reset-password")]
    public async Task<ActionResult<ApiResult>> ResetPassword(
        string slug,
        int id,
        [FromBody] ResetSalesChannelPartnerPasswordRequest request,
        CancellationToken ct = default)
    {
        var result = await _service.ResetPasswordAsync(slug, id, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
