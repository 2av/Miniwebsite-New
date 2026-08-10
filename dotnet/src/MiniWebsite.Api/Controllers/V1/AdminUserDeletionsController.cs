using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniWebsite.Application.Admin.UserDeletions;
using MiniWebsite.Application.Admin.UserDeletions.Dtos;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Shared.Constants;

namespace MiniWebsite.Api.Controllers.V1;

[ApiController]
[Route(ApiConstants.ApiRoutePrefix + "/admin/user-deletions")]
[AllowAnonymous]
public class AdminUserDeletionsController : ControllerBase
{
    private readonly IAdminUserDeletionsService _service;

    public AdminUserDeletionsController(IAdminUserDeletionsService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResult<UserDeletionPageDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? role = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _service.ListAsync(new UserDeletionQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search,
            Role = role,
            Status = status
        }, ct);
        return Ok(result);
    }

    [HttpPost("soft-delete")]
    public async Task<ActionResult<ApiResult>> SoftDelete(
        [FromBody] BulkUserIdsRequest request,
        CancellationToken ct)
    {
        var result = await _service.SoftDeleteAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("restore")]
    public async Task<ActionResult<ApiResult>> Restore(
        [FromBody] BulkUserIdsRequest request,
        CancellationToken ct)
    {
        var result = await _service.RestoreAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
