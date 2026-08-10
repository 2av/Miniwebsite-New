using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniWebsite.Application.Admin.RoleAccessSettings;
using MiniWebsite.Application.Admin.RoleAccessSettings.Dtos;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Shared.Constants;

namespace MiniWebsite.Api.Controllers.V1;

[ApiController]
[Route(ApiConstants.ApiRoutePrefix + "/admin/role-access-settings")]
[AllowAnonymous]
public class AdminRoleAccessSettingsController : ControllerBase
{
    private readonly IAdminRoleAccessSettingsService _service;

    public AdminRoleAccessSettingsController(IAdminRoleAccessSettingsService service)
    {
        _service = service;
    }

    [HttpGet("matrix")]
    public async Task<ActionResult<ApiResult<RoleAccessMatrixDto>>> Matrix(CancellationToken ct)
    {
        var matrix = await _service.GetMatrixAsync(ct);
        return Ok(ApiResult<RoleAccessMatrixDto>.Ok(matrix));
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult<ApiResult>> Update(
        int id,
        [FromBody] UpdateRoleAccessSettingRequest request,
        CancellationToken ct)
    {
        var result = await _service.UpdateSettingAsync(id, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
