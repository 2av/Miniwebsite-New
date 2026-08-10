using MiniWebsite.Application.Admin.GrowWithMw.Dtos;
using MiniWebsite.Application.Common.Models;

namespace MiniWebsite.Application.Admin.GrowWithMw;

public interface IAdminGrowWithMwService
{
    Task<GrowWithMwMetaDto> GetMetaAsync(CancellationToken ct = default);

    Task<ApiResult<DocPagesPageDto>> ListPagesAsync(DocPagesQuery query, CancellationToken ct = default);
    Task<ApiResult<DocPageDetailDto>> GetPageAsync(int id, CancellationToken ct = default);
    Task<ApiResult<DocPageDetailDto>> UpsertPageAsync(int? id, UpsertDocPageRequest request, CancellationToken ct = default);
    Task<ApiResult> DeletePageAsync(int id, CancellationToken ct = default);
    Task<ApiResult> ReorderPagesAsync(ReorderPagesRequest request, CancellationToken ct = default);

    Task<ApiResult<IReadOnlyList<DocSectionDto>>> ListSectionsAsync(CancellationToken ct = default);
    Task<ApiResult<DocSectionDto>> CreateSectionAsync(UpsertDocSectionRequest request, CancellationToken ct = default);
    Task<ApiResult<DocSectionDto>> UpdateSectionAsync(int id, UpsertDocSectionRequest request, CancellationToken ct = default);
    Task<ApiResult> DeleteSectionAsync(int id, CancellationToken ct = default);
    Task<ApiResult> ReorderSectionsAsync(ReorderRequest request, CancellationToken ct = default);

    Task<ApiResult<DocMediaListDto>> ListMediaAsync(CancellationToken ct = default);
    Task<ApiResult<DocUploadResultDto>> UploadMediaAsync(Stream file, string fileName, string contentType, string? uploadedBy, CancellationToken ct = default);
}
