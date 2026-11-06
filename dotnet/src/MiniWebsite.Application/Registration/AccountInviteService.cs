using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MiniWebsite.Application.Admin.EmailTemplates;
using MiniWebsite.Application.Common.Extensions;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Application.Common.Options;
using MiniWebsite.Application.Portal;
using MiniWebsite.Domain.Common;
using MiniWebsite.Domain.Entities;

namespace MiniWebsite.Application.Registration;

public interface IAccountInviteService
{
    Task<ApiResult> SendPartnerInviteAsync(string email, string rolePrefix, int? invitedByUserId, CancellationToken ct = default);
    Task<ApiResult> SendFranchiseRegistrationLinkAsync(string email, string name, CancellationToken ct = default);
    Task<ApiResult<PartnerInvitePreview>> PreviewAsync(string token, CancellationToken ct = default);
    Task<ApiResult> CompleteAsync(CompletePartnerInviteRequest request, CancellationToken ct = default);
    Task<ApiResult> RegisterPrintShopAsync(RegisterPrintShopRequest request, CancellationToken ct = default);
}

public record PartnerInvitePreview(string Email, string RolePrefix, string RoleName);

public record CompletePartnerInviteRequest(
    string Token,
    string FirstName,
    string? LastName,
    string Phone,
    string Password);

public record RegisterPrintShopRequest(
    string OwnerEmail,
    string FirstName,
    string? LastName,
    string Email,
    string Phone);

