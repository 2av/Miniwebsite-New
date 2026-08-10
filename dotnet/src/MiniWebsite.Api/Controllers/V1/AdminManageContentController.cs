using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniWebsite.Application.Admin.ManageContent;
using MiniWebsite.Application.Admin.ManageContent.Dtos;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Shared.Constants;

namespace MiniWebsite.Api.Controllers.V1;

[ApiController]
[Route(ApiConstants.ApiRoutePrefix + "/admin/manage-content")]
[AllowAnonymous]
public class AdminManageContentController : ControllerBase
{
    private readonly IAdminManageContentService _service;

    public AdminManageContentController(IAdminManageContentService service)
    {
        _service = service;
    }

    [HttpGet("meta")]
    public ActionResult<ApiResult<ManageContentMetaDto>> Meta()
    {
        return Ok(ApiResult<ManageContentMetaDto>.Ok(_service.GetMeta()));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResult<ManageContentListDto>>> List(CancellationToken ct = default)
    {
        var result = await _service.ListAsync(ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{contentType}")]
    public async Task<ActionResult<ApiResult<ManageContentItemDto>>> Get(
        string contentType,
        CancellationToken ct = default)
    {
        var result = await _service.GetByTypeAsync(contentType, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut]
    public async Task<ActionResult<ApiResult<ManageContentItemDto>>> Upsert(
        [FromBody] UpsertContentRequest request,
        CancellationToken ct = default)
    {
        var result = await _service.UpsertAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
