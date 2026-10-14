using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniWebsite.Application.Admin.Coupons;
using MiniWebsite.Application.Admin.Coupons.Dtos;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Shared.Constants;

namespace MiniWebsite.Api.Controllers.V1;

[ApiController]
[Route(ApiConstants.ApiRoutePrefix + "/admin/coupons")]
[Authorize(Roles = ApiConstants.AdminRole)]
public class AdminCouponsController : ControllerBase
{
    private readonly IAdminCouponsService _service;

    public AdminCouponsController(IAdminCouponsService service)
    {
        _service = service;
    }

    [HttpGet("meta")]
    public async Task<ActionResult<ApiResult<CouponsMetaDto>>> Meta(CancellationToken ct = default)
    {
        var result = await _service.GetMetaAsync(ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet]
    public async Task<ActionResult<ApiResult<CouponsPageDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] int? roleId = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _service.ListAsync(new CouponsQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search,
            RoleId = roleId,
            Status = status
        }, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("generate-code")]
    public async Task<ActionResult<ApiResult<CouponCodeDto>>> GenerateCode(CancellationToken ct = default)
    {
        var result = await _service.GenerateCodeAsync(ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResult<CouponRowDto>>> Get(int id, CancellationToken ct = default)
    {
        var result = await _service.GetAsync(id, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResult<CouponRowDto>>> Create(
        [FromBody] UpsertCouponRequest request,
        CancellationToken ct = default)
    {
        var result = await _service.CreateAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResult<CouponRowDto>>> Update(
        int id,
        [FromBody] UpsertCouponRequest request,
        CancellationToken ct = default)
    {
        var result = await _service.UpdateAsync(id, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResult>> Delete(int id, CancellationToken ct = default)
    {
        var result = await _service.DeleteAsync(id, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
