using MiniWebsite.Application.Auth.Dtos;
using MiniWebsite.Application.Common.Models;

namespace MiniWebsite.Application.Auth;

public interface IAuthService
{
    Task<ApiResult<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<ApiResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<ApiResult<AuthResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default);
    Task<ApiResult> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default);
    Task<ApiResult> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);
    Task<ApiResult> LogoutAsync(int userId, string refreshToken, CancellationToken ct = default);
    Task<ApiResult> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default);
}
