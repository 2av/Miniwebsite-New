using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Application.Registration;
using MiniWebsite.Application.Registration.Dtos;
using MiniWebsite.Shared.Constants;

namespace MiniWebsite.Api.Controllers.V1;

[ApiController]
[Route(ApiConstants.ApiRoutePrefix + "/registration")]
[AllowAnonymous]
public class RegistrationController : ControllerBase
{
    private readonly IRegistrationService _registration;
    private readonly IValidator<CustomerRegisterRequest> _customerValidator;
    private readonly IValidator<FranchiseeRegisterRequest> _franchiseeValidator;
    private readonly IValidator<VerifyOtpRequest> _otpValidator;
    private readonly IValidator<ResendOtpRequest> _resendValidator;

    public RegistrationController(
        IRegistrationService registration,
        IValidator<CustomerRegisterRequest> customerValidator,
        IValidator<FranchiseeRegisterRequest> franchiseeValidator,
        IValidator<VerifyOtpRequest> otpValidator,
        IValidator<ResendOtpRequest> resendValidator)
    {
        _registration = registration;
        _customerValidator = customerValidator;
        _franchiseeValidator = franchiseeValidator;
        _otpValidator = otpValidator;
        _resendValidator = resendValidator;
    }

    [HttpPost("customer")]
    public async Task<ActionResult<ApiResult<RegistrationStartedResponse>>> StartCustomer(
        [FromBody] CustomerRegisterRequest request, CancellationToken ct)
    {
        var validation = await _customerValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(ApiResult<RegistrationStartedResponse>.Fail("Validation failed.", ToErrors(validation)));

        var result = await _registration.StartCustomerAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("customer/verify-otp")]
    public async Task<ActionResult<ApiResult<RegistrationCompletedResponse>>> VerifyCustomer(
        [FromBody] VerifyOtpRequest request, CancellationToken ct)
    {
        var validation = await _otpValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(ApiResult<RegistrationCompletedResponse>.Fail("Validation failed.", ToErrors(validation)));

        var result = await _registration.VerifyCustomerOtpAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("customer/resend-otp")]
    public async Task<ActionResult<ApiResult<RegistrationStartedResponse>>> ResendCustomer(
        [FromBody] ResendOtpRequest request, CancellationToken ct)
    {
        var validation = await _resendValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(ApiResult<RegistrationStartedResponse>.Fail("Validation failed.", ToErrors(validation)));

        var result = await _registration.ResendCustomerOtpAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("franchisee")]
    public async Task<ActionResult<ApiResult<RegistrationStartedResponse>>> StartFranchisee(
        [FromBody] FranchiseeRegisterRequest request, CancellationToken ct)
    {
        var validation = await _franchiseeValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(ApiResult<RegistrationStartedResponse>.Fail("Validation failed.", ToErrors(validation)));

        var result = await _registration.StartFranchiseeAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("franchisee/verify-otp")]
    public async Task<ActionResult<ApiResult<RegistrationCompletedResponse>>> VerifyFranchisee(
        [FromBody] VerifyOtpRequest request, CancellationToken ct)
    {
        var validation = await _otpValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(ApiResult<RegistrationCompletedResponse>.Fail("Validation failed.", ToErrors(validation)));

        var result = await _registration.VerifyFranchiseeOtpAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("franchisee/resend-otp")]
    public async Task<ActionResult<ApiResult<RegistrationStartedResponse>>> ResendFranchisee(
        [FromBody] ResendOtpRequest request, CancellationToken ct)
    {
        var validation = await _resendValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(ApiResult<RegistrationStartedResponse>.Fail("Validation failed.", ToErrors(validation)));

        var result = await _registration.ResendFranchiseeOtpAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    private static Dictionary<string, string[]> ToErrors(FluentValidation.Results.ValidationResult validation) =>
        validation.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
}
