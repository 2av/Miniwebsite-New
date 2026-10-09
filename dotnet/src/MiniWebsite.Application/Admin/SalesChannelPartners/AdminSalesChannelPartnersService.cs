using System.Globalization;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MiniWebsite.Application.Admin.EmailTemplates;
using MiniWebsite.Application.Admin.SalesChannelPartners.Dtos;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Application.Common.Options;
using MiniWebsite.Domain.Entities;
using MiniWebsite.Domain.Enums;

namespace MiniWebsite.Application.Admin.SalesChannelPartners;

public class AdminSalesChannelPartnersService : IAdminSalesChannelPartnersService
{
    /// <summary>
    /// Sidebar slug → roles.Prefix. LoginRole is the legacy users.Role that decides which portal the partner signs into.
    /// </summary>
    private static readonly (string Slug, string Prefix, UserRole LoginRole)[] Partners =
    [
        ("franchisee-partner", "FRP", UserRole.Franchisee),
        ("print-shop-partner", "PSP", UserRole.Team),
        ("digital-marketing-partner", "DMP", UserRole.Team),
        ("social-media-partner", "SMP", UserRole.Team),
        ("field-sales-partner", "FSP", UserRole.Team),
        ("field-sales-team", "FST", UserRole.Team),
    ];

    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailSender _email;
    private readonly IEmailTemplateRenderer _templates;
    private readonly AppOptions _app;

