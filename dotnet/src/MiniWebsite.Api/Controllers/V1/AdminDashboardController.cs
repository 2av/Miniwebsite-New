using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniWebsite.Application.Admin.Dashboard;
using MiniWebsite.Application.Admin.Dashboard.Dtos;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Shared.Constants;

namespace MiniWebsite.Api.Controllers.V1;

[ApiController]
[Route(ApiConstants.ApiRoutePrefix + "/admin/dashboard")]
[Authorize(Roles = ApiConstants.AdminRole)]
public class AdminDashboardController : ControllerBase
{
    private readonly IAdminDashboardService _dashboard;

    public AdminDashboardController(IAdminDashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResult<AdminDashboardDto>>> Get(CancellationToken ct)
    {
        var result = await _dashboard.GetAsync(ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
