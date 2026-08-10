using MiniWebsite.Application.Admin.KitManagement.Dtos;
using MiniWebsite.Application.Common.Models;

namespace MiniWebsite.Application.Admin.KitManagement;

public interface IAdminKitManagementService
{
    Task<KitManagementMetaDto> GetMetaAsync(CancellationToken ct = default);
    Task<ApiResult<KitExplorerDto>> GetExplorerAsync(string category, int folderId, CancellationToken ct = default);

    Task<ApiResult<KitFolderTileDto>> CreateFolderAsync(CreateKitFolderRequest request, CancellationToken ct = default);
    Task<ApiResult<KitFolderTileDto>> UpdateFolderAsync(int id, UpdateKitFolderRequest request, CancellationToken ct = default);
    Task<ApiResult> DeleteFolderAsync(int id, string category, CancellationToken ct = default);

    Task<ApiResult<KitItemDto>> AddImageAsync(
        string category,
        string title,
        int? folderId,
        int displayOrder,
        Stream file,
        string fileName,
        string contentType,
        CancellationToken ct = default);

    Task<ApiResult<KitItemDto>> AddVideoUrlAsync(AddKitVideoUrlRequest request, CancellationToken ct = default);

    Task<ApiResult<KitItemDto>> AddVideoFileAsync(
        string category,
        string title,
        int? folderId,
        int displayOrder,
        Stream file,
        string fileName,
        string contentType,
        CancellationToken ct = default);

    Task<ApiResult<KitItemDto>> AddFileAsync(
        string category,
        string title,
        int? folderId,
        int displayOrder,
        Stream file,
        string fileName,
        string contentType,
        CancellationToken ct = default);

    Task<ApiResult<KitItemDto>> UpdateImageAsync(
        int id,
        UpdateKitItemMetaRequest meta,
        Stream? file,
        string? fileName,
        string? contentType,
        CancellationToken ct = default);

    Task<ApiResult<KitItemDto>> UpdateVideoAsync(int id, UpdateKitVideoRequest request, CancellationToken ct = default);

    Task<ApiResult<KitItemDto>> UpdateFileAsync(
        int id,
        UpdateKitItemMetaRequest meta,
        Stream? file,
        string? fileName,
        string? contentType,
        CancellationToken ct = default);

    Task<ApiResult> UpdateItemStatusAsync(int id, UpdateKitItemStatusRequest request, CancellationToken ct = default);
    Task<ApiResult> MoveItemAsync(int id, MoveKitItemRequest request, CancellationToken ct = default);
    Task<ApiResult> DeleteItemAsync(int id, CancellationToken ct = default);
}
