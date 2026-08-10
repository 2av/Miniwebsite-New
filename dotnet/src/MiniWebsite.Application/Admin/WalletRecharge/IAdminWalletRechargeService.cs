using MiniWebsite.Application.Admin.WalletRecharge.Dtos;
using MiniWebsite.Application.Common.Models;

namespace MiniWebsite.Application.Admin.WalletRecharge;

public interface IAdminWalletRechargeService
{
    Task<ApiResult<FranchiseeWalletLookupDto>> LookupAsync(string? email, int? userId, CancellationToken ct = default);
    Task<ApiResult<WalletRechargeResultDto>> RechargeAsync(WalletRechargeRequest request, CancellationToken ct = default);
}
