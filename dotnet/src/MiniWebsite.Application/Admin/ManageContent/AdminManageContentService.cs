using System.Globalization;
using Microsoft.EntityFrameworkCore;
using MiniWebsite.Application.Admin.ManageContent.Dtos;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Domain.Entities;

namespace MiniWebsite.Application.Admin.ManageContent;

public class AdminManageContentService : IAdminManageContentService
{
    private static readonly ContentTypeOptionDto[] ContentTypes =
    [
        new("terms_conditions", "Terms & Conditions", "Legal"),
        new("privacy_policy", "Privacy Policy", "Privacy"),
        new("franchisee_agreement", "Franchisee Agreement", "Partnership"),
        new("franchisee_distributer", "Franchisee Distributer", "Program"),
        new("mw_full_franchise_agreement", "MW Full Franchise Agreement", "Franchise"),
        new("mw_franchisee_operation_policy", "MW Franchisee Operation Policy", "Policy"),
    ];

    private readonly IApplicationDbContext _db;

    public AdminManageContentService(IApplicationDbContext db)
    {
        _db = db;
    }

    public ManageContentMetaDto GetMeta() => new(ContentTypes);

    public async Task<ApiResult<ManageContentListDto>> ListAsync(CancellationToken ct = default)
    {
        try
        {
            var items = new List<ManageContentItemDto>();
            foreach (var type in ContentTypes)
                items.Add(await LoadOrEmptyAsync(type.Value, ct));

            return ApiResult<ManageContentListDto>.Ok(new ManageContentListDto(items));
        }
        catch (Exception ex)
        {
            return ApiResult<ManageContentListDto>.Fail("Unable to load content: " + ex.Message);
        }
    }

    public async Task<ApiResult<ManageContentItemDto>> GetByTypeAsync(string contentType, CancellationToken ct = default)
    {
        if (!IsValidType(contentType))
            return ApiResult<ManageContentItemDto>.Fail("Invalid content type");

        try
        {
            return ApiResult<ManageContentItemDto>.Ok(await LoadOrEmptyAsync(contentType.Trim(), ct));
        }
        catch (Exception ex)
        {
            return ApiResult<ManageContentItemDto>.Fail("Unable to load content: " + ex.Message);
        }
    }

    public async Task<ApiResult<ManageContentItemDto>> UpsertAsync(UpsertContentRequest request, CancellationToken ct = default)
    {
        var contentType = (request.ContentType ?? "").Trim();
        if (!IsValidType(contentType))
            return ApiResult<ManageContentItemDto>.Fail("Invalid content type");
        if (string.IsNullOrWhiteSpace(request.Title))
            return ApiResult<ManageContentItemDto>.Fail("Title is required");

        var title = request.Title.Trim();
        var content = request.Content ?? "";
        var metaDesc = request.MetaDescription ?? "";
        var metaKeys = request.MetaKeywords ?? "";
        var updatedBy = string.IsNullOrWhiteSpace(request.UpdatedBy) ? "admin" : request.UpdatedBy.Trim();

        try
        {
            var entity = await _db.ContentPages
                .FirstOrDefaultAsync(c => c.ContentType == contentType, ct);

            if (entity != null)
            {
                entity.Title = title;
                entity.Content = content;
                entity.MetaDescription = metaDesc;
                entity.MetaKeywords = metaKeys;
                entity.UpdatedBy = updatedBy;
                entity.LastUpdated = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);

                return ApiResult<ManageContentItemDto>.Ok(
                    MapEntity(entity),
                    "Content updated successfully!");
            }

            entity = new ContentPage
            {
                ContentType = contentType,
                Title = title,
                Content = content,
                MetaDescription = metaDesc,
                MetaKeywords = metaKeys,
                UpdatedBy = updatedBy,
                LastUpdated = DateTime.UtcNow,
                IsActive = true,
            };
            _db.ContentPages.Add(entity);
            await _db.SaveChangesAsync(ct);

            return ApiResult<ManageContentItemDto>.Ok(
                MapEntity(entity),
                "Content created successfully!");
        }
        catch (Exception ex)
        {
            return ApiResult<ManageContentItemDto>.Fail("Error saving content: " + ex.Message);
        }
    }

    private async Task<ManageContentItemDto> LoadOrEmptyAsync(string contentType, CancellationToken ct)
    {
        var row = await _db.ContentPages.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ContentType == contentType, ct);

        if (row == null)
        {
            return new ManageContentItemDto(
                contentType, "", "", "", "", null, null, null);
        }

        return MapEntity(row);
    }

    private static ManageContentItemDto MapEntity(ContentPage row)
    {
        string? display = null;
        string? lastUpdatedIso = null;
        if (row.LastUpdated is { } dt && dt.Year > 1)
        {
            lastUpdatedIso = dt.ToString("o");
            display = dt.ToString("dd MMM yyyy HH:mm", CultureInfo.InvariantCulture);
        }

        return new ManageContentItemDto(
            row.ContentType,
            row.Title ?? "",
            row.Content ?? "",
            row.MetaDescription ?? "",
            row.MetaKeywords ?? "",
            lastUpdatedIso,
            display,
            row.UpdatedBy);
    }

    private static bool IsValidType(string? contentType) =>
        !string.IsNullOrWhiteSpace(contentType)
        && ContentTypes.Any(t => t.Value.Equals(contentType.Trim(), StringComparison.Ordinal));
}