public class AccountInviteService : IAccountInviteService
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwords;
    private readonly IEmailSender _email;
    private readonly IEmailTemplateRenderer _templates;
    private readonly IReferralEarningLookup _earnings;
    private readonly AppOptions _app;

    public AccountInviteService(
        IApplicationDbContext db,
        IPasswordHasher passwords,
        IEmailSender email,
        IEmailTemplateRenderer templates,
        IReferralEarningLookup earnings,
        IOptions<AppOptions> app)
    {
        _db = db;
        _passwords = passwords;
        _email = email;
        _templates = templates;
        _earnings = earnings;
        _app = app.Value;
    }

    public async Task<ApiResult> SendPartnerInviteAsync(
        string email, string rolePrefix, int? invitedByUserId, CancellationToken ct = default)
    {
        email = (email ?? "").Trim().ToLowerInvariant();
        var prefix = (rolePrefix ?? "").Trim().ToUpperInvariant();
        if (email.Length == 0 || !email.Contains('@'))
            return ApiResult.Fail("A valid email address is required.");
        if (prefix is not (RolePrefixes.DigitalMarketingPartner or RolePrefixes.FieldSalesPartner or RolePrefixes.PrintShopPartner))
            return ApiResult.Fail("This role does not use an account-creation link.");

        if (await _db.Users.AnyAsync(u => u.Email.ToLower() == email, ct))
            return ApiResult.Fail("This email address is already registered.");

        var role = await _db.Roles.AsNoTracking()
            .Where(r => r.Prefix == prefix)
            .OrderBy(r => r.RoleId)
            .FirstOrDefaultAsync(ct);
        if (role == null) return ApiResult.Fail("Role was not found.");

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
        var old = await _db.AccountInvites.Where(i => i.Email == email && i.RolePrefix == prefix && !i.IsConsumed).ToListAsync(ct);
        foreach (var row in old)
        {
            row.IsConsumed = true;
            row.IsDeleted = true;
        }

        _db.AccountInvites.Add(new AccountInvite
        {
            Email = email,
            RolePrefix = prefix,
            TokenHash = Hash(token),
            InvitedByUserId = invitedByUserId,
            ExpiresAt = AppClock.Now.AddDays(7),
            IsConsumed = false,
            CreatedAt = AppClock.Now,
        });
        await _db.SaveChangesAsync(ct);

        var link = _app.SiteBaseUrl.TrimEnd('/') + "/registration/partner?token=" + Uri.EscapeDataString(token);
        var html = $"""
            <p>Hello,</p>
            <p>Finish your {WebUtility.HtmlEncode(role.RoleName)} account with the registration form:</p>
            <p><a href="{WebUtility.HtmlEncode(link)}">{WebUtility.HtmlEncode(link)}</a></p>
            <p>After you submit the form, login credentials and the next steps will be emailed to you.</p>
            """;
        try
        {
            await _email.SendAsync(email, "Complete your ShareBizz registration", html, ct);
        }
        catch
        {
            return ApiResult.Fail("The registration link could not be emailed.");
        }

        return ApiResult.Ok("Registration link sent to " + email + ".");
    }

    public async Task<ApiResult> SendFranchiseRegistrationLinkAsync(string email, string name, CancellationToken ct = default)
    {
        email = (email ?? "").Trim().ToLowerInvariant();
        if (email.Length == 0 || !email.Contains('@'))
            return ApiResult.Fail("A valid email address is required.");
        if (await _db.Users.AnyAsync(u => u.Email.ToLower() == email, ct))
            return ApiResult.Fail("This email address is already registered.");

        var link = _app.SiteBaseUrl.TrimEnd('/') + "/registration/franchisee?email=" + Uri.EscapeDataString(email);
        var safeName = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(name) ? "there" : name.Trim());
        var html = $"""
            <p>Hello {safeName},</p>
            <p>Create your Franchise Partner account, then verify the email OTP. Login credentials and the next steps will follow.</p>
            <p><a href="{WebUtility.HtmlEncode(link)}">{WebUtility.HtmlEncode(link)}</a></p>
            """;
        try
        {
            await _email.SendAsync(email, "Create your Franchise Partner account", html, ct);
        }
        catch
        {
            return ApiResult.Fail("The registration link could not be emailed.");
        }

        return ApiResult.Ok("Registration link sent to " + email + ". They will verify with email OTP.");
    }

    public async Task<ApiResult<PartnerInvitePreview>> PreviewAsync(string token, CancellationToken ct = default)
    {
        var invite = await FindAsync(token, ct);
        if (invite == null) return ApiResult<PartnerInvitePreview>.Fail("This registration link is invalid or has expired.");
        var roleName = await _db.Roles.AsNoTracking()
            .Where(r => r.Prefix == invite.RolePrefix)
            .Select(r => r.RoleName)
            .FirstOrDefaultAsync(ct) ?? invite.RolePrefix;
        return ApiResult<PartnerInvitePreview>.Ok(new PartnerInvitePreview(invite.Email, invite.RolePrefix, roleName));
    }

    public async Task<ApiResult> CompleteAsync(CompletePartnerInviteRequest request, CancellationToken ct = default)
    {
        var invite = await FindAsync(request.Token, ct);
        if (invite == null) return ApiResult.Fail("This registration link is invalid or has expired.");

        var first = (request.FirstName ?? "").Trim();
        var last = (request.LastName ?? "").Trim();
        var phone = (request.Phone ?? "").Trim();
        var password = request.Password ?? "";
        if (first.Length == 0) return ApiResult.Fail("First name is required.");
        if (phone.Length == 0) return ApiResult.Fail("Phone is required.");
        if (password.Length < 6) return ApiResult.Fail("Password must be at least 6 characters long.");
        if (await _db.Users.AnyAsync(u => u.Email.ToLower() == invite.Email, ct))
            return ApiResult.Fail("This email address is already registered.");

        var now = AppClock.Now;
        var user = new User
        {
            AssignedRole = await _db.GetRoleByPrefixAsync(invite.RolePrefix, ct),
            Email = invite.Email,
            Phone = phone,
            FirstName = first,
            LastName = last,
            Password = _passwords.Hash(password),
            Status = "ACTIVE",
            OnboardingStage = OnboardingStage.PaymentDue,
            OnboardingStageAt = now,
            Referral = new UserReferralSettings
            {
                ReferralCode = ReferralCode(invite.Email),
                ReferrerUserId = invite.InvitedByUserId,
                CollaborationEnabled = false,
                SaleskitEnabled = false,
                Influencer = false,
            },
            CreatedAt = now,
            UpdatedAt = now,
        };
        if (invite.InvitedByUserId is int referrerId)
        {
            var referrer = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == referrerId, ct);
            user.Referral.ReferredBy = referrer?.Email ?? "";
        }

        _db.Users.Add(user);
        invite.IsConsumed = true;
        invite.UpdatedAt = now;
        await _db.SaveChangesAsync(ct);

        if (invite.InvitedByUserId is int byId)
        {
            var amount = await _earnings.GetSignupEarningAsync(byId, PlanCommissionValues.CategoryMembership, ct);
            _db.ReferralEarnings.Add(new ReferralEarning
            {
                ReferrerUserId = byId,
                ReferredUserId = user.Id,
                ReferrerEmail = user.Referral.ReferredBy ?? "",
                ReferredEmail = user.Email,
                ReferralDate = now,
                Status = "Pending",
                Amount = amount,
                IsCollaboration = "NO",
            });
            await _db.SaveChangesAsync(ct);
        }

        await SendCredentialsAsync(user, password, ct);
        return ApiResult.Ok("Account created. Login credentials and the next steps were emailed.");
    }

    public async Task<ApiResult> RegisterPrintShopAsync(RegisterPrintShopRequest request, CancellationToken ct = default)
    {
        var ownerKey = (request.OwnerEmail ?? "").Trim().ToLowerInvariant();
        var owner = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == ownerKey, ct);
        if (owner == null) return ApiResult.Fail("User not found.");
        if (!string.Equals(owner.AssignedRole?.Prefix, RolePrefixes.FieldSalesTeam, StringComparison.OrdinalIgnoreCase))
            return ApiResult.Fail("Only Field Sales Team can register a Print Shop Partner from this dashboard.");

        var email = (request.Email ?? "").Trim().ToLowerInvariant();
        var first = (request.FirstName ?? "").Trim();
        var last = (request.LastName ?? "").Trim();
        var phone = (request.Phone ?? "").Trim();
        if (first.Length == 0) return ApiResult.Fail("First name is required.");
        if (email.Length == 0 || !email.Contains('@')) return ApiResult.Fail("A valid email address is required.");
        if (await _db.Users.AnyAsync(u => u.Email.ToLower() == email, ct))
            return ApiResult.Fail("This email address is already registered.");

        var password = GeneratedPassword();
        var now = AppClock.Now;
        var user = new User
        {
            AssignedRole = await _db.GetRoleByPrefixAsync(RolePrefixes.PrintShopPartner, ct),
            Email = email,
            Phone = phone,
            FirstName = first,
            LastName = last,
            Password = _passwords.Hash(password),
            Status = "ACTIVE",
            OnboardingStage = OnboardingStage.PaymentDue,
            OnboardingStageAt = now,
            Referral = new UserReferralSettings
            {
                ReferralCode = ReferralCode(email),
                ReferredBy = owner.Email,
                ReferrerUserId = owner.Id,
                CollaborationEnabled = false,
                SaleskitEnabled = false,
                Influencer = false,
            },
            CreatedAt = now,
            UpdatedAt = now,
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        var amount = await _earnings.GetSignupEarningAsync(owner.Id, PlanCommissionValues.CategoryMembership, ct);
        _db.ReferralEarnings.Add(new ReferralEarning
        {
            ReferrerUserId = owner.Id,
            ReferredUserId = user.Id,
            ReferrerEmail = owner.Email,
            ReferredEmail = email,
            ReferralDate = now,
            Status = "Pending",
            Amount = amount,
            IsCollaboration = "NO",
        });
        await _db.SaveChangesAsync(ct);
        await SendCredentialsAsync(user, password, ct);
        return ApiResult.Ok("Print Shop Partner account created. Login credentials were emailed.");
    }

    private async Task SendCredentialsAsync(User user, string password, CancellationToken ct)
    {
        var pay = _app.MembershipPaymentUrl + "?email=" + Uri.EscapeDataString(user.Email);
        var label = OnboardingAccess.MembershipFeeLabel(user.AssignedRole?.Prefix);
        var html = $"""
            <p>Hello {WebUtility.HtmlEncode(user.FullName)},</p>
            <p>Your account is ready.</p>
            <p><strong>Login email:</strong> {WebUtility.HtmlEncode(user.Email)}<br/>
            <strong>Password:</strong> {WebUtility.HtmlEncode(password)}</p>
            <p>Next step: pay {WebUtility.HtmlEncode(label)} from the dashboard or this link:</p>
            <p><a href="{WebUtility.HtmlEncode(pay)}">{WebUtility.HtmlEncode(pay)}</a></p>
            <p>After payment is confirmed, document verification opens. When the SB team marks your documents Approved, the rest of the dashboard unlocks and you are ready to initiate sales.</p>
            <p><a href="{WebUtility.HtmlEncode(_app.CustomerDashboardUrl)}">Open dashboard</a></p>
            """;
        try
        {
            await _email.SendAsync(user.Email, "Your ShareBizz login credentials", html, ct);
        }
        catch
        {
            // Account exists even when mail fails.
        }
    }

    private async Task<AccountInvite?> FindAsync(string? token, CancellationToken ct)
    {
        var raw = (token ?? "").Trim();
        if (raw.Length == 0) return null;
        var hash = Hash(raw);
        var invite = await _db.AccountInvites.FirstOrDefaultAsync(i => i.TokenHash == hash && !i.IsConsumed, ct);
        if (invite == null || invite.ExpiresAt < AppClock.Now) return null;
        return invite;
    }

    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string ReferralCode(string email) =>
        Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(email + AppClock.Now.Ticks)))[..8];

    public static string GeneratedPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
        return string.Create(10, chars, static (span, alphabet) =>
        {
            for (var i = 0; i < span.Length; i++)
                span[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
        });
    }
}
