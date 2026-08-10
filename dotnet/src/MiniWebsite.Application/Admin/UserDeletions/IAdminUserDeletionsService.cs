using MiniWebsite.Application.Admin.UserDeletions.Dtos;
using MiniWebsite.Application.Common.Models;

namespace MiniWebsite.Application.Admin.UserDeletions;

public interface IAdminUserDeletionsService
{
    Task<ApiResult<UserDeletionPageDto>> ListAsync(UserDeletionQuery query, CancellationToken ct = default);
    Task<ApiResult> SoftDeleteAsync(BulkUserIdsRequest request, CancellationToken ct = default);
    Task<ApiResult> RestoreAsync(BulkUserIdsRequest request, CancellationToken ct = default);
}
