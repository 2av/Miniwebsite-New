using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniWebsite.Application.Admin.FranchiseDistributors;
using MiniWebsite.Application.Admin.FranchiseDistributors.Dtos;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Shared.Constants;

namespace MiniWebsite.Api.Controllers.V1;

[ApiController]
[Route(ApiConstants.ApiRoutePrefix + "/admin/franchise-distributors")]
[AllowAnonymous]
public class AdminFranchiseDistributorsController : ControllerBase
{
    private readonly IAdminFranchiseDistributorsService _service;

    public AdminFranchiseDistributorsController(IAdminFranchiseDistributorsService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResult<FranchiseDistributorPageDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var result = await _service.ListAsync(new FranchiseDistributorQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search
        }, ct);
        return Ok(result);
    }

    [HttpPatch("influencer")]
    public async Task<ActionResult<ApiResult>> Influencer(
        [FromBody] SetInfluencerRequest request,
        CancellationToken ct)
    {
        var result = await _service.SetInfluencerAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
