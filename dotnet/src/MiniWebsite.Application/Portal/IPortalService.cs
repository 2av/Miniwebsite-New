using MiniWebsite.Application.Admin.Invoices;
using MiniWebsite.Application.Admin.KitManagement.Dtos;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Application.Portal.Dtos;

namespace MiniWebsite.Application.Portal;

public interface IPortalService
{
    Task<ApiResult<PortalMeDto>> GetMeAsync(string email, CancellationToken ct = default);
    Task<ApiResult<ReferralPageDto>> GetReferralAsync(string email, CancellationToken ct = default);
    Task<ApiResult> SaveBankDetailsAsync(SaveBankDetailsRequest request, CancellationToken ct = default);
    Task<ApiResult<IReadOnlyList<InvoiceListItemDto>>> ListInvoicesForCardAsync(string email, int cardId, CancellationToken ct = default);
    Task<ApiResult<InvoiceDetailDto>> GetInvoiceAsync(string email, int invoiceId, CancellationToken ct = default);
    Task<ApiResult<InvoiceHtmlResult>> GetInvoiceHtmlAsync(string email, int invoiceId, CancellationToken ct = default);
    Task<ApiResult<InvoiceHtmlResult>> GetFranchiseInvoiceHtmlAsync(string email, CancellationToken ct = default);
    Task<ApiResult<KitExplorerDto>> GetKitExplorerAsync(string category, int folderId, CancellationToken ct = default);
    Task<ApiResult<VerificationStatusDto>> GetVerificationAsync(string email, CancellationToken ct = default);
    Task<ApiResult<VerificationStatusDto>> SubmitVerificationAsync(SubmitVerificationRequest request, CancellationToken ct = default);
    Task<ApiResult<IdCardProfileDto>> GetIdCardAsync(string email, CancellationToken ct = default);
    Task<ApiResult<ProfileImageUploadDto>> UploadProfileImageAsync(
        string email,
        Stream fileStream,
        string originalFileName,
        string contentType,
        long contentLength,
        CancellationToken ct = default);
    Task<ApiResult<CustomerTrackerPageDto>> ListCustomersAsync(string ownerEmail, string? search = null, CancellationToken ct = default);
    Task<ApiResult<CustomerManagerRowDto>> SaveCustomerAsync(SaveCustomerRequest request, CancellationToken ct = default);
    Task<ApiResult> DeleteCustomerAsync(string ownerEmail, int id, CancellationToken ct = default);
    Task<ApiResult<IReadOnlyList<CustomerFollowupRowDto>>> ListFollowupsAsync(string ownerEmail, int customerId, CancellationToken ct = default);
    Task<ApiResult> AddFollowupAsync(AddFollowupRequest request, CancellationToken ct = default);
    Task<ApiResult> RecordLastSharedAsync(string ownerEmail, int customerId, CancellationToken ct = default);
}
