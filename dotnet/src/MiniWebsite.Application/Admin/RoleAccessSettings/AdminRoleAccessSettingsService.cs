using Microsoft.EntityFrameworkCore;
using MiniWebsite.Application.Admin.RoleAccessSettings.Dtos;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;

namespace MiniWebsite.Application.Admin.RoleAccessSettings;

public class AdminRoleAccessSettingsService : IAdminRoleAccessSettingsService
{
    private readonly IApplicationDbContext _db;

    public AdminRoleAccessSettingsService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<RoleAccessMatrixDto> GetMatrixAsync(CancellationToken ct = default)
    {
        var profiles = await _db.RoleAccessProfiles.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Id)
            .ToListAsync(ct);

        var features = await _db.RoleAccessFeatures.AsNoTracking()
            .Where(f => f.IsActive)
            .OrderBy(f => f.SortOrder)
            .ThenBy(f => f.Id)
            .ToListAsync(ct);

        var cells = await (
            from s in _db.RoleAccessSettings.AsNoTracking()
            join p in _db.RoleAccessProfiles.AsNoTracking() on s.ProfileId equals p.Id
            join f in _db.RoleAccessFeatures.AsNoTracking() on s.FeatureId equals f.Id
            orderby p.SortOrder, f.SortOrder
            select new
            {
                s.Id,
                p.ProfileKey,
                f.FeatureKey,
                s.IsNotApplicable,
                s.SettingValue,
            }).ToListAsync(ct);

        var profileDtos = profiles
            .Select(p => new RoleAccessProfileDto(
                p.Id,
                p.ProfileKey ?? "",
                p.ProfileLabel ?? "",
                p.BaseRole ?? "",
                p.RequiresCollaboration ?? "ANY",
                p.RequiresInfluencer ?? "ANY",
                p.SortOrder))
            .ToList();

        var featureDtos = features
            .Select(f => new RoleAccessFeatureDto(
                f.Id,
                f.FeatureKey ?? "",
                f.FeatureLabel ?? "",
                f.FeatureGroup ?? "General",
                f.FieldType ?? "text",
                f.SortOrder))
            .ToList();

        var groups = featureDtos
            .GroupBy(f => f.FeatureGroup, StringComparer.OrdinalIgnoreCase)
            .Select(g => new RoleAccessFeatureGroupDto(
                g.Key,
                g.OrderBy(x => x.SortOrder).ThenBy(x => x.FeatureLabel, StringComparer.OrdinalIgnoreCase).ToList()))
            .ToList();

        var cellDtos = cells
            .Select(c => new RoleAccessCellDto(
                c.Id,
                c.ProfileKey ?? "",
                c.FeatureKey ?? "",
                c.IsNotApplicable,
                c.SettingValue ?? ""))
            .ToList();

        return new RoleAccessMatrixDto(true, profileDtos, groups, cellDtos);
    }

    public async Task<ApiResult> UpdateSettingAsync(int settingId, UpdateRoleAccessSettingRequest request, CancellationToken ct = default)
    {
        if (settingId <= 0)
            return ApiResult.Fail("Invalid setting.");

        try
        {
            var entity = await _db.RoleAccessSettings.FirstOrDefaultAsync(s => s.Id == settingId, ct);
            if (entity == null)
                return ApiResult.Fail("Setting not found.");

            entity.IsNotApplicable = request.IsNotApplicable;
            entity.SettingValue = (request.SettingValue ?? "").Trim();
            entity.UpdatedBy = string.IsNullOrWhiteSpace(request.UpdatedBy) ? "admin" : request.UpdatedBy.Trim();
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return ApiResult.Ok("Setting updated successfully.");
        }
        catch (Exception ex)
        {
            return ApiResult.Fail("Failed to update setting: " + ex.Message);
        }
    }
}
