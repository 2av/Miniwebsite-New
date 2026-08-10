using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniWebsite.Application.Admin.KitManagement;
using MiniWebsite.Application.Admin.KitManagement.Dtos;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Shared.Constants;

namespace MiniWebsite.Api.Controllers.V1;

[ApiController]
[Route(ApiConstants.ApiRoutePrefix + "/admin/kit-management")]
[AllowAnonymous]
public class AdminKitManagementController : ControllerBase
{
    private readonly IAdminKitManagementService _service;

    public AdminKitManagementController(IAdminKitManagementService service)
    {
        _service = service;
    }

    [HttpGet("meta")]
    public async Task<ActionResult<ApiResult<KitManagementMetaDto>>> Meta(CancellationToken ct)
    {
        var meta = await _service.GetMetaAsync(ct);
        return Ok(ApiResult<KitManagementMetaDto>.Ok(meta));
    }

    [HttpGet("explorer")]
    public async Task<ActionResult<ApiResult<KitExplorerDto>>> Explorer(
        [FromQuery] string category = "sales",
        [FromQuery] int folderId = 0,
        CancellationToken ct = default)
    {
        var result = await _service.GetExplorerAsync(category, folderId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("folders")]
    public async Task<ActionResult<ApiResult<KitFolderTileDto>>> CreateFolder(
        [FromBody] CreateKitFolderRequest request,
        CancellationToken ct)
    {
        var result = await _service.CreateFolderAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("folders/{id:int}")]
    public async Task<ActionResult<ApiResult<KitFolderTileDto>>> UpdateFolder(
        int id,
        [FromBody] UpdateKitFolderRequest request,
        CancellationToken ct)
    {
        var result = await _service.UpdateFolderAsync(id, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("folders/{id:int}")]
    public async Task<ActionResult<ApiResult>> DeleteFolder(
        int id,
        [FromQuery] string category,
        CancellationToken ct)
    {
        var result = await _service.DeleteFolderAsync(id, category, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("items/image")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ApiResult<KitItemDto>>> AddImage(
        [FromForm] string category,
        [FromForm] string? title,
        [FromForm] int? folderId,
        [FromForm] int displayOrder,
        IFormFile file,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResult<KitItemDto>.Fail("Please select an image file."));

        await using var stream = file.OpenReadStream();
        var result = await _service.AddImageAsync(
            category,
            title ?? "",
            folderId,
            displayOrder,
            stream,
            file.FileName,
            file.ContentType ?? "",
            ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("items/video-url")]
    public async Task<ActionResult<ApiResult<KitItemDto>>> AddVideoUrl(
        [FromBody] AddKitVideoUrlRequest request,
        CancellationToken ct)
    {
        var result = await _service.AddVideoUrlAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("items/video-file")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<ActionResult<ApiResult<KitItemDto>>> AddVideoFile(
        [FromForm] string category,
        [FromForm] string? title,
        [FromForm] int? folderId,
        [FromForm] int displayOrder,
        IFormFile file,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResult<KitItemDto>.Fail("Please select a video file."));

        await using var stream = file.OpenReadStream();
        var result = await _service.AddVideoFileAsync(
            category,
            title ?? "",
            folderId,
            displayOrder,
            stream,
            file.FileName,
            file.ContentType ?? "",
            ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("items/file")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ApiResult<KitItemDto>>> AddFile(
        [FromForm] string category,
        [FromForm] string? title,
        [FromForm] int? folderId,
        [FromForm] int displayOrder,
        IFormFile file,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResult<KitItemDto>.Fail("Please select a file."));

        await using var stream = file.OpenReadStream();
        var result = await _service.AddFileAsync(
            category,
            title ?? "",
            folderId,
            displayOrder,
            stream,
            file.FileName,
            file.ContentType ?? "",
            ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("items/{id:int}/image")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ApiResult<KitItemDto>>> UpdateImage(
        int id,
        [FromForm] string category,
        [FromForm] string? title,
        [FromForm] int? folderId,
        [FromForm] int displayOrder,
        [FromForm] string status,
        IFormFile? file,
        CancellationToken ct)
    {
        await using var stream = file?.OpenReadStream();
        var result = await _service.UpdateImageAsync(
            id,
            new UpdateKitItemMetaRequest(category, title ?? "", folderId, displayOrder, status),
            stream,
            file?.FileName,
            file?.ContentType,
            ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("items/{id:int}/video")]
    public async Task<ActionResult<ApiResult<KitItemDto>>> UpdateVideo(
        int id,
        [FromBody] UpdateKitVideoRequest request,
        CancellationToken ct)
    {
        var result = await _service.UpdateVideoAsync(id, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("items/{id:int}/file")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ApiResult<KitItemDto>>> UpdateFile(
        int id,
        [FromForm] string category,
        [FromForm] string? title,
        [FromForm] int? folderId,
        [FromForm] int displayOrder,
        [FromForm] string status,
        IFormFile? file,
        CancellationToken ct)
    {
        await using var stream = file?.OpenReadStream();
        var result = await _service.UpdateFileAsync(
            id,
            new UpdateKitItemMetaRequest(category, title ?? "", folderId, displayOrder, status),
            stream,
            file?.FileName,
            file?.ContentType,
            ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("items/{id:int}/status")]
    public async Task<ActionResult<ApiResult>> UpdateStatus(
        int id,
        [FromBody] UpdateKitItemStatusRequest request,
        CancellationToken ct)
    {
        var result = await _service.UpdateItemStatusAsync(id, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("items/{id:int}/move")]
    public async Task<ActionResult<ApiResult>> MoveItem(
        int id,
        [FromBody] MoveKitItemRequest request,
        CancellationToken ct)
    {
        var result = await _service.MoveItemAsync(id, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("items/{id:int}")]
    public async Task<ActionResult<ApiResult>> DeleteItem(int id, CancellationToken ct)
    {
        var result = await _service.DeleteItemAsync(id, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
