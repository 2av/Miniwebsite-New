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
    private readonly IEmailSender _email;
    private readonly IEmailTemplateRenderer _templates;
    private readonly IDealBonusLookup _deals;
    private readonly AppOptions _app;

    public RegistrationService(
        IApplicationDbContext db,
        IPasswordHasher passwordHasher,
        IEmailSender email,
        IEmailTemplateRenderer templates,
        IDealBonusLookup deals,
        IOptions<AppOptions> app)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _email = email;
        _templates = templates;
        _deals = deals;
        _app = app.Value;
    }

    public Task<ApiResult<RegistrationStartedResponse>> StartCustomerAsync(CustomerRegisterRequest request, string? clientIp, CancellationToken ct = default) =>
        StartAsync(RoleCustomer, request.Name, request.Email, request.Phone, request.Password, request.State, request.ReferralCode, ct);

    public Task<ApiResult<RegistrationStartedResponse>> StartFranchiseeAsync(FranchiseeRegisterRequest request, CancellationToken ct = default) =>
        StartAsync(RoleFranchisee, request.Name, request.Email, request.Phone, request.Password, state: null, request.ReferralCode, ct);

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
            Role = UserRole.Customer,
            Email = pending.Email,
            Phone = pending.Phone,
            Name = pending.Name,
            State = pending.State,
            Password = hash,
            PasswordHash = hash,
            Ip = clientIp,
            Status = "ACTIVE",
            ReferralCode = referralCode,
            ReferredBy = pending.ReferrerEmail ?? "",
            CollaborationEnabled = false,
            SaleskitEnabled = false,
            Influencer = false,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            IsDeleted = false
        };

        _db.Users.Add(user);
        pending.IsConsumed = true;
        pending.IsDeleted = true;
        await _db.SaveChangesAsync(ct);

        if (!string.IsNullOrWhiteSpace(pending.ReferrerEmail))
        {
            var amount = await _deals.GetBonusAmountAsync(pending.ReferrerEmail, "MiniWebsite", "DMW001", ct);
            _db.ReferralEarnings.Add(new ReferralEarning
            {
                ReferrerEmail = pending.ReferrerEmail,
                ReferredEmail = pending.Email,
                ReferralDate = DateTime.Now,
                Status = "Pending",
                Amount = amount,
                IsCollaboration = "NO"
            });
            await _db.SaveChangesAsync(ct);
        }

        try
        {
            var vars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["name"] = pending.Name,
                ["email"] = pending.Email,
                ["password"] = pending.PlainPassword,
                ["host"] = _app.PublicHost,
                ["dashboard_url"] = _app.CustomerDashboardUrl,
            };
            var rendered = await _templates.TryRenderAsync("customer_welcome", vars, ct);
            var (subject, html) = rendered ?? RegistrationEmailTemplates.CustomerWelcome(
                pending.Name, pending.Email, pending.PlainPassword, _app);
            await _email.SendAsync(pending.Email, subject, html, ct);
        }
        catch
        {
            // Match PHP: registration succeeds even if welcome email fails
        }

        return ApiResult<RegistrationCompletedResponse>.Ok(new RegistrationCompletedResponse(
            user.Id, user.Email, user.Name, RoleCustomer, referralCode), "Registration completed.");
    }

    public async Task<ApiResult<RegistrationCompletedResponse>> VerifyFranchiseeOtpAsync(VerifyOtpRequest request, CancellationToken ct = default)
    {
        var pending = await GetValidPendingAsync(RoleFranchisee, request.Email, request.Otp, ct);
        if (pending == null)
            return ApiResult<RegistrationCompletedResponse>.Fail("Invalid or expired OTP. Please try again.");

        var uniqueness = await EnsureUniqueAsync(pending.Email, pending.Phone, ct);
        if (uniqueness != null)
            return ApiResult<RegistrationCompletedResponse>.Fail(uniqueness);

        // Franchisee: hash again at insert (same as PHP)
        var hash = _passwordHasher.Hash(pending.PlainPassword);
        var referralCode = GenerateReferralCode(pending.Email);
        var user = new User
        {
            Role = UserRole.Franchisee,
            Email = pending.Email,
            Phone = pending.Phone,
            Name = pending.Name,
            Password = hash,
            PasswordHash = hash,
            Status = "ACTIVE",
            ReferralCode = referralCode,
            ReferredBy = pending.ReferrerEmail ?? "",
            CollaborationEnabled = false,
            SaleskitEnabled = false,
            Influencer = false,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            IsDeleted = false
        };

        _db.Users.Add(user);
        pending.IsConsumed = true;
        pending.IsDeleted = true;
        await _db.SaveChangesAsync(ct);

        if (!string.IsNullOrWhiteSpace(pending.ReferrerEmail))
        {
            var amount = await _deals.GetBonusAmountAsync(pending.ReferrerEmail, "Franchisee", "DFRAN101", ct);
            _db.ReferralEarnings.Add(new ReferralEarning
            {
                ReferrerEmail = pending.ReferrerEmail,
                ReferredEmail = pending.Email,
                ReferralDate = DateTime.Now,
                Status = "Pending",
                Amount = amount,
                IsCollaboration = "YES"
            });
            await _db.SaveChangesAsync(ct);
        }

        try
        {
            var payUrl = $"{_app.FranchiseAgreementUrl}?email={Uri.EscapeDataString(pending.Email)}";
            var vars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["name"] = pending.Name,
                ["email"] = pending.Email,
                ["password"] = pending.PlainPassword,
                ["login_url"] = _app.FranchiseeLoginUrl,
                ["payment_url"] = payUrl,
                ["host"] = _app.PublicHost,
            };
            var rendered = await _templates.TryRenderAsync("franchisee_welcome", vars, ct);
            if (rendered == null)
                rendered = await _templates.TryRenderAsync("franchisee_welcome_no_payment", vars, ct);
            var (subject, html) = rendered ?? RegistrationEmailTemplates.FranchiseeWelcome(
                pending.Name, pending.Email, pending.PlainPassword, _app, includePaymentStep: true);
            await _email.SendAsync(pending.Email, subject, html, ct);
        }
        catch
        {
            // registration still succeeds
        }

        return ApiResult<RegistrationCompletedResponse>.Ok(new RegistrationCompletedResponse(
            user.Id, user.Email, user.Name, RoleFranchisee, referralCode), "Franchisee registration completed.");
    }

    private async Task<ApiResult<RegistrationStartedResponse>> StartAsync(
        string role, string name, string email, string phone, string password, string? state, string? referralCode, CancellationToken ct)
    {
        email = email.Trim().ToLowerInvariant();
        phone = phone.Trim();
        name = name.Trim();

        var uniqueness = await EnsureUniqueAsync(email, phone, ct);
        if (uniqueness != null)
            return ApiResult<RegistrationStartedResponse>.Fail(uniqueness);

        var referrerEmail = await ResolveReferrerEmailAsync(referralCode, ct);
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
            Name = name,
            State = state,
            PasswordHash = hash,
            PlainPassword = password,
            ReferrerEmail = referrerEmail,
            Otp = otp,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false,
            IsConsumed = false
        };
        _db.RegistrationPendings.Add(pending);
        await _db.SaveChangesAsync(ct);

        try
        {
            var vars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["name"] = name,
                ["otp"] = otp,
                ["host"] = _app.PublicHost,
            };
            var rendered = await _templates.TryRenderAsync("registration_otp", vars, ct);
            var (subject, html) = rendered ?? RegistrationEmailTemplates.OtpEmail(name, otp, _app.PublicHost);
            await _email.SendAsync(email, subject, html, ct);
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
        pending.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        try
        {
            var vars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["name"] = pending.Name,
                ["otp"] = pending.Otp,
                ["host"] = _app.PublicHost,
            };
            var rendered = await _templates.TryRenderAsync("registration_otp", vars, ct);
            var (subject, html) = rendered ?? RegistrationEmailTemplates.OtpEmail(pending.Name, pending.Otp, _app.PublicHost);
            await _email.SendAsync(email, subject, html, ct);
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
            return $"This email address is already registered as a {RoleLabel(byEmail.Role)}. Please use a different email.";

        var byPhone = await _db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Phone == phone && !u.IsDeleted, ct);
        if (byPhone != null)
            return $"This mobile number is already registered as a {RoleLabel(byPhone.Role)}. Please use a different mobile number.";

        return null;
    }

    private async Task<string?> ResolveReferrerEmailAsync(string? referralCode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(referralCode)) return null;
        var code = referralCode.Trim().ToUpperInvariant();
        var referrer = await _db.Users
            .FirstOrDefaultAsync(u => u.ReferralCode != null && u.ReferralCode.ToUpper() == code, ct);
        return referrer?.Email;
    }

    private static string RoleLabel(UserRole role) => role switch
    {
        UserRole.Customer => "Customer",
        UserRole.Franchisee => "Franchisee",
        UserRole.Team => "Team",
        UserRole.Admin => "Admin",
        _ => "user"
    };

    private static string GenerateReferralCode(string email) =>
        Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(email + DateTime.Now.Ticks)))[..8];
}
