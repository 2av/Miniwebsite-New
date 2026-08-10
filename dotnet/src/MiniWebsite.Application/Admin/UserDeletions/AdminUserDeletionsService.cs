using Microsoft.EntityFrameworkCore;
using MiniWebsite.Application.Admin.UserDeletions.Dtos;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Domain.Enums;

namespace MiniWebsite.Application.Admin.UserDeletions;

public class AdminUserDeletionsService : IAdminUserDeletionsService
{
    private readonly IApplicationDbContext _db;

    public AdminUserDeletionsService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ApiResult<UserDeletionPageDto>> ListAsync(UserDeletionQuery query, CancellationToken ct = default)
    {
        query.Page = query.Page < 1 ? 1 : query.Page;
        query.PageSize = query.PageSize is < 1 or > 100 ? 10 : query.PageSize;

        // Global filter hides soft-deleted users — ignore it for this admin screen.
        var baseQ = _db.Users.AsNoTracking().IgnoreQueryFilters();

        var role = ParseRole(query.Role);
        if (role != null)
            baseQ = baseQ.Where(u => u.Role == role.Value);

        var activeCount = await baseQ.CountAsync(u => !u.IsDeleted, ct);
        var deletedCount = await baseQ.CountAsync(u => u.IsDeleted, ct);

        var q = baseQ.AsQueryable();
        var status = (query.Status ?? "").Trim().ToLowerInvariant();
        if (status == "deleted")
            q = q.Where(u => u.IsDeleted);
        else if (status == "active")
            q = q.Where(u => !u.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLower();
            q = q.Where(u =>
                u.Name.ToLower().Contains(s)
                || u.Email.ToLower().Contains(s));
        }

        var total = await q.CountAsync(ct);
        var users = await q
            .OrderByDescending(u => u.UpdatedAt ?? u.CreatedAt)
            .ThenByDescending(u => u.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        var rows = users.Select(u => new UserDeletionRowDto(
            u.Id,
            u.Email,
            u.Name,
            RoleToString(u.Role),
            u.Status,
            u.IsDeleted,
            u.CreatedAt,
            u.UpdatedAt)).ToList();

        return ApiResult<UserDeletionPageDto>.Ok(new UserDeletionPageDto(
            rows, total, activeCount, deletedCount, query.Page, query.PageSize));
    }

    public async Task<ApiResult> SoftDeleteAsync(BulkUserIdsRequest request, CancellationToken ct = default)
    {
        var ids = NormalizeIds(request.UserIds);
        if (ids.Count == 0)
            return ApiResult.Fail("No users selected.");

        var users = await _db.Users.IgnoreQueryFilters()
            .Where(u => ids.Contains(u.Id) && !u.IsDeleted)
            .ToListAsync(ct);

        if (users.Count == 0)
            return ApiResult.Fail("No users deleted");

        foreach (var u in users)
        {
            u.IsDeleted = true;
            u.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        return ApiResult.Ok(users.Count + " user(s) marked as deleted");
    }

    public async Task<ApiResult> RestoreAsync(BulkUserIdsRequest request, CancellationToken ct = default)
    {
        var ids = NormalizeIds(request.UserIds);
        if (ids.Count == 0)
            return ApiResult.Fail("No users selected.");

        var users = await _db.Users.IgnoreQueryFilters()
            .Where(u => ids.Contains(u.Id) && u.IsDeleted)
            .ToListAsync(ct);

        if (users.Count == 0)
            return ApiResult.Fail("No users restored");

        foreach (var u in users)
        {
            u.IsDeleted = false;
            u.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        return ApiResult.Ok(users.Count + " user(s) restored");
    }

    private static List<int> NormalizeIds(IReadOnlyList<int>? ids) =>
        (ids ?? [])
        .Where(id => id > 0)
        .Distinct()
        .ToList();

    private static UserRole? ParseRole(string? role) =>
        (role ?? "").Trim().ToUpperInvariant() switch
        {
            "CUSTOMER" => UserRole.Customer,
            "FRANCHISEE" => UserRole.Franchisee,
            "TEAM" => UserRole.Team,
            "ADMIN" => UserRole.Admin,
            _ => null
        };

    private static string RoleToString(UserRole role) => role switch
    {
        UserRole.Franchisee => "FRANCHISEE",
        UserRole.Team => "TEAM",
        UserRole.Admin => "ADMIN",
        _ => "CUSTOMER"
    };
}
