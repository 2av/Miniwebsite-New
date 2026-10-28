using MiniWebsite.Application.Admin.Files.Dtos;
using MiniWebsite.Application.Common.Models;

namespace MiniWebsite.Application.Admin.Files;

public interface IAdminFilesService
{
    Task<ApiResult<AdminFilesPageDto>> ListAsync(AdminFilesQuery query, CancellationToken cancellationToken = default);
}
