namespace MiniWebsite.Application.Auth.Dtos;

public record RegisterRequest(
    string Name,
    string Email,
    string Phone,
    string Password,
    string? State = null,
    string? ReferralCode = null);

/// <param name="Role">Optional portal role filter: CUSTOMER, FRANCHISEE, or TEAM (matches PHP login pages).</param>
public record LoginRequest(string UserId, string Password, string? Role = null);

public record RefreshTokenRequest(string AccessToken, string RefreshToken);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(string Email, string Token, string NewPassword);

public record LogoutRequest(string RefreshToken);

public record ChangePasswordRequest(string Email, string CurrentPassword, string NewPassword, string ConfirmPassword);

public record AuthResponse(
    int UserId,
    string Email,
    string Name,
    string Role,
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    string? CollaborationEnabled = null,
    string? SaleskitEnabled = null);
