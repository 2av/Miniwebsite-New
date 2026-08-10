using MiniWebsite.Application.Admin.ManageUsers.Dtos;
using MiniWebsite.Application.Common.Models;

namespace MiniWebsite.Application.Admin.ManageUsers;

public interface IAdminManageUsersService
{
    Task<ApiResult<ManageUsersPageDto>> ListAsync(ManageUsersQuery query, CancellationToken ct = default);
    Task<ApiResult> MapDealAsync(MapDealRequest request, CancellationToken ct = default);
    Task<ApiResult> RemoveDealAsync(int mappingId, CancellationToken ct = default);
    Task<ApiResult> SetCollaborationAsync(ToggleStatusRequest request, CancellationToken ct = default);
    Task<ApiResult> SetSaleskitAsync(ToggleStatusRequest request, CancellationToken ct = default);
    Task<ApiResult> SetRefundAsync(SetRefundRequest request, CancellationToken ct = default);
    Task<ApiResult> ResetPasswordAsync(AdminResetPasswordRequest request, CancellationToken ct = default);
    Task<ApiResult<DashboardDetailsDto>> GetDashboardDetailsAsync(string userEmail, CancellationToken ct = default);
    Task<ApiResult<ReferralDetailsDto>> GetReferralDetailsAsync(string referrerEmail, CancellationToken ct = default);
    Task<ApiResult> UpsertBankDetailsAsync(UpsertBankDetailsRequest request, CancellationToken ct = default);
    Task<(byte[] Content, string FileName)> ExportCsvAsync(ManageUsersQuery query, CancellationToken ct = default);
}
