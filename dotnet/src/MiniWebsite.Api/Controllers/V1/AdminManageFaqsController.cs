using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniWebsite.Application.Admin.ManageFaqs;
using MiniWebsite.Application.Admin.ManageFaqs.Dtos;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Shared.Constants;

namespace MiniWebsite.Api.Controllers.V1;

[ApiController]
[Route(ApiConstants.ApiRoutePrefix + "/admin/manage-faqs")]
[AllowAnonymous]
public class AdminManageFaqsController : ControllerBase
{
    private readonly IAdminManageFaqsService _service;

    public AdminManageFaqsController(IAdminManageFaqsService service)
    {
        _service = service;
    }

    [HttpGet("meta")]
    public ActionResult<ApiResult<ManageFaqsMetaDto>> Meta()
    {
        return Ok(ApiResult<ManageFaqsMetaDto>.Ok(_service.GetMeta()));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResult<ManageFaqsPageDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? pageType = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _service.ListAsync(new ManageFaqsQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search,
            PageType = pageType,
            Status = status
        }, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResult<ManageFaqRowDto>>> Get(int id, CancellationToken ct = default)
    {
        var result = await _service.GetAsync(id, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResult<ManageFaqRowDto>>> Create(
        [FromBody] UpsertFaqRequest request,
        CancellationToken ct = default)
    {
        var result = await _service.CreateAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResult<ManageFaqRowDto>>> Update(
        int id,
        [FromBody] UpsertFaqRequest request,
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
