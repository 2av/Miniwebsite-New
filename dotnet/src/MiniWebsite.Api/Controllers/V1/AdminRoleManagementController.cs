using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniWebsite.Application.Admin.RoleManagement;
using MiniWebsite.Application.Admin.RoleManagement.Dtos;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Shared.Constants;

namespace MiniWebsite.Api.Controllers.V1;

[ApiController]
[Route(ApiConstants.ApiRoutePrefix + "/admin/role-management")]
[AllowAnonymous]
public class AdminRoleManagementController : ControllerBase
{
    private readonly IAdminRoleManagementService _service;

    public AdminRoleManagementController(IAdminRoleManagementService service)
    {
        _service = service;
    }

    [HttpGet("meta")]
    public ActionResult<ApiResult<ManageRolesMetaDto>> Meta()
    {
        return Ok(ApiResult<ManageRolesMetaDto>.Ok(_service.GetMeta()));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResult<ManageRolesPageDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? salesType = null,
        CancellationToken ct = default)
    {
        var result = await _service.ListAsync(new ManageRolesQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search,
            SalesType = salesType
        }, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{roleId:int}")]
    public async Task<ActionResult<ApiResult<ManageRoleRowDto>>> Get(int roleId, CancellationToken ct = default)
    {
        var result = await _service.GetAsync(roleId, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResult<ManageRoleRowDto>>> Create(
        [FromBody] UpsertRoleRequest request,
        CancellationToken ct = default)
    {
        var result = await _service.CreateAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{roleId:int}")]
    public async Task<ActionResult<ApiResult<ManageRoleRowDto>>> Update(
        int roleId,
        [FromBody] UpsertRoleRequest request,
        CancellationToken ct = default)
    {
        var result = await _service.UpdateAsync(roleId, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{roleId:int}")]
    public async Task<ActionResult<ApiResult>> Delete(int roleId, CancellationToken ct = default)
    {
        var result = await _service.DeleteAsync(roleId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
