using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniWebsite.Application.Admin.Franchisees;
using MiniWebsite.Application.Admin.Franchisees.Dtos;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Shared.Constants;

namespace MiniWebsite.Api.Controllers.V1;

[ApiController]
[Route(ApiConstants.ApiRoutePrefix + "/admin/franchisees")]
[AllowAnonymous]
public class AdminFranchiseesController : ControllerBase
{
    private readonly IAdminFranchiseesService _service;

    public AdminFranchiseesController(IAdminFranchiseesService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResult<FranchiseePageDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var result = await _service.ListAsync(new FranchiseeQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search
        }, ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResult>> Create(
        [FromBody] CreateFranchiseeRequest request,
        CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("activate")]
    public async Task<ActionResult<ApiResult>> Activate(
        [FromBody] ActivateFranchiseeRequest request,
        CancellationToken ct)
    {
        var result = await _service.ActivateAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResult<FranchiseeDashboardDto>>> Dashboard(
        [FromQuery] string email,
        CancellationToken ct)
    {
        var result = await _service.GetDashboardAsync(email, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
