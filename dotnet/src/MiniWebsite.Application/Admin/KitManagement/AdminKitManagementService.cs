using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MiniWebsite.Application.Admin.KitManagement.Dtos;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Application.Common.Options;
using MiniWebsite.Domain.Entities;

namespace MiniWebsite.Application.Admin.KitManagement;

public class AdminKitManagementService : IAdminKitManagementService
{
    private static readonly string[] AllowedCategories = ["sales", "marketing", "franchise_sales"];

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "jpg", "jpeg", "png", "gif"
    };

    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "mp4", "webm", "mov", "avi"
    };

    private static readonly HashSet<string> FileExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "pdf", "doc", "docx", "xls", "xlsx", "ppt", "pptx", "txt", "zip", "rar",
        "mp4", "avi", "mov", "mp3", "wav"
    };

    private readonly IApplicationDbContext _db;
    private readonly AppOptions _app;

    public AdminKitManagementService(IApplicationDbContext db, IOptions<AppOptions> app)
    {
        _db = db;
        _app = app.Value;
    }

    public async Task<KitManagementMetaDto> GetMetaAsync(CancellationToken ct = default)
    {
        var counts = await _db.KitItems.AsNoTracking()
            .GroupBy(i => i.Category)
            .Select(g => new { Category = g.Key, ItemCount = g.Count() })
            .ToListAsync(ct);

        var countMap = counts.ToDictionary(
            c => NormalizeCategory(c.Category),
            c => c.ItemCount,
            StringComparer.OrdinalIgnoreCase);

        var categories = AllowedCategories
            .Select(k => new KitCategoryMetaDto(k, CategoryLabel(k), countMap.GetValueOrDefault(k)))
            .ToList();

        return new KitManagementMetaDto(categories, BuildPublicPrefix());
    }

    public async Task<ApiResult<KitExplorerDto>> GetExplorerAsync(string category, int folderId, CancellationToken ct = default)
    {
        category = NormalizeCategory(category);
        if (!IsAllowedCategory(category))
            return ApiResult<KitExplorerDto>.Fail("Invalid kit category.");

        try
        {
            var folders = await LoadFoldersAsync(category, ct);
            var foldersById = folders.ToDictionary(f => f.Id);

            if (folderId > 0 && !foldersById.ContainsKey(folderId))
                folderId = 0;

            var items = await LoadItemsAsync(category, ct);
            var childrenMap = BuildChildrenMap(folders);
            var itemsByFolder = GroupItemsByFolder(items);

            var breadcrumb = BuildBreadcrumb(folderId, foldersById);
            var subfolders = (childrenMap.GetValueOrDefault(folderId) ?? [])
                .Where(f => !string.Equals(f.Status, "inactive", StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => f.DisplayOrder)
                .ThenBy(f => f.Title, StringComparer.OrdinalIgnoreCase)
                .Select(f =>
                {
                    var fid = f.Id;
                    var totalItems = CountFolderItemsRecursive(fid, childrenMap, itemsByFolder);
                    var subCount = childrenMap.GetValueOrDefault(fid)?.Count ?? 0;
                    return new KitFolderTileDto(fid, f.Title, f.DisplayOrder, f.Status, totalItems, subCount);
                })
                .ToList();

            var currentItems = (folderId == 0
                ? items.Where(i => i.FolderId is null or 0)
                : itemsByFolder.GetValueOrDefault(folderId) ?? [])
                .Where(i => !string.Equals(i.Status, "inactive", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var folderOptions = BuildFolderOptions(childrenMap);

            var stats = new KitStatsDto(
                folders.Count,
                items.Count(i => i.Type == "image"),
                items.Count(i => i.Type == "video"),
                items.Count(i => i.Type == "file"),
                items.Count(i => i.Status == "active"),
                items.Count);

            var dto = new KitExplorerDto(
                category,
                CategoryLabel(category),
                folderId,
                stats,
                breadcrumb,
                subfolders,
                currentItems
                    .OrderBy(i => i.DisplayOrder)
                    .ThenByDescending(i => i.CreatedAt ?? "")
                    .ToList(),
                folderOptions);

            return ApiResult<KitExplorerDto>.Ok(dto);
        }
        catch (Exception ex)
        {
            return ApiResult<KitExplorerDto>.Fail("Unable to load kit explorer: " + ex.Message);
        }
    }

    public async Task<ApiResult<KitFolderTileDto>> CreateFolderAsync(CreateKitFolderRequest request, CancellationToken ct = default)
    {
        var category = NormalizeCategory(request.Category);
        if (!IsAllowedCategory(category))
            return ApiResult<KitFolderTileDto>.Fail("Invalid kit category.");

        var title = (request.Title ?? "").Trim();
        if (title.Length == 0)
            return ApiResult<KitFolderTileDto>.Fail("Please enter a folder name.");

        var parentId = await ValidateParentFolderAsync(request.ParentId, category, null, ct);
        var displayOrder = request.DisplayOrder < 0 ? 0 : request.DisplayOrder;
        var now = DateTime.UtcNow;

        try
        {
            var entity = new KitFolder
            {
                Title = title,
                Category = category,
                ParentId = parentId > 0 ? parentId : null,
                DisplayOrder = displayOrder,
                Status = "active",
                CreatedAt = now,
                UpdatedAt = now,
            };
            _db.KitFolders.Add(entity);
            await _db.SaveChangesAsync(ct);

            return ApiResult<KitFolderTileDto>.Ok(
                new KitFolderTileDto(entity.Id, title, displayOrder, "active", 0, 0),
                "Folder created successfully.");
        }
        catch (Exception ex)
        {
            return ApiResult<KitFolderTileDto>.Fail("Error creating folder: " + ex.Message);
        }
    }

    public async Task<ApiResult<KitFolderTileDto>> UpdateFolderAsync(int id, UpdateKitFolderRequest request, CancellationToken ct = default)
    {
        if (id <= 0)
            return ApiResult<KitFolderTileDto>.Fail("Invalid folder.");

        var category = NormalizeCategory(request.Category);
        if (!IsAllowedCategory(category))
            return ApiResult<KitFolderTileDto>.Fail("Invalid kit category.");

        var title = (request.Title ?? "").Trim();
        if (title.Length == 0)
            return ApiResult<KitFolderTileDto>.Fail("Please enter a folder name.");

        var status = NormalizeStatus(request.Status);
        var displayOrder = request.DisplayOrder < 0 ? 0 : request.DisplayOrder;

        var entity = await _db.KitFolders.FirstOrDefaultAsync(f => f.Id == id && f.Category == category, ct);
        if (entity == null)
            return ApiResult<KitFolderTileDto>.Fail("Folder not found.");

        var parentId = await ValidateParentFolderAsync(request.ParentId, category, id, ct);

        try
        {
            entity.Title = title;
            entity.ParentId = parentId > 0 ? parentId : null;
            entity.DisplayOrder = displayOrder;
            entity.Status = status;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            var itemCount = await _db.KitItems.CountAsync(i => i.FolderId == id, ct);
            var subCount = await _db.KitFolders.CountAsync(f => f.ParentId == id, ct);

            return ApiResult<KitFolderTileDto>.Ok(
                new KitFolderTileDto(id, title, displayOrder, status, itemCount, subCount),
                "Folder updated successfully.");
        }
        catch (Exception ex)
        {
            return ApiResult<KitFolderTileDto>.Fail("Error updating folder: " + ex.Message);
        }
    }

    public async Task<ApiResult> DeleteFolderAsync(int id, string category, CancellationToken ct = default)
    {
        if (id <= 0)
            return ApiResult.Fail("Invalid folder.");

        category = NormalizeCategory(category);
        if (!IsAllowedCategory(category))
            return ApiResult.Fail("Invalid kit category.");

        if (!await FolderExistsAsync(id, category, ct))
            return ApiResult.Fail("Folder not found.");

        try
        {
            await DeleteFolderRecursiveAsync(id, ct);
            return ApiResult.Ok("Folder deleted successfully.");
        }
        catch (Exception ex)
        {
            return ApiResult.Fail("Error deleting folder: " + ex.Message);
        }
    }

    public Task<ApiResult<KitItemDto>> AddImageAsync(
        string category,
        string title,
        int? folderId,
        int displayOrder,
        Stream file,
        string fileName,
        string contentType,
        CancellationToken ct = default)
        => AddUploadedItemAsync(
            category, "image", title, folderId, displayOrder, file, fileName, contentType,
            ImageExtensions, 10 * 1024 * 1024, "kit_image_", ct);

    public async Task<ApiResult<KitItemDto>> AddVideoUrlAsync(AddKitVideoUrlRequest request, CancellationToken ct = default)
    {
        var category = NormalizeCategory(request.Category);
        if (!IsAllowedCategory(category))
            return ApiResult<KitItemDto>.Fail("Invalid kit category.");

        var videoUrl = (request.VideoUrl ?? "").Trim();
        if (videoUrl.Length == 0)
            return ApiResult<KitItemDto>.Fail("Please enter a video URL.");

        var folderId = await ValidateFolderForCategoryAsync(request.FolderId, category, ct);
        var title = (request.Title ?? "").Trim();
        var displayOrder = request.DisplayOrder < 0 ? 0 : request.DisplayOrder;
        var now = DateTime.UtcNow;

        try
        {
            var entity = new KitItem
            {
                Type = "video",
                Title = title,
                VideoUrl = videoUrl,
                DisplayOrder = displayOrder,
                Status = "active",
                Category = category,
                FolderId = folderId > 0 ? folderId : null,
                CreatedAt = now,
                UpdatedAt = now,
            };
            _db.KitItems.Add(entity);
            await _db.SaveChangesAsync(ct);

            var item = MapItem(entity);
            return ApiResult<KitItemDto>.Ok(item, "Video link added successfully.");
        }
        catch (Exception ex)
        {
            return ApiResult<KitItemDto>.Fail("Error saving video: " + ex.Message);
        }
    }

    public Task<ApiResult<KitItemDto>> AddVideoFileAsync(
        string category,
        string title,
        int? folderId,
        int displayOrder,
        Stream file,
        string fileName,
        string contentType,
        CancellationToken ct = default)
        => AddUploadedItemAsync(
            category, "video", title, folderId, displayOrder, file, fileName, contentType,
            VideoExtensions, 50 * 1024 * 1024, "kit_video_", ct);

    public Task<ApiResult<KitItemDto>> AddFileAsync(
        string category,
        string title,
        int? folderId,
        int displayOrder,
        Stream file,
        string fileName,
        string contentType,
        CancellationToken ct = default)
        => AddUploadedItemAsync(
            category, "file", title, folderId, displayOrder, file, fileName, contentType,
            FileExtensions, 10 * 1024 * 1024, "kit_file_", ct);

    public async Task<ApiResult<KitItemDto>> UpdateImageAsync(
        int id,
        UpdateKitItemMetaRequest meta,
        Stream? file,
        string? fileName,
        string? contentType,
        CancellationToken ct = default)
    {
        var existing = await RequireItemAsync(id, "image", meta.Category, ct);
        if (existing.Error != null) return existing.Error;

        var folderId = await ValidateFolderForCategoryAsync(meta.FolderId, existing.Category!, ct);
        var title = (meta.Title ?? "").Trim();
        var displayOrder = meta.DisplayOrder < 0 ? 0 : meta.DisplayOrder;
        var status = NormalizeStatus(meta.Status);

        string? newStoredName = null;
        if (file != null && !string.IsNullOrWhiteSpace(fileName))
        {
            var upload = await SaveUploadAsync(file, fileName!, contentType ?? "", ImageExtensions, 10 * 1024 * 1024, "kit_image_", ct);
            if (!upload.Success) return ApiResult<KitItemDto>.Fail(upload.Message!);
            newStoredName = upload.StoredName;
            DeletePhysicalFile(existing.FilePath);
        }

        try
        {
            var entity = await _db.KitItems.FirstAsync(i => i.Id == id, ct);
            entity.Title = title;
            entity.DisplayOrder = displayOrder;
            entity.Status = status;
            entity.FolderId = folderId > 0 ? folderId : null;
            if (newStoredName != null) entity.FilePath = newStoredName;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return ApiResult<KitItemDto>.Ok(MapItem(entity), "Image updated successfully.");
        }
        catch (Exception ex)
        {
            if (newStoredName != null) DeletePhysicalFile(newStoredName);
            return ApiResult<KitItemDto>.Fail("Error updating image: " + ex.Message);
        }
    }

    public async Task<ApiResult<KitItemDto>> UpdateVideoAsync(int id, UpdateKitVideoRequest request, CancellationToken ct = default)
    {
        var category = NormalizeCategory(request.Category);
        var existing = await RequireItemAsync(id, "video", category, ct);
        if (existing.Error != null) return existing.Error;

        var folderId = await ValidateFolderForCategoryAsync(request.FolderId, category, ct);
        var title = (request.Title ?? "").Trim();
        var displayOrder = request.DisplayOrder < 0 ? 0 : request.DisplayOrder;
        var status = NormalizeStatus(request.Status);
        var videoUrl = (request.VideoUrl ?? "").Trim();
        var hasFile = !string.IsNullOrWhiteSpace(existing.FilePath);

        if (videoUrl.Length == 0 && !hasFile)
            return ApiResult<KitItemDto>.Fail("Please enter a video URL or keep the uploaded video file.");

        try
        {
            var entity = await _db.KitItems.FirstAsync(i => i.Id == id, ct);
            entity.Title = title;
            entity.DisplayOrder = displayOrder;
            entity.Status = status;
            entity.FolderId = folderId > 0 ? folderId : null;
            if (videoUrl.Length > 0) entity.VideoUrl = videoUrl;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return ApiResult<KitItemDto>.Ok(MapItem(entity), "Video updated successfully.");
        }
        catch (Exception ex)
        {
            return ApiResult<KitItemDto>.Fail("Error updating video: " + ex.Message);
        }
    }

    public async Task<ApiResult<KitItemDto>> UpdateFileAsync(
        int id,
        UpdateKitItemMetaRequest meta,
        Stream? file,
        string? fileName,
        string? contentType,
        CancellationToken ct = default)
    {
        var existing = await RequireItemAsync(id, "file", meta.Category, ct);
        if (existing.Error != null) return existing.Error;

        var folderId = await ValidateFolderForCategoryAsync(meta.FolderId, existing.Category!, ct);
        var title = (meta.Title ?? "").Trim();
        var displayOrder = meta.DisplayOrder < 0 ? 0 : meta.DisplayOrder;
        var status = NormalizeStatus(meta.Status);

        string? newStoredName = null;
        if (file != null && !string.IsNullOrWhiteSpace(fileName))
        {
            var upload = await SaveUploadAsync(file, fileName!, contentType ?? "", FileExtensions, 10 * 1024 * 1024, "kit_file_", ct);
            if (!upload.Success) return ApiResult<KitItemDto>.Fail(upload.Message!);
            newStoredName = upload.StoredName;
            DeletePhysicalFile(existing.FilePath);
        }

        try
        {
            var entity = await _db.KitItems.FirstAsync(i => i.Id == id, ct);
            entity.Title = title;
            entity.DisplayOrder = displayOrder;
            entity.Status = status;
            entity.FolderId = folderId > 0 ? folderId : null;
            if (newStoredName != null) entity.FilePath = newStoredName;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return ApiResult<KitItemDto>.Ok(MapItem(entity), "File updated successfully.");
        }
        catch (Exception ex)
        {
            if (newStoredName != null) DeletePhysicalFile(newStoredName);
            return ApiResult<KitItemDto>.Fail("Error updating file: " + ex.Message);
        }
    }

    public async Task<ApiResult> UpdateItemStatusAsync(int id, UpdateKitItemStatusRequest request, CancellationToken ct = default)
    {
        if (id <= 0) return ApiResult.Fail("Invalid item.");
        var status = NormalizeStatus(request.Status);

        var entity = await _db.KitItems.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (entity == null) return ApiResult.Fail("Item not found.");

        entity.Status = status;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ApiResult.Ok("Status updated successfully.");
    }

    public async Task<ApiResult> MoveItemAsync(int id, MoveKitItemRequest request, CancellationToken ct = default)
    {
        if (id <= 0) return ApiResult.Fail("Invalid item.");
        var category = NormalizeCategory(request.Category);
        if (!IsAllowedCategory(category))
            return ApiResult.Fail("Invalid kit category.");

        var folderId = await ValidateFolderForCategoryAsync(request.FolderId, category, ct);
        var entity = await _db.KitItems.FirstOrDefaultAsync(i => i.Id == id && i.Category == category, ct);
        if (entity == null) return ApiResult.Fail("Item not found.");

        entity.FolderId = folderId > 0 ? folderId : null;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var msg = folderId > 0 ? "Item moved to folder successfully." : "Item removed from folder (uncategorized).";
        return ApiResult.Ok(msg);
    }

    public async Task<ApiResult> DeleteItemAsync(int id, CancellationToken ct = default)
    {
        if (id <= 0) return ApiResult.Fail("Invalid item.");

        var entity = await _db.KitItems.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (entity == null) return ApiResult.Fail("Item not found.");

        if ((entity.Type is "image" or "file" or "video") && !string.IsNullOrWhiteSpace(entity.FilePath))
            DeletePhysicalFile(entity.FilePath);

        _db.KitItems.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return ApiResult.Ok("Item deleted successfully.");
    }

    private async Task<ApiResult<KitItemDto>> AddUploadedItemAsync(
        string category,
        string type,
        string title,
        int? folderId,
        int displayOrder,
        Stream file,
        string fileName,
        string contentType,
        HashSet<string> allowedExtensions,
        long maxBytes,
        string namePrefix,
        CancellationToken ct)
    {
        category = NormalizeCategory(category);
        if (!IsAllowedCategory(category))
            return ApiResult<KitItemDto>.Fail("Invalid kit category.");

        var validFolderId = await ValidateFolderForCategoryAsync(folderId, category, ct);
        var upload = await SaveUploadAsync(file, fileName, contentType, allowedExtensions, maxBytes, namePrefix, ct);
        if (!upload.Success)
            return ApiResult<KitItemDto>.Fail(upload.Message!);

        title = (title ?? "").Trim();
        displayOrder = displayOrder < 0 ? 0 : displayOrder;
        var now = DateTime.UtcNow;

        try
        {
            var entity = new KitItem
            {
                Type = type,
                Title = title,
                FilePath = upload.StoredName,
                DisplayOrder = displayOrder,
                Status = "active",
                Category = category,
                FolderId = validFolderId > 0 ? validFolderId : null,
                CreatedAt = now,
                UpdatedAt = now,
            };
            _db.KitItems.Add(entity);
            await _db.SaveChangesAsync(ct);

            var msg = type switch
            {
                "image" => "Image uploaded successfully.",
                "video" => "Video uploaded successfully.",
                _ => "File uploaded successfully."
            };
            return ApiResult<KitItemDto>.Ok(MapItem(entity), msg);
        }
        catch (Exception ex)
        {
            DeletePhysicalFile(upload.StoredName);
            return ApiResult<KitItemDto>.Fail("Error saving item: " + ex.Message);
        }
    }

    private async Task<(string? Category, string? FilePath, ApiResult<KitItemDto>? Error)> RequireItemAsync(
        int id,
        string expectedType,
        string category,
        CancellationToken ct)
    {
        category = NormalizeCategory(category);
        var row = await _db.KitItems.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id && i.Category == category, ct);

        if (row == null)
            return (null, null, ApiResult<KitItemDto>.Fail("Item not found."));
        if (!string.Equals(row.Type, expectedType, StringComparison.OrdinalIgnoreCase))
            return (null, null, ApiResult<KitItemDto>.Fail("Item type mismatch."));

        return (category, string.IsNullOrWhiteSpace(row.FilePath) ? null : row.FilePath, null);
    }

    private async Task DeleteFolderRecursiveAsync(int folderId, CancellationToken ct)
    {
        var children = await _db.KitFolders
            .Where(f => f.ParentId == folderId)
            .Select(f => f.Id)
            .ToListAsync(ct);

        foreach (var child in children)
            await DeleteFolderRecursiveAsync(child, ct);

        var items = await _db.KitItems.Where(i => i.FolderId == folderId).ToListAsync(ct);
        foreach (var item in items)
        {
            if (!string.IsNullOrWhiteSpace(item.FilePath))
                DeletePhysicalFile(item.FilePath);
            _db.KitItems.Remove(item);
        }

        var folder = await _db.KitFolders.FirstOrDefaultAsync(f => f.Id == folderId, ct);
        if (folder != null)
            _db.KitFolders.Remove(folder);

        await _db.SaveChangesAsync(ct);
    }

    private async Task<(bool Success, string? StoredName, string? Message)> SaveUploadAsync(
        Stream file,
        string fileName,
        string contentType,
        HashSet<string> allowedExtensions,
        long maxBytes,
        string namePrefix,
        CancellationToken ct)
    {
        await using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        if (ms.Length == 0)
            return (false, null, "Empty file.");
        if (ms.Length > maxBytes)
            return (false, null, $"File size too large. Maximum allowed size is {FormatBytes(maxBytes)}.");

        var ext = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(ext) && !string.IsNullOrWhiteSpace(contentType))
        {
            ext = contentType.ToLowerInvariant() switch
            {
                "image/jpeg" => "jpg",
                "image/png" => "png",
                "image/gif" => "gif",
                "video/mp4" => "mp4",
                "video/webm" => "webm",
                "video/quicktime" => "mov",
                "video/x-msvideo" => "avi",
                "application/pdf" => "pdf",
                _ => ext
            };
        }

        if (!allowedExtensions.Contains(ext))
            return (false, null, "Invalid file type.");

        var dir = ResolveUploadDir();
        if (string.IsNullOrWhiteSpace(dir))
            return (false, null, "Kit upload path is not configured (App:KitsUploadsFsPath).");

        try
        {
            Directory.CreateDirectory(dir);
            var stored = $"{namePrefix}{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_{Random.Shared.Next(1000, 9999)}.{ext}";
            var dest = Path.Combine(dir, stored);
            await File.WriteAllBytesAsync(dest, ms.ToArray(), ct);
            return (true, stored, null);
        }
        catch (Exception ex)
        {
            return (false, null, "Upload failed: " + ex.Message);
        }
    }

    private void DeletePhysicalFile(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return;
        var dir = ResolveUploadDir();
        if (string.IsNullOrWhiteSpace(dir)) return;
        var path = Path.Combine(dir, Path.GetFileName(fileName.Replace('\\', '/')));
        if (File.Exists(path))
        {
            try { File.Delete(path); } catch { /* best effort */ }
        }
    }

    private async Task<List<KitFolder>> LoadFoldersAsync(string category, CancellationToken ct)
        => await _db.KitFolders.AsNoTracking()
            .Where(f => f.Category == category)
            .OrderBy(f => f.DisplayOrder)
            .ThenBy(f => f.Title)
            .ToListAsync(ct);

    private async Task<List<KitItemDto>> LoadItemsAsync(string category, CancellationToken ct)
    {
        var rows = await _db.KitItems.AsNoTracking()
            .Where(i => i.Category == category)
            .OrderBy(i => i.DisplayOrder)
            .ThenByDescending(i => i.CreatedAt)
            .ToListAsync(ct);
        return rows.Select(MapItem).ToList();
    }

    private KitItemDto MapItem(KitItem row)
    {
        var filePath = string.IsNullOrWhiteSpace(row.FilePath) ? null : row.FilePath.Trim();
        var fileUrl = filePath == null ? null : BuildFileUrl(filePath);
        return new KitItemDto(
            row.Id,
            row.Type ?? "",
            row.Title ?? "",
            filePath,
            fileUrl,
            string.IsNullOrWhiteSpace(row.VideoUrl) ? null : row.VideoUrl.Trim(),
            row.DisplayOrder,
            row.Status ?? "active",
            row.FolderId,
            FormatDateTime(row.CreatedAt));
    }

    private static Dictionary<int, List<KitFolder>> BuildChildrenMap(IReadOnlyList<KitFolder> folders)
    {
        var map = new Dictionary<int, List<KitFolder>>();
        foreach (var folder in folders)
        {
            var parentId = folder.ParentId is > 0 ? folder.ParentId.Value : 0;
            if (!map.TryGetValue(parentId, out var list))
            {
                list = [];
                map[parentId] = list;
            }
            list.Add(folder);
        }
        return map;
    }

    private static int CountFolderItemsRecursive(
        int folderId,
        IReadOnlyDictionary<int, List<KitFolder>> childrenMap,
        IReadOnlyDictionary<int, List<KitItemDto>> itemsByFolder)
    {
        var count = itemsByFolder.GetValueOrDefault(folderId)?
            .Count(i => !string.Equals(i.Status, "inactive", StringComparison.OrdinalIgnoreCase)) ?? 0;
        foreach (var child in childrenMap.GetValueOrDefault(folderId) ?? [])
        {
            if (string.Equals(child.Status, "inactive", StringComparison.OrdinalIgnoreCase)) continue;
            count += CountFolderItemsRecursive(child.Id, childrenMap, itemsByFolder);
        }
        return count;
    }

    private static Dictionary<int, List<KitItemDto>> GroupItemsByFolder(IReadOnlyList<KitItemDto> items)
    {
        var map = new Dictionary<int, List<KitItemDto>>();
        foreach (var item in items)
        {
            if (item.FolderId is not > 0) continue;
            if (!map.TryGetValue(item.FolderId.Value, out var list))
            {
                list = [];
                map[item.FolderId.Value] = list;
            }
            list.Add(item);
        }
        return map;
    }

    private static List<KitBreadcrumbDto> BuildBreadcrumb(int folderId, IReadOnlyDictionary<int, KitFolder> foldersById)
    {
        var crumbs = new List<KitBreadcrumbDto>();
        var current = folderId;
        var guard = 0;
        while (current > 0 && foldersById.TryGetValue(current, out var folder) && guard < 50)
        {
            crumbs.Add(new KitBreadcrumbDto(folder.Id, folder.Title));
            current = folder.ParentId is > 0 ? folder.ParentId.Value : 0;
            guard++;
        }
        crumbs.Reverse();
        return crumbs;
    }

    private static List<KitFolderOptionDto> BuildFolderOptions(
        IReadOnlyDictionary<int, List<KitFolder>> childrenMap)
    {
        var options = new List<KitFolderOptionDto>();
        void Walk(int parentId, int depth)
        {
            if (!childrenMap.TryGetValue(parentId, out var children)) return;
            foreach (var folder in children.OrderBy(f => f.DisplayOrder).ThenBy(f => f.Title, StringComparer.OrdinalIgnoreCase))
            {
                options.Add(new KitFolderOptionDto(folder.Id, folder.Title, depth, folder.ParentId));
                Walk(folder.Id, depth + 1);
            }
        }

        Walk(0, 0);
        return options;
    }

    private async Task<int> ValidateFolderForCategoryAsync(int? folderId, string category, CancellationToken ct)
    {
        if (folderId is not > 0) return 0;
        return await FolderExistsAsync(folderId.Value, category, ct) ? folderId.Value : 0;
    }

    private async Task<int> ValidateParentFolderAsync(
        int? parentId,
        string category,
        int? excludeFolderId,
        CancellationToken ct)
    {
        if (parentId is not > 0) return 0;
        if (excludeFolderId.HasValue && parentId.Value == excludeFolderId.Value) return 0;
        if (!await FolderExistsAsync(parentId.Value, category, ct)) return 0;

        if (excludeFolderId is > 0)
        {
            var descendants = await GetFolderDescendantIdsAsync(excludeFolderId.Value, ct);
            if (descendants.Contains(parentId.Value)) return 0;
        }

        return parentId.Value;
    }

    private async Task<HashSet<int>> GetFolderDescendantIdsAsync(int folderId, CancellationToken ct)
    {
        var ids = new HashSet<int>();
        var queue = new Queue<int>();
        queue.Enqueue(folderId);
        var guard = 0;
        while (queue.Count > 0 && guard < 500)
        {
            guard++;
            var current = queue.Dequeue();
            var children = await _db.KitFolders.AsNoTracking()
                .Where(f => f.ParentId == current)
                .Select(f => f.Id)
                .ToListAsync(ct);
            foreach (var child in children)
            {
                if (ids.Add(child))
                    queue.Enqueue(child);
            }
        }
        return ids;
    }

    private async Task<bool> FolderExistsAsync(int folderId, string category, CancellationToken ct)
        => await _db.KitFolders.AsNoTracking().AnyAsync(f => f.Id == folderId && f.Category == category, ct);

    private string BuildFileUrl(string fileName)
    {
        var prefix = BuildPublicPrefix().TrimEnd('/');
        return $"{prefix}/{fileName.TrimStart('/')}";
    }

    private string BuildPublicPrefix()
    {
        var path = string.IsNullOrWhiteSpace(_app.KitsUploadsPublicPath)
            ? "/assets/upload/kits"
            : _app.KitsUploadsPublicPath.Trim();
        if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return path.TrimEnd('/');

        var baseUrl = (_app.PhpSiteBaseUrl ?? "").TrimEnd('/');
        return $"{baseUrl}/{path.TrimStart('/')}".TrimEnd('/');
    }

    private string? ResolveUploadDir()
    {
        if (!string.IsNullOrWhiteSpace(_app.KitsUploadsFsPath))
            return _app.KitsUploadsFsPath.Trim();
        return null;
    }

    private static string NormalizeCategory(string? category)
        => (category ?? "").Trim().ToLowerInvariant();

    private static bool IsAllowedCategory(string category)
        => AllowedCategories.Contains(category, StringComparer.OrdinalIgnoreCase);

    private static string CategoryLabel(string key) => key switch
    {
        "sales" => "SB Sales Kit",
        "marketing" => "Creator Kit",
        "franchise_sales" => "Franchise Sales Kit",
        _ => key
    };

    private static string NormalizeStatus(string? status)
        => string.Equals(status, "inactive", StringComparison.OrdinalIgnoreCase) ? "inactive" : "active";

    private static string? FormatDateTime(DateTime? dt)
        => dt.HasValue
            ? dt.Value.ToString("dd MMM yyyy, hh:mm tt", CultureInfo.InvariantCulture)
            : null;

    private static string FormatBytes(long bytes)
    {
        if (bytes >= 1_073_741_824) return $"{bytes / 1_073_741_824d:0.##} GB";
        if (bytes >= 1_048_576) return $"{bytes / 1_048_576d:0.##} MB";
        if (bytes >= 1024) return $"{bytes / 1024d:0.##} KB";
        return $"{bytes} bytes";
    }
}
