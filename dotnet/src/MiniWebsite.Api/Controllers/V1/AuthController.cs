using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniWebsite.Application.Auth;
using MiniWebsite.Application.Auth.Dtos;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Shared.Constants;
using System.Security.Claims;

namespace MiniWebsite.Api.Controllers.V1;

/// <summary>Public auth endpoints for customer / franchisee / team portals.</summary>
[ApiController]
[Route(ApiConstants.ApiRoutePrefix + "/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<ResetPasswordRequest> _resetValidator;

    public AuthController(
        IAuthService auth,
        IValidator<LoginRequest> loginValidator,
        IValidator<ResetPasswordRequest> resetValidator)
    {
        _auth = auth;
        _loginValidator = loginValidator;
        _resetValidator = resetValidator;
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResult<AuthResponse>>> Login(
        [FromBody] LoginRequest request, CancellationToken ct)
    {
        var validation = await _loginValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(ApiResult<AuthResponse>.Fail("Validation failed.", ToErrors(validation)));

        var result = await _auth.LoginAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResult<AuthResponse>>> Refresh(
        [FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var result = await _auth.RefreshAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult<ApiResult>> ForgotPassword(
        [FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(ApiResult.Fail("Email is required."));

        var result = await _auth.ForgotPasswordAsync(request, ct);
        return Ok(result);
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult<ApiResult>> ResetPassword(
        [FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        var validation = await _resetValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(ApiResult.Fail("Validation failed.", ToErrors(validation)));

        var result = await _auth.ResetPasswordAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("logout")]
    public async Task<ActionResult<ApiResult>> Logout(
        [FromBody] LogoutRequest request, CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Ok(ApiResult.Ok("Logged out."));
        }

        var result = await _auth.LogoutAsync(userId, request.RefreshToken ?? "", ct);
        return Ok(result);
    }

    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResult>> ChangePassword(
        [FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(ApiResult.Fail("Email and new password are required."));

        var result = await _auth.ChangePasswordAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    private static Dictionary<string, string[]> ToErrors(FluentValidation.Results.ValidationResult validation) =>
        validation.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
}
