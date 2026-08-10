using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniWebsite.Application.Admin.ManageDeals;
using MiniWebsite.Application.Admin.ManageDeals.Dtos;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Shared.Constants;

namespace MiniWebsite.Api.Controllers.V1;

[ApiController]
[Route(ApiConstants.ApiRoutePrefix + "/admin/manage-deals")]
[AllowAnonymous]
public class AdminManageDealsController : ControllerBase
{
    private readonly IAdminManageDealsService _service;

    public AdminManageDealsController(IAdminManageDealsService service)
    {
        _service = service;
    }

    [HttpGet("meta")]
    public ActionResult<ApiResult<ManageDealsMetaDto>> Meta()
    {
        return Ok(ApiResult<ManageDealsMetaDto>.Ok(_service.GetMeta()));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResult<ManageDealsPageDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? planType = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _service.ListAsync(new ManageDealsQuery
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
    public async Task<ActionResult<ApiResult<ManageDealRowDto>>> Get(int id, CancellationToken ct = default)
    {
        var result = await _service.GetAsync(id, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResult<ManageDealRowDto>>> Create(
        [FromBody] UpsertDealRequest request,
        CancellationToken ct = default)
    {
        var result = await _service.CreateAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResult<ManageDealRowDto>>> Update(
        int id,
        [FromBody] UpsertDealRequest request,
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
