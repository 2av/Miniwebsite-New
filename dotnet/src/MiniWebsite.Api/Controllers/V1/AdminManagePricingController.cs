using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniWebsite.Application.Admin.ManagePricing;
using MiniWebsite.Application.Admin.ManagePricing.Dtos;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Shared.Constants;

namespace MiniWebsite.Api.Controllers.V1;

[ApiController]
[Route(ApiConstants.ApiRoutePrefix + "/admin/manage-pricing")]
[AllowAnonymous]
public class AdminManagePricingController : ControllerBase
{
    private readonly IAdminManagePricingService _service;

    public AdminManagePricingController(IAdminManagePricingService service)
    {
        _service = service;
    }

    [HttpGet("meta")]
    public async Task<ActionResult<ApiResult<ManagePricingMetaDto>>> Meta(CancellationToken ct)
    {
        var result = await _service.GetMetaAsync(ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet]
    public async Task<ActionResult<ApiResult<ManagePricingPageDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? planType = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _service.ListAsync(new ManagePricingQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search,
            PlanType = planType,
            Status = status
        }, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResult<ManagePricingRowDto>>> Get(int id, CancellationToken ct = default)
    {
        var result = await _service.GetAsync(id, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResult<ManagePricingRowDto>>> Create(
        [FromBody] UpsertPricingRequest request,
        CancellationToken ct = default)
    {
        var result = await _service.CreateAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResult<ManagePricingRowDto>>> Update(
        int id,
        [FromBody] UpsertPricingRequest request,
        CancellationToken ct = default)
    {
        var result = await _service.UpdateAsync(id, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<ApiResult>> ToggleStatus(int id, CancellationToken ct = default)
    {
        var result = await _service.ToggleStatusAsync(id, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResult>> Delete(int id, CancellationToken ct = default)
    {
        var result = await _service.DeleteAsync(id, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
