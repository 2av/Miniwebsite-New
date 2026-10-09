using Microsoft.EntityFrameworkCore;
using MiniWebsite.Application.Admin.RoleManagement.Dtos;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Domain.Common;
using MiniWebsite.Domain.Entities;

namespace MiniWebsite.Application.Admin.RoleManagement;

public class AdminRoleManagementService : IAdminRoleManagementService
{
    private static readonly RoleSalesTypeOptionDto[] SalesTypes =
    [
        new("Do Offline Sales", "Do Offline Sales"),
        new("Do Online Sales", "Do Online Sales"),
        new("NA", "NA"),
    ];

    private readonly IApplicationDbContext _db;

    public AdminRoleManagementService(IApplicationDbContext db)
    {
        _db = db;
    }

    public ManageRolesMetaDto GetMeta() => new(SalesTypes);

    public async Task<ApiResult<ManageRolesPageDto>> ListAsync(ManageRolesQuery query, CancellationToken ct = default)
    {
        query.Page = query.Page < 1 ? 1 : query.Page;
        query.PageSize = query.PageSize is < 1 or > 100 ? 10 : query.PageSize;

        try
        {
            var q = ApplyFilters(_db.Roles.AsNoTracking(), query);
            var total = await q.CountAsync(ct);
            var rows = await q
                .OrderBy(r => r.RoleId)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(ct);

            return ApiResult<ManageRolesPageDto>.Ok(new ManageRolesPageDto(
                rows.Select(MapEntity).ToList(),
                total,
                query.Page,
                query.PageSize));
        }
        catch (Exception ex)
        {
            return ApiResult<ManageRolesPageDto>.Fail("Unable to load roles: " + ex.Message);
        }
    }

