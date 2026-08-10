using MiniWebsite.Application.Common.Models;

namespace MiniWebsite.Application.Admin.Invoices;

public interface IAdminInvoiceAccessService
{
    Task<ApiResult<InvoiceHtmlResult>> GetInvoiceHtmlByIdAsync(int invoiceId, CancellationToken ct = default);
    Task<ApiResult<InvoiceHtmlResult>> GetInvoiceHtmlByCardIdAsync(int cardId, CancellationToken ct = default);
}

public record InvoiceHtmlResult(string Html, string FileName);
