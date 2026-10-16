using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniWebsite.Application.Admin.PlanCommissions;
using MiniWebsite.Application.Admin.PlanCommissions.Dtos;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Shared.Constants;

namespace MiniWebsite.Api.Controllers.V1;

[ApiController]
[Route(ApiConstants.ApiRoutePrefix + "/admin/plan-commissions")]
[Authorize(Roles = ApiConstants.AdminRole)]
public class AdminPlanCommissionsController : ControllerBase
{
    private readonly IAdminPlanCommissionsService _service;

    public AdminPlanCommissionsController(IAdminPlanCommissionsService service)
    {
        _service = service;
    }

    [HttpGet("meta")]
    public async Task<ActionResult<ApiResult<PlanCommissionsMetaDto>>> Meta(CancellationToken ct = default)
    {
        var result = await _service.GetMetaAsync(ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet]
    public async Task<ActionResult<ApiResult<PlanCommissionsPageDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] int? roleId = null,
        [FromQuery] int? pricingPlanId = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _service.ListAsync(new PlanCommissionsQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search,
            RoleId = roleId,
            PricingPlanId = pricingPlanId,
            Status = status
        }, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResult<PlanCommissionRowDto>>> Get(int id, CancellationToken ct = default)
    {
        var result = await _service.GetAsync(id, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResult<IReadOnlyList<PlanCommissionRowDto>>>> Create(
        [FromBody] UpsertPlanCommissionRequest request,
        CancellationToken ct = default)
    {
        var result = await _service.CreateAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResult<PlanCommissionRowDto>>> Update(
        int id,
        [FromBody] UpsertPlanCommissionRequest request,
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
