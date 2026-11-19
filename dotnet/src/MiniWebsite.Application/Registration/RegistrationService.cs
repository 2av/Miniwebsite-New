using MiniWebsite.Application.Admin.EmailTemplates;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Application.Common.Options;
using MiniWebsite.Application.Registration.Dtos;
using MiniWebsite.Domain.Entities;
using MiniWebsite.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using MiniWebsite.Application.Common.Extensions;
using MiniWebsite.Application.Portal;
using MiniWebsite.Domain.Common;

namespace MiniWebsite.Application.Registration;

public interface IRegistrationService
{
    Task<ApiResult<RegistrationStartedResponse>> StartCustomerAsync(CustomerRegisterRequest request, string? clientIp, CancellationToken ct = default);
    Task<ApiResult<RegistrationCompletedResponse>> VerifyCustomerOtpAsync(VerifyOtpRequest request, string? clientIp, CancellationToken ct = default);
    Task<ApiResult<RegistrationStartedResponse>> ResendCustomerOtpAsync(ResendOtpRequest request, CancellationToken ct = default);

    Task<ApiResult<RegistrationStartedResponse>> StartFranchiseeAsync(FranchiseeRegisterRequest request, CancellationToken ct = default);
    Task<ApiResult<RegistrationCompletedResponse>> VerifyFranchiseeOtpAsync(VerifyOtpRequest request, CancellationToken ct = default);
    Task<ApiResult<RegistrationStartedResponse>> ResendFranchiseeOtpAsync(ResendOtpRequest request, CancellationToken ct = default);
}

public class RegistrationService : IRegistrationService
{
    private const string RoleCustomer = "CUSTOMER";
    private const string RoleFranchisee = "FRANCHISEE";

    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILibraryEmailSender _mail;
    private readonly IReferralEarningLookup _earnings;
    private readonly AppOptions _app;

