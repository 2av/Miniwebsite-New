using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniWebsite.Application.Admin.EmailTemplates;
using MiniWebsite.Application.Admin.EmailTemplates.Dtos;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Shared.Constants;

namespace MiniWebsite.Api.Controllers.V1;

[ApiController]
[Route(ApiConstants.ApiRoutePrefix + "/admin/email-templates")]
[AllowAnonymous]
public class AdminEmailTemplatesController : ControllerBase
{
    private readonly IAdminEmailTemplatesService _service;

    public AdminEmailTemplatesController(IAdminEmailTemplatesService service)
    {
        _service = service;
    }

    [HttpGet("meta")]
    public async Task<ActionResult<ApiResult<EmailTemplateMetaDto>>> Meta(CancellationToken ct)
    {
        var result = await _service.GetMetaAsync(ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet]
    public async Task<ActionResult<ApiResult<EmailTemplatePageDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? category = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _service.ListAsync(new EmailTemplateQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search,
            Category = category,
            Status = status,
        }, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResult<EmailTemplateRowDto>>> Get(int id, CancellationToken ct)
    {
        var result = await _service.GetAsync(id, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet("by-key/{templateKey}")]
    public async Task<ActionResult<ApiResult<EmailTemplateRowDto>>> GetByKey(string templateKey, CancellationToken ct)
    {
        var result = await _service.GetByKeyAsync(templateKey, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResult<EmailTemplateRowDto>>> Create(
        [FromBody] UpsertEmailTemplateRequest request,
        CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResult<EmailTemplateRowDto>>> Update(
        int id,
        [FromBody] UpsertEmailTemplateRequest request,
        CancellationToken ct)
    {
        var result = await _service.UpdateAsync(id, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<ApiResult>> ToggleStatus(int id, CancellationToken ct)
    {
        var result = await _service.ToggleStatusAsync(id, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResult>> Delete(int id, CancellationToken ct)
    {
        var result = await _service.DeleteAsync(id, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("preview")]
    public async Task<ActionResult<ApiResult<PreviewEmailTemplateDto>>> Preview(
        [FromBody] PreviewEmailTemplateRequest request,
        CancellationToken ct)
    {
        var result = await _service.PreviewAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
