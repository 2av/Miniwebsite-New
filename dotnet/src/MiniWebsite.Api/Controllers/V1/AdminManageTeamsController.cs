using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniWebsite.Application.Admin.ManageTeams;
using MiniWebsite.Application.Admin.ManageTeams.Dtos;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Shared.Constants;

namespace MiniWebsite.Api.Controllers.V1;

[ApiController]
[Route(ApiConstants.ApiRoutePrefix + "/admin/manage-teams")]
[AllowAnonymous]
public class AdminManageTeamsController : ControllerBase
{
    private readonly IAdminManageTeamsService _service;

    public AdminManageTeamsController(IAdminManageTeamsService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResult<ManageTeamsPageDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _service.ListAsync(new ManageTeamsQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search,
            Status = status
        }, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResult<ManageTeamRowDto>>> Get(int id, CancellationToken ct = default)
    {
        var result = await _service.GetAsync(id, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResult<ManageTeamRowDto>>> Create(
        [FromBody] CreateTeamMemberRequest request,
        CancellationToken ct = default)
    {
        var result = await _service.CreateAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResult<ManageTeamRowDto>>> Update(
        int id,
        [FromBody] UpdateTeamMemberRequest request,
        CancellationToken ct = default)
    {
        var result = await _service.UpdateAsync(id, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id:int}/toggle-status")]
    public async Task<ActionResult<ApiResult<ManageTeamRowDto>>> ToggleStatus(
        int id,
        [FromBody] ToggleTeamStatusRequest? request,
        CancellationToken ct = default)
    {
        var result = await _service.ToggleStatusAsync(id, request ?? new ToggleTeamStatusRequest(null), ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id:int}/reset-password")]
    public async Task<ActionResult<ApiResult>> ResetPassword(
        int id,
        [FromBody] ResetTeamPasswordRequest request,
        CancellationToken ct = default)
    {
        var result = await _service.ResetPasswordAsync(id, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id:int}/referrals")]
    public async Task<ActionResult<ApiResult<TeamReferralsDto>>> Referrals(int id, CancellationToken ct = default)
    {
        var result = await _service.GetReferralsAsync(id, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id:int}/tracker")]
    public async Task<ActionResult<ApiResult<TeamTrackerDto>>> Tracker(int id, CancellationToken ct = default)
    {
        var result = await _service.GetTrackerAsync(id, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id:int}/tracker/export")]
    public async Task<IActionResult> TrackerExport(int id, CancellationToken ct = default)
    {
        var file = await _service.ExportTrackerCsvAsync(id, ct);
        if (file == null) return NotFound(ApiResult.Fail("Tracker export not available"));
        return File(file.Value.Content, "text/csv; charset=utf-8", file.Value.FileName);
    }
}
