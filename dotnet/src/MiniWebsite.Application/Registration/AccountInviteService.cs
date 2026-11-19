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

public record PartnerInvitePreview(string Email, string RolePrefix, string RoleName, string? ReferralCode);

public record CompletePartnerInviteRequest(
    string Token,
    string FirstName,
    string? LastName,
    string Phone,
    string Password,
    string? State = null,
    string? ReferralCode = null);

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
    private readonly ILibraryEmailSender _mail;
    private readonly IReferralEarningLookup _earnings;
    private readonly AppOptions _app;

    public AccountInviteService(
        IApplicationDbContext db,
        IPasswordHasher passwords,
        ILibraryEmailSender mail,
        IReferralEarningLookup earnings,
        IOptions<AppOptions> app)
    {
        _db = db;
        _passwords = passwords;
        _mail = mail;
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
        if (prefix is not (
            RolePrefixes.FranchisePartner
            or RolePrefixes.PrintShopPartner
            or RolePrefixes.DigitalMarketingPartner
            or RolePrefixes.SocialMediaPartner
            or RolePrefixes.FieldSalesPartner
            or RolePrefixes.FieldSalesTeam))
            return ApiResult.Fail("This role does not use an account-creation link.");

        if (await _db.Users.AnyAsync(u => u.Email.ToLower() == email, ct))
            return ApiResult.Fail("This email address is already registered.");

        await EnsureReferrerCodeAsync(invitedByUserId, ct);

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
            ExpiresAt = AppClock.Now.AddHours(48),
            IsConsumed = false,
            CreatedAt = AppClock.Now,
        });
        await _db.SaveChangesAsync(ct);

        var link = _app.SiteBaseUrl.TrimEnd('/') + "/registration/customer?token=" + Uri.EscapeDataString(token);
        try
        {
            await _mail.SendAsync(EmailLibraryCatalog.RegistrationInvitation, email, new Dictionary<string, string?>
            {
                ["first_name"] = "there",
                ["user_type"] = role.RoleName,
                ["registration_link"] = link,
                ["invitation_context"] = EmailLibraryMessages.Paragraph(
                    "This link is valid for 48 hours. After you submit the form, login details and the next steps will be emailed to you."),
            }, ct: ct);
        }
        catch
        {
            return ApiResult.Fail("The registration link could not be emailed.");
        }

        return ApiResult.Ok("Registration link sent to " + email + ". It is valid for 48 hours.");
    }

    public async Task<ApiResult> SendFranchiseRegistrationLinkAsync(string email, string name, CancellationToken ct = default)
    {
        email = (email ?? "").Trim().ToLowerInvariant();
        if (email.Length == 0 || !email.Contains('@'))
            return ApiResult.Fail("A valid email address is required.");
        if (await _db.Users.AnyAsync(u => u.Email.ToLower() == email, ct))
            return ApiResult.Fail("This email address is already registered.");

        var link = _app.SiteBaseUrl.TrimEnd('/') + "/registration/franchisee?email=" + Uri.EscapeDataString(email);
        var greeting = string.IsNullOrWhiteSpace(name) ? "there" : name.Trim();
        try
        {
            await _mail.SendAsync(EmailLibraryCatalog.RegistrationInvitation, email, new Dictionary<string, string?>
            {
                ["first_name"] = EmailLibraryMessages.FirstWord(greeting),
                ["user_type"] = "Franchise Partner",
                ["registration_link"] = link,
                ["invitation_context"] = EmailLibraryMessages.Paragraph(
                    "After you open the link, verify the email OTP. Login details and the next steps will follow."),
            }, ct: ct);
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
        var referralCode = await EnsureReferrerCodeAsync(invite.InvitedByUserId, ct);
        return ApiResult<PartnerInvitePreview>.Ok(new PartnerInvitePreview(invite.Email, invite.RolePrefix, roleName, referralCode));
    }

    public async Task<ApiResult> CompleteAsync(CompletePartnerInviteRequest request, CancellationToken ct = default)
    {
        var invite = await FindAsync(request.Token, ct);
        if (invite == null) return ApiResult.Fail("This registration link is invalid or has expired.");

        var first = (request.FirstName ?? "").Trim();
        var last = (request.LastName ?? "").Trim();
        var phone = (request.Phone ?? "").Trim();
        var state = (request.State ?? "").Trim();
        var password = request.Password ?? "";
        if (first.Length == 0) return ApiResult.Fail("First name is required.");
        if (phone.Length != 10 || phone.Any(c => !char.IsDigit(c)))
            return ApiResult.Fail("Valid 10-digit mobile number is required.");
        if (state.Length == 0) return ApiResult.Fail("Please select a valid State.");
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
            Address = new UserAddress { State = state },
            Password = _passwords.Hash(password),
            Status = "ACTIVE",
            OnboardingStage = OnboardingAccess.PaysMembership(invite.RolePrefix)
                ? OnboardingStage.PaymentDue
                : OnboardingStage.ReadyToSell,
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
        else if (!string.IsNullOrWhiteSpace(request.ReferralCode))
        {
            var code = request.ReferralCode.Trim().ToUpperInvariant();
            var referrer = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Referral.ReferralCode != null && u.Referral.ReferralCode.ToUpper() == code, ct);
            if (referrer != null)
            {
                user.Referral.ReferrerUserId = referrer.Id;
                user.Referral.ReferredBy = referrer.Email;
            }
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
        await SendProspectLoginAsync(user, password, owner, ct);
        return ApiResult.Ok("Print Shop Partner account created. Login credentials were emailed.");
    }

    private Task<bool> SendProspectLoginAsync(User user, string password, User owner, CancellationToken ct)
    {
        var pay = _app.MembershipPaymentUrl + "?email=" + Uri.EscapeDataString(user.Email);
        return _mail.TrySendAsync(EmailLibraryCatalog.ProspectLoginPayment, user.Email, new Dictionary<string, string?>
        {
            ["first_name"] = string.IsNullOrWhiteSpace(user.FirstName) ? "there" : user.FirstName,
            ["registration_type"] = user.AssignedRole?.RoleName ?? "Print Shop Partner",
            ["fst_name"] = string.IsNullOrWhiteSpace(owner.FullName) ? "a colleague" : owner.FullName,
            ["login_id"] = user.Email,
            ["password"] = password,
            ["login_link"] = _app.FranchiseeLoginUrl,
            ["payment_link"] = pay,
            ["plan_name"] = OnboardingAccess.MembershipFeeLabel(user.AssignedRole?.Prefix),
            ["amount_block"] = "",
        }, ct: ct);
    }

    private async Task SendCredentialsAsync(User user, string password, CancellationToken ct)
    {
        var payment = "";
        if (OnboardingAccess.PaysMembership(user.AssignedRole?.Prefix))
        {
            var pay = _app.MembershipPaymentUrl + "?email=" + Uri.EscapeDataString(user.Email);
            payment = EmailLibraryMessages.PaymentBlock(pay, OnboardingAccess.MembershipFeeLabel(user.AssignedRole?.Prefix));
        }

        await _mail.TrySendAsync(EmailLibraryCatalog.AccountCredentials, user.Email, new Dictionary<string, string?>
        {
            ["first_name"] = string.IsNullOrWhiteSpace(user.FirstName) ? "there" : user.FirstName,
            ["user_type"] = user.AssignedRole?.RoleName ?? "partner",
            ["login_id"] = user.Email,
            ["password"] = password,
            ["payment_block"] = payment,
            ["login_link"] = _app.FranchiseeLoginUrl,
        }, ct: ct);
    }

    private async Task<string?> EnsureReferrerCodeAsync(int? userId, CancellationToken ct)
    {
        if (userId is not int id) return null;
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user == null) return null;
        if (string.IsNullOrWhiteSpace(user.Referral.ReferralCode))
        {
            user.Referral.ReferralCode = ReferralCode(user.Email);
            user.UpdatedAt = AppClock.Now;
            await _db.SaveChangesAsync(ct);
        }
        return user.Referral.ReferralCode;
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
