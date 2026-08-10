using MiniWebsite.Application.Admin.FranchiseDistributors.Dtos;
using MiniWebsite.Application.Common.Models;

namespace MiniWebsite.Application.Admin.FranchiseDistributors;

public interface IAdminFranchiseDistributorsService
{
    Task<ApiResult<FranchiseDistributorPageDto>> ListAsync(FranchiseDistributorQuery query, CancellationToken ct = default);
    Task<ApiResult> SetInfluencerAsync(SetInfluencerRequest request, CancellationToken ct = default);
}