    public async Task<ApiResult<ManageRoleRowDto>> GetAsync(int roleId, CancellationToken ct = default)
    {
        try
        {
            var row = await _db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.RoleId == roleId, ct);
            if (row == null) return ApiResult<ManageRoleRowDto>.Fail("Role not found");
            return ApiResult<ManageRoleRowDto>.Ok(MapEntity(row));
        }
        catch (Exception ex)
        {
            return ApiResult<ManageRoleRowDto>.Fail("Unable to load role: " + ex.Message);
        }
    }

    public async Task<ApiResult<ManageRoleRowDto>> CreateAsync(UpsertRoleRequest request, CancellationToken ct = default)
    {
        var validation = Validate(request);
        if (validation != null) return ApiResult<ManageRoleRowDto>.Fail(validation);

        var data = Normalize(request);
        var now = DateTime.UtcNow;

        try
        {
            if (await _db.Roles.AnyAsync(r => r.RoleName.ToLower() == data.RoleName.ToLower(), ct))
                return ApiResult<ManageRoleRowDto>.Fail("A role with this name already exists");

            var entity = new Role
            {
                RoleName = data.RoleName,
                RoleDescription = data.RoleDescription,
                Prefix = data.Prefix,
                SalesType = data.SalesType,
                CreatedAt = now,
                UpdatedAt = now,
            };
            _db.Roles.Add(entity);
            await _db.SaveChangesAsync(ct);
            return ApiResult<ManageRoleRowDto>.Ok(MapEntity(entity), "Role added successfully!");
        }
        catch (Exception ex)
        {
            return ApiResult<ManageRoleRowDto>.Fail("Error adding role: " + ex.Message);
        }
    }

    public async Task<ApiResult<ManageRoleRowDto>> UpdateAsync(int roleId, UpsertRoleRequest request, CancellationToken ct = default)
    {
        var validation = Validate(request);
        if (validation != null) return ApiResult<ManageRoleRowDto>.Fail(validation);

        var data = Normalize(request);

        try
        {
            var entity = await _db.Roles.FirstOrDefaultAsync(r => r.RoleId == roleId, ct);
            if (entity == null) return ApiResult<ManageRoleRowDto>.Fail("Role not found");

            if (await _db.Roles.AnyAsync(
                    r => r.RoleId != roleId && r.RoleName.ToLower() == data.RoleName.ToLower(), ct))
                return ApiResult<ManageRoleRowDto>.Fail("A role with this name already exists");

            var prefixError = await ValidatePrefixChangeAsync(entity, data.Prefix, ct);
            if (prefixError != null) return ApiResult<ManageRoleRowDto>.Fail(prefixError);

            entity.RoleName = data.RoleName;
            entity.RoleDescription = data.RoleDescription;
            entity.Prefix = data.Prefix;
            entity.SalesType = data.SalesType;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return ApiResult<ManageRoleRowDto>.Ok(MapEntity(entity), "Role updated successfully!");
        }
        catch (Exception ex)
        {
            return ApiResult<ManageRoleRowDto>.Fail("Error updating role: " + ex.Message);
        }
    }

    public async Task<ApiResult> DeleteAsync(int roleId, CancellationToken ct = default)
    {
        try
        {
            var entity = await _db.Roles.FirstOrDefaultAsync(r => r.RoleId == roleId, ct);
            if (entity == null) return ApiResult.Fail("Role not found");

            if (IsSystemPrefix(entity.Prefix))
                return ApiResult.Fail($"Role '{entity.RoleName}' is a system role and can't be deleted.");
            if (await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.RoleId == roleId, ct))
                return ApiResult.Fail($"Role '{entity.RoleName}' is assigned to users and can't be deleted.");

            _db.Roles.Remove(entity);
            await _db.SaveChangesAsync(ct);
            return ApiResult.Ok("Role deleted successfully!");
        }
        catch (Exception ex)
        {
            return ApiResult.Fail("Error deleting role: " + ex.Message);
        }
    }

    private static bool IsSystemPrefix(string? prefix) =>
        prefix != null && RolePrefixes.All.Contains(prefix.Trim().ToUpperInvariant());

    /// <summary>System prefixes decide each user's portal, so they must stay unique and unchanged.</summary>
    private async Task<string?> ValidatePrefixChangeAsync(Role entity, string? newPrefix, CancellationToken ct)
    {
        var oldKey = (entity.Prefix ?? "").Trim().ToUpperInvariant();
        var newKey = (newPrefix ?? "").Trim().ToUpperInvariant();
        if (oldKey == newKey) return null;

        if (IsSystemPrefix(oldKey))
            return $"Prefix '{entity.Prefix}' is used by the system and can't be changed.";
        if (IsSystemPrefix(newKey))
            return $"Prefix '{newKey}' is reserved for a system role.";
        return null;
    }

    private static IQueryable<Role> ApplyFilters(IQueryable<Role> q, ManageRolesQuery query)
    {
        var salesType = string.IsNullOrWhiteSpace(query.SalesType) ? null : query.SalesType.Trim();
        if (salesType != null)
            q = q.Where(r => r.SalesType == salesType);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLower();
            q = q.Where(r =>
                r.RoleName.ToLower().Contains(s)
                || (r.RoleDescription != null && r.RoleDescription.ToLower().Contains(s))
                || (r.Prefix != null && r.Prefix.ToLower().Contains(s)));
        }

        return q;
    }

    private static string? Validate(UpsertRoleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RoleName)) return "Role name is required";
        if (request.RoleName.Trim().Length > 120) return "Role name is too long";
        if ((request.RoleDescription ?? "").Trim().Length > 500) return "Role description is too long";
        if ((request.Prefix ?? "").Trim().Length > 40) return "Prefix is too long";
        var salesType = (request.SalesType ?? "").Trim();
        if (salesType.Length > 0 && SalesTypes.All(s => s.Value != salesType)) return "Invalid sales type";
        return null;
    }

    private static (string RoleName, string RoleDescription, string Prefix, string SalesType) Normalize(
        UpsertRoleRequest request) =>
        (
            request.RoleName.Trim(),
            (request.RoleDescription ?? "").Trim(),
            (request.Prefix ?? "").Trim(),
            (request.SalesType ?? "").Trim()
        );

    private static ManageRoleRowDto MapEntity(Role r) => new(
        r.RoleId,
        r.RoleName,
        r.RoleDescription ?? "",
        r.Prefix ?? "",
        r.SalesType ?? "");
}
