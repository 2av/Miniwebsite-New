using MiniWebsite.Application.Common.Models;
using MiniWebsite.Application.Users.Dtos;

namespace MiniWebsite.Application.Users;

public interface IUserService
{
    Task<ApiResult<UserDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ApiResult<UserDto>> GetMeAsync(int userId, CancellationToken ct = default);
    Task<ApiResult<PagedResult<UserDto>>> ListAsync(int page, int pageSize, string? role, string? search, CancellationToken ct = default);
    Task<ApiResult<UserDto>> CreateAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<ApiResult<UserDto>> UpdateAsync(int id, UpdateUserRequest request, CancellationToken ct = default);
    Task<ApiResult> SoftDeleteAsync(int id, CancellationToken ct = default);
}
