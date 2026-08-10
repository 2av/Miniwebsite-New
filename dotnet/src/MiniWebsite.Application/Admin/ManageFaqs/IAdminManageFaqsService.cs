using MiniWebsite.Application.Admin.ManageFaqs.Dtos;
using MiniWebsite.Application.Common.Models;

namespace MiniWebsite.Application.Admin.ManageFaqs;

public interface IAdminManageFaqsService
{
    ManageFaqsMetaDto GetMeta();
    Task<ApiResult<ManageFaqsPageDto>> ListAsync(ManageFaqsQuery query, CancellationToken ct = default);
    Task<ApiResult<ManageFaqRowDto>> GetAsync(int id, CancellationToken ct = default);
    Task<ApiResult<ManageFaqRowDto>> CreateAsync(UpsertFaqRequest request, CancellationToken ct = default);
    Task<ApiResult<ManageFaqRowDto>> UpdateAsync(int id, UpsertFaqRequest request, CancellationToken ct = default);
    Task<ApiResult> DeleteAsync(int id, CancellationToken ct = default);
}
