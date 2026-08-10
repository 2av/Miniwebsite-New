using FluentValidation;
using MiniWebsite.Application.Registration.Dtos;

namespace MiniWebsite.Application.Registration.Validators;

public static class IndianStates
{
    public static readonly HashSet<string> All = new(StringComparer.Ordinal)
    {
        "Andhra Pradesh", "Arunachal Pradesh", "Assam", "Bihar", "Chhattisgarh", "Goa", "Gujarat",
        "Haryana", "Himachal Pradesh", "Jharkhand", "Karnataka", "Kerala", "Madhya Pradesh",
        "Maharashtra", "Manipur", "Meghalaya", "Mizoram", "Nagaland", "Odisha", "Punjab",
        "Rajasthan", "Sikkim", "Tamil Nadu", "Telangana", "Tripura", "Uttar Pradesh",
        "Uttarakhand", "West Bengal",
        "Andaman and Nicobar Islands", "Chandigarh", "Dadra and Nagar Haveli and Daman and Diu",
        "Delhi", "Jammu and Kashmir", "Ladakh", "Lakshadweep", "Puducherry"
    };
}

public class CustomerRegisterRequestValidator : AbstractValidator<CustomerRegisterRequest>
{
    public CustomerRegisterRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.Phone).NotEmpty().Matches(@"^[0-9]{10}$").WithMessage("Valid 10-digit mobile number is required.");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        RuleFor(x => x.State).NotEmpty().Must(s => IndianStates.All.Contains(s))
            .WithMessage("Please select a valid State.");
    }
}

public class FranchiseeRegisterRequestValidator : AbstractValidator<FranchiseeRegisterRequest>
{
    public FranchiseeRegisterRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.Phone).NotEmpty().Matches(@"^[0-9]{10}$").WithMessage("Valid 10-digit mobile number is required.");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}

public class VerifyOtpRequestValidator : AbstractValidator<VerifyOtpRequest>
{
    public VerifyOtpRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Otp).NotEmpty().Length(6);
    }
}

public class ResendOtpRequestValidator : AbstractValidator<ResendOtpRequest>
{
    public ResendOtpRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
