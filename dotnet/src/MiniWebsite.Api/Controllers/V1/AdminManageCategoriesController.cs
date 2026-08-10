using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniWebsite.Application.Admin.ManageCategories;
using MiniWebsite.Application.Admin.ManageCategories.Dtos;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Shared.Constants;

namespace MiniWebsite.Api.Controllers.V1;

[ApiController]
[Route(ApiConstants.ApiRoutePrefix + "/admin/manage-categories")]
[AllowAnonymous]
public class AdminManageCategoriesController : ControllerBase
{
    private readonly IAdminManageCategoriesService _service;

    public AdminManageCategoriesController(IAdminManageCategoriesService service)
    {
        _service = service;
    }

    [HttpGet("meta")]
    public ActionResult<ApiResult<ManageCategoriesMetaDto>> Meta()
    {
        return Ok(ApiResult<ManageCategoriesMetaDto>.Ok(_service.GetMeta()));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResult<ManageCategoriesPageDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? active = null,
        CancellationToken ct = default)
    {
        var result = await _service.ListAsync(new ManageCategoriesQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search,
            Active = active
        }, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(CancellationToken ct = default)
    {
        var (content, fileName) = await _service.ExportCsvAsync(ct);
        return File(content, "text/csv; charset=utf-8", fileName);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResult<ManageCategoryRowDto>>> Get(int id, CancellationToken ct = default)
    {
        var result = await _service.GetAsync(id, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResult<ManageCategoryRowDto>>> Create(
        [FromBody] UpsertCategoryRequest request,
        CancellationToken ct = default)
    {
        var result = await _service.CreateAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResult<ManageCategoryRowDto>>> Update(
        int id,
        [FromBody] UpsertCategoryRequest request,
        CancellationToken ct = default)
    {
        var result = await _service.UpdateAsync(id, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id:int}/toggle-active")]
    public async Task<ActionResult<ApiResult<ManageCategoryRowDto>>> ToggleActive(int id, CancellationToken ct = default)
    {
        var result = await _service.ToggleActiveAsync(id, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResult>> Delete(int id, CancellationToken ct = default)
    {
        var result = await _service.DeleteAsync(id, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("import")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<ApiResult<CategoryImportResultDto>>> Import(
        IFormFile file,
        [FromForm] bool replaceAll = false,
        [FromForm] bool skipDuplicates = false,
        [FromForm] string? createdBy = null,
        CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResult<CategoryImportResultDto>.Fail("Please select a valid CSV file!"));

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".csv")
            return BadRequest(ApiResult<CategoryImportResultDto>.Fail("Only CSV files are allowed!"));

        await using var stream = file.OpenReadStream();
        var result = await _service.ImportCsvAsync(stream, replaceAll, skipDuplicates, createdBy, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
