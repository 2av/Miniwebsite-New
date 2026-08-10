using MiniWebsite.Application.Admin.ManageReferrals.Dtos;
using MiniWebsite.Application.Common.Models;

namespace MiniWebsite.Application.Admin.ManageReferrals;

public interface IAdminManageReferralsService
{
    Task<ApiResult<ManageReferralsPageDto>> ListAsync(ManageReferralsQuery query, CancellationToken ct = default);
    Task<ApiResult<ReferrerPaymentDetailsDto>> GetReferrerDetailsAsync(string referrerEmail, CancellationToken ct = default);
    Task<ApiResult<ReferralPaymentHistoryDto>> GetPaymentHistoryAsync(int referralId, CancellationToken ct = default);
    Task<ApiResult> ProcessPaymentAsync(ProcessReferralPaymentRequest request, CancellationToken ct = default);
    Task<ApiResult<ManageReferralBankDetailsDto>> GetBankDetailsAsync(string userEmail, CancellationToken ct = default);
}
