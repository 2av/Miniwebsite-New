using MiniWebsite.Application.Admin.ManageCategories.Dtos;
using MiniWebsite.Application.Common.Models;

namespace MiniWebsite.Application.Admin.ManageCategories;

public interface IAdminManageCategoriesService
{
    ManageCategoriesMetaDto GetMeta();
    Task<ApiResult<ManageCategoriesPageDto>> ListAsync(ManageCategoriesQuery query, CancellationToken ct = default);
    Task<ApiResult<ManageCategoryRowDto>> GetAsync(int id, CancellationToken ct = default);
    Task<ApiResult<ManageCategoryRowDto>> CreateAsync(UpsertCategoryRequest request, CancellationToken ct = default);
    Task<ApiResult<ManageCategoryRowDto>> UpdateAsync(int id, UpsertCategoryRequest request, CancellationToken ct = default);
    Task<ApiResult<ManageCategoryRowDto>> ToggleActiveAsync(int id, CancellationToken ct = default);
    Task<ApiResult> DeleteAsync(int id, CancellationToken ct = default);
    Task<(byte[] Content, string FileName)> ExportCsvAsync(CancellationToken ct = default);
    Task<ApiResult<CategoryImportResultDto>> ImportCsvAsync(
        Stream csvStream,
        bool replaceAll,
        bool skipDuplicates,
        string? createdBy,
        CancellationToken ct = default);
}