    public AdminSalesChannelPartnersService(
        IApplicationDbContext db,
        IPasswordHasher passwordHasher,
        IEmailSender email,
        IEmailTemplateRenderer templates,
        IOptions<AppOptions> app)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _email = email;
        _templates = templates;
        _app = app.Value;
    }

    public async Task<ApiResult<IReadOnlyList<SalesChannelPartnerRoleDto>>> ListRolesAsync(CancellationToken ct = default)
    {
        var roles = await _db.Roles.AsNoTracking().ToListAsync(ct);
        var result = new List<SalesChannelPartnerRoleDto>();
        foreach (var p in Partners)
        {
            var role = roles.Where(r => string.Equals(r.Prefix, p.Prefix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(r => r.RoleId)
                .FirstOrDefault();
            if (role != null) result.Add(MapRole(p.Slug, role));
        }
        return ApiResult<IReadOnlyList<SalesChannelPartnerRoleDto>>.Ok(result);
    }

    public async Task<ApiResult<SalesChannelPartnersPageDto>> ListAsync(
        string slug, SalesChannelPartnersQuery query, CancellationToken ct = default)
    {
        var (partner, role, error) = await ResolveAsync(slug, ct);
        if (error != null) return ApiResult<SalesChannelPartnersPageDto>.Fail(error);

        query.Page = query.Page < 1 ? 1 : query.Page;
        query.PageSize = query.PageSize is < 1 or > 100 ? 10 : query.PageSize;

        try
        {
            var q = _db.Users.AsNoTracking().Where(u => u.RoleId == role!.RoleId);
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var s = query.Search.Trim().ToLower();
                q = q.Where(u =>
                    u.Email.ToLower().Contains(s)
                    || u.Name.ToLower().Contains(s)
                    || (u.Phone != null && u.Phone.Contains(s))
                    || (u.District != null && u.District.ToLower().Contains(s))
                    || (u.State != null && u.State.ToLower().Contains(s)));
            }

            var status = NormalizeStatusFilter(query.Status);
            if (status != null)
                q = q.Where(u => u.Status == status);

            var total = await q.CountAsync(ct);
            var users = await q
                .OrderByDescending(u => u.CreatedAt)
                .ThenByDescending(u => u.Id)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(ct);

            return ApiResult<SalesChannelPartnersPageDto>.Ok(new SalesChannelPartnersPageDto(
                MapRole(partner.Slug, role!),
                users.Select(u => MapRow(u, role!)).ToList(),
                total,
                query.Page,
                query.PageSize));
        }
        catch (Exception ex)
        {
            return ApiResult<SalesChannelPartnersPageDto>.Fail("Unable to load partners: " + ex.Message);
        }
    }

    public async Task<ApiResult<SalesChannelPartnerRowDto>> CreateAsync(
        string slug, CreateSalesChannelPartnerRequest request, CancellationToken ct = default)
    {
        var (partner, role, error) = await ResolveAsync(slug, ct);
        if (error != null) return ApiResult<SalesChannelPartnerRowDto>.Fail(error);

        var name = (request.Name ?? "").Trim();
        var email = NormalizeEmail(request.Email);
        var phone = (request.Phone ?? "").Trim();
        var password = request.Password ?? "";

        if (name.Length == 0) return ApiResult<SalesChannelPartnerRowDto>.Fail("Name is required.");
        if (email.Length == 0 || !email.Contains('@')) return ApiResult<SalesChannelPartnerRowDto>.Fail("A valid email address is required.");
        if (password.Length < 6) return ApiResult<SalesChannelPartnerRowDto>.Fail("Password must be at least 6 characters long.");

        var conflict = await FindConflictAsync(null, email, phone, ct);
        if (conflict != null) return ApiResult<SalesChannelPartnerRowDto>.Fail(conflict);

        var hash = _passwordHasher.Hash(password);
        var user = new User
        {
            Role = partner.LoginRole,
            RoleId = role!.RoleId,
            Email = email,
            Phone = phone,
            Name = name,
            District = (request.District ?? "").Trim(),
            State = (request.State ?? "").Trim(),
            Password = hash,
            PasswordHash = hash,
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        var message = $"{role.RoleName} added successfully.";
        try
        {
            await SendWelcomeAsync(partner.LoginRole, role, name, email, password, ct);
        }
        catch
        {
            message = $"{role.RoleName} created, but welcome email could not be sent.";
        }

        return ApiResult<SalesChannelPartnerRowDto>.Ok(MapRow(user, role), message);
    }

    public async Task<ApiResult<SalesChannelPartnerRowDto>> UpdateAsync(
        string slug, int id, UpdateSalesChannelPartnerRequest request, CancellationToken ct = default)
    {
        var (_, role, error) = await ResolveAsync(slug, ct);
        if (error != null) return ApiResult<SalesChannelPartnerRowDto>.Fail(error);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.RoleId == role!.RoleId, ct);
        if (user == null) return ApiResult<SalesChannelPartnerRowDto>.Fail($"{role!.RoleName} not found");

        var name = (request.Name ?? "").Trim();
        var email = NormalizeEmail(request.Email);
        var phone = (request.Phone ?? "").Trim();

        if (name.Length == 0) return ApiResult<SalesChannelPartnerRowDto>.Fail("Name is required.");
        if (email.Length == 0 || !email.Contains('@')) return ApiResult<SalesChannelPartnerRowDto>.Fail("A valid email address is required.");

        var conflict = await FindConflictAsync(user.Id, email, phone, ct);
        if (conflict != null) return ApiResult<SalesChannelPartnerRowDto>.Fail(conflict);

        user.Name = name;
        user.Email = email;
        user.Phone = phone;
        user.District = (request.District ?? "").Trim();
        user.State = (request.State ?? "").Trim();
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return ApiResult<SalesChannelPartnerRowDto>.Ok(MapRow(user, role!), $"{role!.RoleName} updated successfully.");
    }

    public async Task<ApiResult<SalesChannelPartnerRowDto>> ToggleStatusAsync(
        string slug, int id, ToggleSalesChannelPartnerStatusRequest request, CancellationToken ct = default)
    {
        var (_, role, error) = await ResolveAsync(slug, ct);
        if (error != null) return ApiResult<SalesChannelPartnerRowDto>.Fail(error);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.RoleId == role!.RoleId, ct);
        if (user == null) return ApiResult<SalesChannelPartnerRowDto>.Fail($"{role!.RoleName} not found");

        var requested = (request.NewStatus ?? "").Trim().ToUpperInvariant();
        user.Status = requested is "ACTIVE" or "INACTIVE"
            ? requested
            : string.Equals(user.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase) ? "INACTIVE" : "ACTIVE";
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return ApiResult<SalesChannelPartnerRowDto>.Ok(MapRow(user, role!), "Status updated.");
    }

    public async Task<ApiResult> ResetPasswordAsync(
        string slug, int id, ResetSalesChannelPartnerPasswordRequest request, CancellationToken ct = default)
    {
        var (_, role, error) = await ResolveAsync(slug, ct);
        if (error != null) return ApiResult.Fail(error);

        var password = (request.NewPassword ?? "").Trim();
        if (password.Length < 6)
            return ApiResult.Fail("Please provide a valid password (minimum 6 characters).");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.RoleId == role!.RoleId, ct);
        if (user == null) return ApiResult.Fail($"{role!.RoleName} not found");

        var hash = _passwordHasher.Hash(password);
        user.Password = hash;
        user.PasswordHash = hash;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ApiResult.Ok("Password reset successfully.");
    }

    private async Task<((string Slug, string Prefix, UserRole LoginRole) Partner, Role? Role, string? Error)> ResolveAsync(
        string slug, CancellationToken ct)
    {
        var key = (slug ?? "").Trim().ToLowerInvariant();
        var partner = Partners.FirstOrDefault(p => p.Slug == key);
        if (partner.Slug == null) return (partner, null, "Unknown sales channel partner type.");

        var role = await _db.Roles.AsNoTracking()
            .Where(r => r.Prefix == partner.Prefix)
            .OrderBy(r => r.RoleId)
            .FirstOrDefaultAsync(ct);
        if (role == null)
            return (partner, null, $"Role with prefix '{partner.Prefix}' is missing in Role Management.");

        return (partner, role, null);
    }

    private async Task<string?> FindConflictAsync(int? excludeId, string email, string phone, CancellationToken ct)
    {
        // Soft-deleted users still hold the unique email index.
        var emailOwner = await _db.Users.IgnoreQueryFilters().AsNoTracking()
            .Where(u => (excludeId == null || u.Id != excludeId) && u.Email.ToLower() == email)
            .Select(u => new { RoleName = u.AssignedRole != null ? u.AssignedRole.RoleName : null })
            .FirstOrDefaultAsync(ct);
        if (emailOwner != null)
            return emailOwner.RoleName != null
                ? $"This email address is already registered as a {emailOwner.RoleName}. Please use a different email."
                : "This email address is already registered. Please use a different email.";

        if (phone.Length == 0) return null;

        var phoneTaken = await _db.Users.AsNoTracking()
            .AnyAsync(u => (excludeId == null || u.Id != excludeId) && u.Phone == phone, ct);
        return phoneTaken ? "This mobile number is already registered. Please use a different mobile number." : null;
    }

    private async Task SendWelcomeAsync(
        UserRole loginRole, Role role, string name, string email, string password, CancellationToken ct)
    {
        var loginUrl = string.IsNullOrWhiteSpace(_app.PhpSiteBaseUrl)
            ? $"https://{_app.PublicHost}"
            : _app.PhpSiteBaseUrl.TrimEnd('/');
        var vars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = name,
            ["email"] = email,
            ["password"] = password,
            ["login_url"] = loginUrl,
            ["host"] = _app.PublicHost,
        };
        var templateKey = loginRole == UserRole.Franchisee ? "franchisee_admin_created" : "team_member_welcome";
        var rendered = await _templates.TryRenderAsync(templateKey, vars, ct);
        var subject = rendered?.Subject ?? $"Welcome to {_app.PublicHost} — {role.RoleName}";
        var html = rendered?.Html ?? $"""
            <p>Hello {WebUtility.HtmlEncode(name)},</p>
            <p>Your {WebUtility.HtmlEncode(role.RoleName)} account has been created.</p>
            <p><strong>Login Email:</strong> {WebUtility.HtmlEncode(email)}<br/>
            <strong>Password:</strong> {WebUtility.HtmlEncode(password)}</p>
            <p>Please keep these credentials secure.</p>
            """;
        await _email.SendAsync(email, subject, html, ct);
    }

    private static SalesChannelPartnerRoleDto MapRole(string slug, Role role) => new(
        slug,
        role.RoleId,
        role.RoleName,
        role.RoleDescription ?? "",
        role.Prefix ?? "",
        role.SalesType ?? "");

    private static SalesChannelPartnerRowDto MapRow(User u, Role role)
    {
        var status = (u.Status ?? "ACTIVE").ToUpperInvariant();
        return new SalesChannelPartnerRowDto(
            u.Id,
            $"{role.Prefix}-{u.Id.ToString("D4", CultureInfo.InvariantCulture)}",
            u.Name,
            u.Email,
            u.Phone ?? "",
            u.District ?? "",
            u.State ?? "",
            status,
            status == "ACTIVE" ? "ok" : "danger",
            u.CreatedAt.Year < 2 ? "—" : u.CreatedAt.ToString("dd MMM yyyy", CultureInfo.InvariantCulture));
    }

    private static string? NormalizeStatusFilter(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("all", StringComparison.OrdinalIgnoreCase))
            return null;
        var v = value.Trim().ToUpperInvariant();
        return v is "ACTIVE" or "INACTIVE" ? v : null;
    }

    private static string NormalizeEmail(string? email) => (email ?? "").Trim().ToLowerInvariant();
}