    public RegistrationService(
        IApplicationDbContext db,
        IPasswordHasher passwordHasher,
        ILibraryEmailSender mail,
        IReferralEarningLookup earnings,
        IOptions<AppOptions> app)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _mail = mail;
        _earnings = earnings;
        _app = app.Value;
    }

    public Task<ApiResult<RegistrationStartedResponse>> StartCustomerAsync(CustomerRegisterRequest request, string? clientIp, CancellationToken ct = default) =>
        StartAsync(RoleCustomer, request.FirstName, request.LastName, request.Email, request.Phone, request.Password, request.State, request.ReferralCode, ct);

    public Task<ApiResult<RegistrationStartedResponse>> StartFranchiseeAsync(FranchiseeRegisterRequest request, CancellationToken ct = default) =>
        StartAsync(RoleFranchisee, request.FirstName, request.LastName, request.Email, request.Phone, request.Password, state: null, request.ReferralCode, ct);

    public Task<ApiResult<RegistrationStartedResponse>> ResendCustomerOtpAsync(ResendOtpRequest request, CancellationToken ct = default) =>
        ResendAsync(RoleCustomer, request.Email, ct);

    public Task<ApiResult<RegistrationStartedResponse>> ResendFranchiseeOtpAsync(ResendOtpRequest request, CancellationToken ct = default) =>
        ResendAsync(RoleFranchisee, request.Email, ct);

    public async Task<ApiResult<RegistrationCompletedResponse>> VerifyCustomerOtpAsync(VerifyOtpRequest request, string? clientIp, CancellationToken ct = default)
    {
        var pending = await GetValidPendingAsync(RoleCustomer, request.Email, request.Otp, ct);
        if (pending == null)
            return ApiResult<RegistrationCompletedResponse>.Fail("Invalid or expired OTP. Please try again.");

        var uniqueness = await EnsureUniqueAsync(pending.Email, pending.Phone, ct);
        if (uniqueness != null)
            return ApiResult<RegistrationCompletedResponse>.Fail(uniqueness);

        var hash = pending.PasswordHash;
        var referralCode = GenerateReferralCode(pending.Email);
        var user = new User
        {
            AssignedRole = await _db.GetRoleByPrefixAsync(RolePrefixes.ShareBizzUser, ct),
            Email = pending.Email,
            Phone = pending.Phone,
            FirstName = pending.FirstName,
            LastName = pending.LastName,
            Address = new UserAddress { State = pending.State },
            Password = hash,
            Ip = clientIp,
            Status = "ACTIVE",
            OnboardingStage = OnboardingStage.PaymentDue,
            OnboardingStageAt = AppClock.Now,
            Referral = new UserReferralSettings
            {
                ReferralCode = referralCode,
                ReferredBy = pending.ReferrerEmail ?? "",
                ReferrerUserId = pending.ReferrerUserId,
                CollaborationEnabled = false,
                SaleskitEnabled = false,
                Influencer = false,
            },
            CreatedAt = AppClock.Now,
            UpdatedAt = AppClock.Now,
            IsDeleted = false
        };

        _db.Users.Add(user);
        var issuedPassword = string.IsNullOrWhiteSpace(pending.IssuedPassword)
            ? RegistrationEmailTemplates.PasswordHint
            : pending.IssuedPassword;
        pending.IssuedPassword = null;
        pending.IsConsumed = true;
        pending.IsDeleted = true;
        await _db.SaveChangesAsync(ct);

        if (!string.IsNullOrWhiteSpace(pending.ReferrerEmail))
        {
            var referrerId = pending.ReferrerUserId ?? await FindUserIdAsync(pending.ReferrerEmail, ct);
            var amount = await _earnings.GetSignupEarningAsync(referrerId, PlanCommissionValues.CategoryDbProfile, ct);
            _db.ReferralEarnings.Add(new ReferralEarning
            {
                ReferrerUserId = referrerId,
                ReferredUserId = user.Id,
                ReferrerEmail = pending.ReferrerEmail,
                ReferredEmail = pending.Email,
                ReferralDate = AppClock.Now,
                Status = "Pending",
                Amount = amount,
                IsCollaboration = "NO"
            });
            await _db.SaveChangesAsync(ct);
        }

        await _mail.TrySendAsync(EmailLibraryCatalog.RegistrationWelcome, pending.Email, new Dictionary<string, string?>
        {
            ["first_name"] = user.FirstName,
            ["user_type"] = user.AssignedRole?.RoleName ?? "ShareBizz User",
            ["next_steps_block"] = EmailLibraryMessages.Paragraph(
                "Complete any pending onboarding steps shown in your Dashboard."),
            ["login_id"] = pending.Email,
            ["password"] = issuedPassword,
            ["login_link"] = _app.CustomerDashboardUrl,
            ["payment_block"] = "",
        }, ct: ct);

        return ApiResult<RegistrationCompletedResponse>.Ok(new RegistrationCompletedResponse(
            user.Id, user.Email, user.FullName, RoleCustomer, referralCode), "Registration completed.");
    }

    public async Task<ApiResult<RegistrationCompletedResponse>> VerifyFranchiseeOtpAsync(VerifyOtpRequest request, CancellationToken ct = default)
    {
        var pending = await GetValidPendingAsync(RoleFranchisee, request.Email, request.Otp, ct);
        if (pending == null)
            return ApiResult<RegistrationCompletedResponse>.Fail("Invalid or expired OTP. Please try again.");

        var uniqueness = await EnsureUniqueAsync(pending.Email, pending.Phone, ct);
        if (uniqueness != null)
            return ApiResult<RegistrationCompletedResponse>.Fail(uniqueness);

        var hash = pending.PasswordHash;
        var referralCode = GenerateReferralCode(pending.Email);
        var user = new User
        {
            AssignedRole = await _db.GetRoleByPrefixAsync(RolePrefixes.FranchisePartner, ct),
            Email = pending.Email,
            Phone = pending.Phone,
            FirstName = pending.FirstName,
            LastName = pending.LastName,
            Password = hash,
            Status = "ACTIVE",
            OnboardingStage = OnboardingStage.PaymentDue,
            OnboardingStageAt = AppClock.Now,
            Referral = new UserReferralSettings
            {
                ReferralCode = referralCode,
                ReferredBy = pending.ReferrerEmail ?? "",
                ReferrerUserId = pending.ReferrerUserId,
                CollaborationEnabled = false,
                SaleskitEnabled = false,
                Influencer = false,
            },
            CreatedAt = AppClock.Now,
            UpdatedAt = AppClock.Now,
            IsDeleted = false
        };

        _db.Users.Add(user);
        var issuedPassword = string.IsNullOrWhiteSpace(pending.IssuedPassword)
            ? RegistrationEmailTemplates.PasswordHint
            : pending.IssuedPassword;
        pending.IssuedPassword = null;
        pending.IsConsumed = true;
        pending.IsDeleted = true;
        await _db.SaveChangesAsync(ct);

        if (!string.IsNullOrWhiteSpace(pending.ReferrerEmail))
        {
            var referrerId = pending.ReferrerUserId ?? await FindUserIdAsync(pending.ReferrerEmail, ct);
            var amount = await _earnings.GetSignupEarningAsync(referrerId, PlanCommissionValues.CategoryFranchise, ct);
            _db.ReferralEarnings.Add(new ReferralEarning
            {
                ReferrerUserId = referrerId,
                ReferredUserId = user.Id,
                ReferrerEmail = pending.ReferrerEmail,
                ReferredEmail = pending.Email,
                ReferralDate = AppClock.Now,
                Status = "Pending",
                Amount = amount,
                IsCollaboration = "YES"
            });
            await _db.SaveChangesAsync(ct);
        }

        var payUrl = $"{_app.MembershipPaymentUrl}?email={Uri.EscapeDataString(pending.Email)}";
        await _mail.TrySendAsync(EmailLibraryCatalog.RegistrationWelcome, pending.Email, new Dictionary<string, string?>
        {
            ["first_name"] = user.FirstName,
            ["user_type"] = user.AssignedRole?.RoleName ?? "Franchise Partner",
            ["next_steps_block"] = EmailLibraryMessages.Paragraph(
                "Pay the applicable membership fee, then complete document verification from your Dashboard."),
            ["login_id"] = pending.Email,
            ["password"] = issuedPassword,
            ["login_link"] = _app.CustomerDashboardUrl,
            ["payment_block"] = EmailLibraryMessages.PaymentBlock(payUrl, OnboardingAccess.MembershipFeeLabel(RolePrefixes.FranchisePartner)),
        }, ct: ct);

        return ApiResult<RegistrationCompletedResponse>.Ok(new RegistrationCompletedResponse(
            user.Id, user.Email, user.FullName, RoleFranchisee, referralCode), "Franchisee registration completed.");
    }

    private async Task<ApiResult<RegistrationStartedResponse>> StartAsync(
        string role, string firstName, string? lastName, string email, string phone, string password, string? state, string? referralCode, CancellationToken ct)
    {
        email = email.Trim().ToLowerInvariant();
        phone = phone.Trim();
        firstName = firstName.Trim();
        lastName = (lastName ?? "").Trim();
        var name = $"{firstName} {lastName}".Trim();

        var uniqueness = await EnsureUniqueAsync(email, phone, ct);
        if (uniqueness != null)
            return ApiResult<RegistrationStartedResponse>.Fail(uniqueness);

        var (referrerEmail, referrerId) = await ResolveReferrerAsync(referralCode, ct);
        var otp = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        var hash = _passwordHasher.Hash(password);

        // Invalidate previous pending for same email+role
        var old = await _db.RegistrationPendings
            .Where(p => p.Email == email && p.Role == role && !p.IsConsumed)
            .ToListAsync(ct);
        foreach (var p in old)
        {
            p.IsDeleted = true;
            p.IsConsumed = true;
        }

        var pending = new RegistrationPending
        {
            Role = role,
            Email = email,
            Phone = phone,
            FirstName = firstName,
            LastName = lastName,
            State = state,
            PasswordHash = hash,
            IssuedPassword = password,
            ReferrerEmail = referrerEmail,
            ReferrerUserId = referrerId,
            Otp = otp,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            CreatedAt = AppClock.Now,
            IsDeleted = false,
            IsConsumed = false
        };
        _db.RegistrationPendings.Add(pending);
        await _db.SaveChangesAsync(ct);

        try
        {
            await _mail.SendAsync(EmailLibraryCatalog.EmailVerificationOtp, email, new Dictionary<string, string?>
            {
                ["first_name"] = string.IsNullOrWhiteSpace(firstName) ? EmailLibraryMessages.FirstWord(name) : firstName,
                ["otp"] = otp,
                ["otp_validity"] = "10 minutes",
            }, ct: ct);
        }
        catch (Exception)
        {
            pending.IsDeleted = true;
            await _db.SaveChangesAsync(ct);
            return ApiResult<RegistrationStartedResponse>.Fail("Error sending OTP email. Please try again.");
        }

        return ApiResult<RegistrationStartedResponse>.Ok(
            new RegistrationStartedResponse(email, "OTP has been sent to your email. Please verify to complete registration."),
            "OTP sent.");
    }

    private async Task<ApiResult<RegistrationStartedResponse>> ResendAsync(string role, string email, CancellationToken ct)
    {
        email = email.Trim().ToLowerInvariant();
        var pending = await _db.RegistrationPendings
            .Where(p => p.Email == email && p.Role == role && !p.IsConsumed)
            .OrderByDescending(p => p.Id)
            .FirstOrDefaultAsync(ct);

        if (pending == null)
            return ApiResult<RegistrationStartedResponse>.Fail("No pending registration found. Please start registration again.");

        pending.Otp = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        pending.ExpiresAt = DateTime.UtcNow.AddMinutes(10);
        pending.UpdatedAt = AppClock.Now;
        await _db.SaveChangesAsync(ct);

        try
        {
            await _mail.SendAsync(EmailLibraryCatalog.EmailVerificationOtp, email, new Dictionary<string, string?>
            {
                ["first_name"] = string.IsNullOrWhiteSpace(pending.FirstName) ? "there" : pending.FirstName,
                ["otp"] = pending.Otp,
                ["otp_validity"] = "10 minutes",
            }, ct: ct);
        }
        catch
        {
            return ApiResult<RegistrationStartedResponse>.Fail("Error sending OTP email. Please try again.");
        }

        return ApiResult<RegistrationStartedResponse>.Ok(
            new RegistrationStartedResponse(email, $"New OTP has been sent on your registered email ({email})."),
            "OTP resent.");
    }

    private async Task<RegistrationPending?> GetValidPendingAsync(string role, string email, string otp, CancellationToken ct)
    {
        email = email.Trim().ToLowerInvariant();
        otp = otp.Trim();
        var pending = await _db.RegistrationPendings
            .Where(p => p.Email == email && p.Role == role && !p.IsConsumed)
            .OrderByDescending(p => p.Id)
            .FirstOrDefaultAsync(ct);

        if (pending == null) return null;
        if (pending.ExpiresAt < DateTime.UtcNow) return null;
        if (!string.Equals(pending.Otp, otp, StringComparison.Ordinal)) return null;
        return pending;
    }

    private async Task<string?> EnsureUniqueAsync(string email, string phone, CancellationToken ct)
    {
        var byEmail = await _db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && !u.IsDeleted, ct);
        if (byEmail != null)
            return $"This email address is already registered as a {RoleLabel(byEmail.LoginRole)}. Please use a different email.";

        var byPhone = await _db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Phone == phone && !u.IsDeleted, ct);
        if (byPhone != null)
            return $"This mobile number is already registered as a {RoleLabel(byPhone.LoginRole)}. Please use a different mobile number.";

        return null;
    }

    private async Task<(string? Email, int? Id)> ResolveReferrerAsync(string? referralCode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(referralCode)) return (null, null);
        var code = referralCode.Trim().ToUpperInvariant();
        var referrer = await _db.Users
            .FirstOrDefaultAsync(u => u.Referral.ReferralCode != null && u.Referral.ReferralCode.ToUpper() == code, ct);
        return (referrer?.Email, referrer?.Id);
    }

    private static string RoleLabel(UserRole role) => role switch
    {
        UserRole.Customer => "Customer",
        UserRole.Franchisee => "Franchisee",
        UserRole.Team => "Team",
        UserRole.Admin => "Admin",
        _ => "user"
    };

    private Task<int?> FindUserIdAsync(string email, CancellationToken ct)
    {
        var key = email.Trim();
        return _db.Users.AsNoTracking()
            .Where(u => u.Email == key)
            .Select(u => (int?)u.Id)
            .FirstOrDefaultAsync(ct);
    }

    private static string GenerateReferralCode(string email) =>
        Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(email + AppClock.Now.Ticks)))[..8];
}
