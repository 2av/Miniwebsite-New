using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Domain.Common;
using MiniWebsite.Domain.Entities;
using MiniWebsite.Domain.Enums;

namespace MiniWebsite.Application.Common.Extensions;

public static class UserRoleExtensions
{
    public static IQueryable<User> WhereRole(this IQueryable<User> users, UserRole role) =>
        users.Where(HasRole(role));

    /// <summary>SQL-translatable "user signs into <paramref name="role"/>'s portal" predicate.</summary>
    public static Expression<Func<User, bool>> HasRole(UserRole role)
    {
        if (role == UserRole.Customer)
        {
            var nonCustomer = RolePrefixes.NonCustomer;
            return u => u.AssignedRole!.Prefix == null || !nonCustomer.Contains(u.AssignedRole.Prefix);
        }

        var prefixes = role switch
        {
            UserRole.Franchisee => RolePrefixes.Franchisee,
            UserRole.Team => RolePrefixes.Team,
            _ => RolePrefixes.Admin,
        };
        return u => prefixes.Contains(u.AssignedRole!.Prefix!);
    }

    /// <summary>Tracked role entity, so a new user's <see cref="User.AssignedRole"/> is populated before save.</summary>
    public static async Task<Role> GetRoleByPrefixAsync(this IApplicationDbContext db, string prefix, CancellationToken ct) =>
        await db.Roles.Where(r => r.Prefix == prefix).OrderBy(r => r.RoleId).FirstOrDefaultAsync(ct)
        ?? throw new InvalidOperationException($"Role with prefix '{prefix}' is missing from the roles table.");

    public static Task<Role> GetDefaultRoleAsync(this IApplicationDbContext db, UserRole loginRole, CancellationToken ct) =>
        db.GetRoleByPrefixAsync(RolePrefixes.DefaultFor(loginRole), ct);

    public static string ToApi(this UserRole role) => role switch
    {
        UserRole.Franchisee => "FRANCHISEE",
        UserRole.Team => "TEAM",
        UserRole.Admin => "ADMIN",
        _ => "CUSTOMER",
    };

    public static UserRole? ParseApiRole(string? role) =>
        (role ?? "").Trim().ToUpperInvariant() switch
        {
            "CUSTOMER" => UserRole.Customer,
            "FRANCHISEE" => UserRole.Franchisee,
            "TEAM" => UserRole.Team,
            "ADMIN" => UserRole.Admin,
            _ => null,
        };
}
