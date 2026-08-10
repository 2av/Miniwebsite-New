using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniWebsite.Application.Admin.GrowWithMw;
using MiniWebsite.Application.Admin.GrowWithMw.Dtos;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Shared.Constants;

namespace MiniWebsite.Api.Controllers.V1;

[ApiController]
[Route(ApiConstants.ApiRoutePrefix + "/admin/grow-with-mw")]
[AllowAnonymous]
public class AdminGrowWithMwController : ControllerBase
{
    private readonly IAdminGrowWithMwService _service;

    public AdminGrowWithMwController(IAdminGrowWithMwService service)
    {
        _service = service;
    }

    [HttpGet("meta")]
    public async Task<ActionResult<ApiResult<GrowWithMwMetaDto>>> Meta(CancellationToken ct)
    {
        var meta = await _service.GetMetaAsync(ct);
        return Ok(ApiResult<GrowWithMwMetaDto>.Ok(meta));
    }

    [HttpGet("pages")]
    public async Task<ActionResult<ApiResult<DocPagesPageDto>>> ListPages(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] int? sectionId = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _service.ListPagesAsync(new DocPagesQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search,
            SectionId = sectionId,
            Status = status
        }, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("pages/{id:int}")]
    public async Task<ActionResult<ApiResult<DocPageDetailDto>>> GetPage(int id, CancellationToken ct)
    {
        var result = await _service.GetPageAsync(id, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost("pages")]
    public async Task<ActionResult<ApiResult<DocPageDetailDto>>> CreatePage(
        [FromBody] UpsertDocPageRequest request,
        CancellationToken ct)
    {
        var result = await _service.UpsertPageAsync(null, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("pages/{id:int}")]
    public async Task<ActionResult<ApiResult<DocPageDetailDto>>> UpdatePage(
        int id,
        [FromBody] UpsertDocPageRequest request,
        CancellationToken ct)
    {
        var result = await _service.UpsertPageAsync(id, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("pages/{id:int}")]
    public async Task<ActionResult<ApiResult>> DeletePage(int id, CancellationToken ct)
    {
        var result = await _service.DeletePageAsync(id, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("pages/reorder")]
    public async Task<ActionResult<ApiResult>> ReorderPages([FromBody] ReorderPagesRequest request, CancellationToken ct)
    {
        var result = await _service.ReorderPagesAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("sections")]
    public async Task<ActionResult<ApiResult<IReadOnlyList<DocSectionDto>>>> ListSections(CancellationToken ct)
    {
        var result = await _service.ListSectionsAsync(ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("sections")]
    public async Task<ActionResult<ApiResult<DocSectionDto>>> CreateSection(
        [FromBody] UpsertDocSectionRequest request,
        CancellationToken ct)
    {
        var result = await _service.CreateSectionAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("sections/{id:int}")]
    public async Task<ActionResult<ApiResult<DocSectionDto>>> UpdateSection(
        int id,
        [FromBody] UpsertDocSectionRequest request,
        CancellationToken ct)
    {
        var result = await _service.UpdateSectionAsync(id, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("sections/{id:int}")]
    public async Task<ActionResult<ApiResult>> DeleteSection(int id, CancellationToken ct)
    {
        var result = await _service.DeleteSectionAsync(id, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("sections/reorder")]
    public async Task<ActionResult<ApiResult>> ReorderSections([FromBody] ReorderRequest request, CancellationToken ct)
    {
        var result = await _service.ReorderSectionsAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("media")]
    public async Task<ActionResult<ApiResult<DocMediaListDto>>> ListMedia(CancellationToken ct)
    {
        var result = await _service.ListMediaAsync(ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("media/upload")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<ApiResult<DocUploadResultDto>>> Upload(
        IFormFile file,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResult<DocUploadResultDto>.Fail("No file"));

        await using var stream = file.OpenReadStream();
        var result = await _service.UploadMediaAsync(
            stream,
            file.FileName,
            file.ContentType ?? "",
            "admin",
            ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
