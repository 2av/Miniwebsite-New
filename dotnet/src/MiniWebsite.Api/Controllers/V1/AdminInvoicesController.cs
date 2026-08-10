using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniWebsite.Application.Admin.Invoices;
using MiniWebsite.Shared.Constants;

namespace MiniWebsite.Api.Controllers.V1;

[ApiController]
[Route(ApiConstants.ApiRoutePrefix + "/admin/invoices")]
[AllowAnonymous]
public class AdminInvoicesController : ControllerBase
{
    private readonly IAdminInvoiceAccessService _service;

    public AdminInvoicesController(IAdminInvoiceAccessService service)
    {
        _service = service;
    }

    /// <summary>Returns printable tax-invoice HTML from invoice_details (no PHP).</summary>
    [HttpGet("{invoiceId:int}/download")]
    public async Task<IActionResult> DownloadByInvoiceId(int invoiceId, CancellationToken ct)
    {
        var result = await _service.GetInvoiceHtmlByIdAsync(invoiceId, ct);
        if (!result.Success || result.Data == null)
            return BadRequest(result);

        Response.Headers.ContentDisposition = $"inline; filename=\"{result.Data.FileName}\"";
        return Content(result.Data.Html, "text/html; charset=utf-8");
    }

    /// <summary>Latest invoice for a digi_card / MW id.</summary>
    [HttpGet("by-card/{cardId:int}/download")]
    public async Task<IActionResult> DownloadByCardId(int cardId, CancellationToken ct)
    {
        var result = await _service.GetInvoiceHtmlByCardIdAsync(cardId, ct);
        if (!result.Success || result.Data == null)
            return BadRequest(result);

        Response.Headers.ContentDisposition = $"inline; filename=\"{result.Data.FileName}\"";
        return Content(result.Data.Html, "text/html; charset=utf-8");
    }
}
