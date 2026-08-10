namespace MiniWebsite.Application.Admin.KitManagement.Dtos;

public record KitCategoryMetaDto(string Key, string Label, int ItemCount);

public record KitManagementMetaDto(
    IReadOnlyList<KitCategoryMetaDto> Categories,
    string UploadsPublicPrefix);

public record KitStatsDto(
    int Folders,
    int Images,
    int Videos,
    int Files,
    int ActiveItems,
    int TotalItems);

public record KitBreadcrumbDto(int Id, string Title);

public record KitFolderTileDto(
    int Id,
    string Title,
    int DisplayOrder,
    string Status,
    int DirectItemCount,
    int SubfolderCount);

public record KitFolderOptionDto(int Id, string Title, int Depth, int? ParentId);

public record KitItemDto(
    int Id,
    string Type,
    string Title,
    string? FilePath,
    string? FileUrl,
    string? VideoUrl,
    int DisplayOrder,
    string Status,
    int? FolderId,
    string? CreatedAt);

public record KitExplorerDto(
    string Category,
    string CategoryLabel,
    int FolderId,
    KitStatsDto Stats,
    IReadOnlyList<KitBreadcrumbDto> Breadcrumb,
    IReadOnlyList<KitFolderTileDto> Subfolders,
    IReadOnlyList<KitItemDto> Items,
    IReadOnlyList<KitFolderOptionDto> FolderOptions);

public record CreateKitFolderRequest(
    string Category,
    string Title,
    int? ParentId,
    int DisplayOrder);

public record UpdateKitFolderRequest(
    string Category,
    string Title,
    int? ParentId,
    int DisplayOrder,
    string Status);

public record AddKitVideoUrlRequest(
    string Category,
    string Title,
    string VideoUrl,
    int? FolderId,
    int DisplayOrder);

public record UpdateKitVideoRequest(
    string Category,
    string Title,
    string? VideoUrl,
    int? FolderId,
    int DisplayOrder,
    string Status);

public record UpdateKitItemMetaRequest(
    string Category,
    string Title,
    int? FolderId,
    int DisplayOrder,
    string Status);

public record UpdateKitItemStatusRequest(string Status);

public record MoveKitItemRequest(string Category, int? FolderId);
