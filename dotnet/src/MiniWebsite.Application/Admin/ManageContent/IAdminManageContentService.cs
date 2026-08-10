using MiniWebsite.Application.Admin.ManageContent.Dtos;
using MiniWebsite.Application.Common.Models;

namespace MiniWebsite.Application.Admin.ManageContent;

public interface IAdminManageContentService
{
    ManageContentMetaDto GetMeta();
    Task<ApiResult<ManageContentListDto>> ListAsync(CancellationToken ct = default);
    Task<ApiResult<ManageContentItemDto>> GetByTypeAsync(string contentType, CancellationToken ct = default);
    Task<ApiResult<ManageContentItemDto>> UpsertAsync(UpsertContentRequest request, CancellationToken ct = default);
}
