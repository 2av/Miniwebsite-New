namespace MiniWebsite.Application.Registration.Dtos;

public record CustomerRegisterRequest(
    string Name,
    string Email,
    string Phone,
    string Password,
    string State,
    string? ReferralCode = null);

public record FranchiseeRegisterRequest(
    string Name,
    string Email,
    string Phone,
    string Password,
    string? ReferralCode = null);

public record VerifyOtpRequest(string Email, string Otp);

public record ResendOtpRequest(string Email);

public record RegistrationStartedResponse(string Email, string Message);

public record RegistrationCompletedResponse(
    int UserId,
    string Email,
    string Name,
    string Role,
    string ReferralCode);
