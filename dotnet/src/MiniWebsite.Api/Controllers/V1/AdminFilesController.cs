using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniWebsite.Application.Admin.Files;
using MiniWebsite.Application.Admin.Files.Dtos;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Shared.Constants;

namespace MiniWebsite.Api.Controllers.V1;

[ApiController]
[Route(ApiConstants.ApiRoutePrefix + "/admin/files")]
[Authorize(Roles = ApiConstants.AdminRole)]
public class AdminFilesController : ControllerBase
{
    private readonly IAdminFilesService _service;

    public AdminFilesController(IAdminFilesService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResult<AdminFilesPageDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? folder = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _service.ListAsync(new AdminFilesQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search,
            Folder = folder,
            Status = status,
        }, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
