using Microsoft.EntityFrameworkCore;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Application.Users.Dtos;
using MiniWebsite.Domain.Entities;
using MiniWebsite.Domain.Enums;
using MiniWebsite.Application.Common.Extensions;
using MiniWebsite.Domain.Common;

namespace MiniWebsite.Application.Users;

public class UserService : IUserService
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(IApplicationDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<ApiResult<UserDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking()
            .Include(u => u.Wallet)
            .FirstOrDefaultAsync(u => u.Id == id, ct);
        return user == null
            ? ApiResult<UserDto>.Fail("User not found.")
            : ApiResult<UserDto>.Ok(Map(user));
    }

    public Task<ApiResult<UserDto>> GetMeAsync(int userId, CancellationToken ct = default) =>
        GetByIdAsync(userId, ct);

    public async Task<ApiResult<PagedResult<UserDto>>> ListAsync(int page, int pageSize, string? role, string? search, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = _db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(role))
        {
            var roleParsed = ParseRole(role);
            if (roleParsed == null)
                return ApiResult<PagedResult<UserDto>>.Fail("Invalid role. Use CUSTOMER, FRANCHISEE, TEAM, or ADMIN.");
            query = query.WhereRole(roleParsed.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            query = query.Where(u =>
                u.Email.ToLower().Contains(s) ||
                (u.FirstName + " " + u.LastName).ToLower().Contains(s) ||
                (u.Phone != null && u.Phone.Contains(s)));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return ApiResult<PagedResult<UserDto>>.Ok(new PagedResult<UserDto>
        {
            Items = items.Select(Map).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    public async Task<ApiResult<UserDto>> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var phone = request.Phone.Trim();

        if (!TryParseRole(request.Role, out var role))
            return ApiResult<UserDto>.Fail("Invalid role. Use CUSTOMER, FRANCHISEE, TEAM, or ADMIN.");

        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            return ApiResult<UserDto>.Fail("Email is already registered.");

        if (await _db.Users.AnyAsync(u => u.Phone == phone, ct))
            return ApiResult<UserDto>.Fail("Phone number is already registered.");

        var hash = _passwordHasher.Hash(request.Password);
        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = (request.LastName ?? "").Trim(),
            Email = email,
            Phone = phone,
            Password = hash,
            AssignedRole = await _db.GetDefaultRoleAsync(role, ct),
            Status = "ACTIVE",
            Address = new UserAddress
            {
                State = string.IsNullOrWhiteSpace(request.State) ? null : request.State.Trim(),
                District = string.IsNullOrWhiteSpace(request.District) ? null : request.District.Trim(),
            },
            Referral = new UserReferralSettings
            {
                CollaborationEnabled = false,
                SaleskitEnabled = false,
                Influencer = false,
                ReferralCode = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            },
            CreatedAt = AppClock.Now,
            UpdatedAt = AppClock.Now,
            IsDeleted = false
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return ApiResult<UserDto>.Ok(Map(user), "User created.");
    }

    public async Task<ApiResult<UserDto>> UpdateAsync(int id, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user == null)
            return ApiResult<UserDto>.Fail("User not found.");

        if (!string.IsNullOrWhiteSpace(request.FirstName))
            user.FirstName = request.FirstName.Trim();
        if (request.LastName != null)
            user.LastName = request.LastName.Trim();
        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            var phone = request.Phone.Trim();
            if (await _db.Users.AnyAsync(u => u.Phone == phone && u.Id != id, ct))
                return ApiResult<UserDto>.Fail("Phone number is already in use.");
            user.Phone = phone;
        }
        if (request.State != null)
            user.Address.State = string.IsNullOrWhiteSpace(request.State) ? null : request.State.Trim();
        if (request.District != null)
            user.Address.District = string.IsNullOrWhiteSpace(request.District) ? null : request.District.Trim();
        if (request.Department != null)
            user.Department = string.IsNullOrWhiteSpace(request.Department) ? null : request.Department.Trim();
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = request.Status.Trim().ToUpperInvariant();
            if (status is not ("ACTIVE" or "INACTIVE"))
                return ApiResult<UserDto>.Fail("Status must be ACTIVE or INACTIVE.");
            user.Status = status;
        }

        user.UpdatedAt = AppClock.Now;
        await _db.SaveChangesAsync(ct);
        return ApiResult<UserDto>.Ok(Map(user), "User updated.");
    }

    public async Task<ApiResult> SoftDeleteAsync(int id, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user == null)
            return ApiResult.Fail("User not found.");

        user.IsDeleted = true;
        user.UpdatedAt = AppClock.Now;
        await _db.SaveChangesAsync(ct);
        return ApiResult.Ok("User deleted.");
    }

    private static UserDto Map(User u) => new(
        u.Id,
        ToRoleLabel(u.LoginRole),
        u.Email,
        u.Phone,
        u.FirstName,
        u.LastName,
        u.FullName,
        u.Status,
        u.CreatedAt,
        u.UpdatedAt,
        u.Referral.ReferralCode,
        u.Referral.ReferredBy,
        u.Address.District,
        u.Address.State,
        u.Department,
        u.ProfileImage,
        u.Referral.CollaborationEnabled ? "YES" : "NO",
        u.Referral.SaleskitEnabled ? "YES" : "NO",
        u.Referral.Influencer ? "YES" : "NO",
        u.Wallet?.Balance.ToString(System.Globalization.CultureInfo.InvariantCulture),
        u.SelectService,
        u.Referral.MwReferralId,
        u.Referral.SenderUserId);

    private static UserRole? ParseRole(string role) =>
        TryParseRole(role, out var r) ? r : null;

    private static bool TryParseRole(string role, out UserRole parsed)
    {
        parsed = UserRole.Customer;
        switch (role.Trim().ToUpperInvariant())
        {
            case "CUSTOMER":
            case "1":
                parsed = UserRole.Customer;
                return true;
            case "FRANCHISEE":
            case "2":
                parsed = UserRole.Franchisee;
                return true;
            case "TEAM":
            case "3":
                parsed = UserRole.Team;
                return true;
            case "ADMIN":
            case "4":
                parsed = UserRole.Admin;
                return true;
            default:
                return Enum.TryParse(role, true, out parsed);
        }
    }

    private static string ToRoleLabel(UserRole role) => role switch
    {
        UserRole.Customer => "CUSTOMER",
        UserRole.Franchisee => "FRANCHISEE",
        UserRole.Team => "TEAM",
        UserRole.Admin => "ADMIN",
        _ => "CUSTOMER"
    };
}
