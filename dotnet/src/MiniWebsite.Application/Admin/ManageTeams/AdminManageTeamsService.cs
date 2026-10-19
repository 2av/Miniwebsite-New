using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MiniWebsite.Application.Admin.EmailTemplates;
using MiniWebsite.Application.Admin.ManageTeams.Dtos;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Application.Common.Options;
using MiniWebsite.Domain.Entities;
using MiniWebsite.Domain.Enums;
using MiniWebsite.Application.Common;
using MiniWebsite.Application.Common.Extensions;
using MiniWebsite.Domain.Common;

namespace MiniWebsite.Application.Admin.ManageTeams;

public class AdminManageTeamsService : IAdminManageTeamsService
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailSender _email;
    private readonly IEmailTemplateRenderer _templates;
    private readonly AppOptions _app;

    public AdminManageTeamsService(
        IApplicationDbContext db,
        IPasswordHasher passwordHasher,
        IEmailSender email,
        IEmailTemplateRenderer templates,
        IOptions<AppOptions> app)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _email = email;
        _templates = templates;
        _app = app.Value;
    }

    public async Task<ApiResult<ManageTeamsPageDto>> ListAsync(ManageTeamsQuery query, CancellationToken ct = default)
    {
        query.Page = query.Page < 1 ? 1 : query.Page;
        query.PageSize = query.PageSize is < 1 or > 100 ? 10 : query.PageSize;

        try
        {
            var q = _db.Users.AsNoTracking().WhereRole(UserRole.Team);
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var s = query.Search.Trim().ToLower();
                q = q.Where(u =>
                    u.Email.ToLower().Contains(s)
                    || (u.FirstName + " " + u.LastName).ToLower().Contains(s)
                    || (u.Phone != null && u.Phone.Contains(s))
                    || (u.Address.District != null && u.Address.District.ToLower().Contains(s))
                    || (u.Address.State != null && u.Address.State.ToLower().Contains(s)));
            }

            var status = NormalizeStatusFilter(query.Status);
            if (status != null)
                q = q.Where(u => u.Status == status);

            var total = await q.CountAsync(ct);
            var users = await q
                .OrderByDescending(u => u.CreatedAt)
                .ThenByDescending(u => u.Id)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(ct);

            var rows = new List<ManageTeamRowDto>(users.Count);
            foreach (var u in users)
                rows.Add(await MapRowAsync(u, ct));

            return ApiResult<ManageTeamsPageDto>.Ok(new ManageTeamsPageDto(rows, total, query.Page, query.PageSize));
        }
        catch (Exception ex)
        {
            return ApiResult<ManageTeamsPageDto>.Fail("Unable to load team members: " + ex.Message);
        }
    }

    public async Task<ApiResult<ManageTeamRowDto>> GetAsync(int id, CancellationToken ct = default)
    {
        var user = await FindTeamAsync(id, tracked: false, ct);
        if (user == null) return ApiResult<ManageTeamRowDto>.Fail("Team member not found");
        return ApiResult<ManageTeamRowDto>.Ok(await MapRowAsync(user, ct));
    }

    public async Task<ApiResult<ManageTeamRowDto>> CreateAsync(CreateTeamMemberRequest request, CancellationToken ct = default)
    {
        var firstName = (request.FirstName ?? "").Trim();
        var lastName = (request.LastName ?? "").Trim();
        var email = NormalizeEmail(request.Email);
        var phone = (request.Phone ?? "").Trim();
        var district = (request.District ?? "").Trim();
        var state = (request.State ?? "").Trim();
        var password = request.Password ?? "";

        if (firstName.Length == 0) return ApiResult<ManageTeamRowDto>.Fail("Member first name is required.");
        if (email.Length == 0 || !email.Contains('@')) return ApiResult<ManageTeamRowDto>.Fail("A valid member email address is required.");
        if (password.Length < 6) return ApiResult<ManageTeamRowDto>.Fail("Password must be at least 6 characters long.");

        var emailConflict = await _db.Users.AsNoTracking()
            .Where(u => u.Email.ToLower() == email)
            .Select(u => u.AssignedRole!.RoleName)
            .FirstOrDefaultAsync(ct);
        if (emailConflict != null)
            return ApiResult<ManageTeamRowDto>.Fail(
                $"This email address is already registered as a {emailConflict}. Please use a different email.");

        if (phone.Length > 0)
        {
            var phoneConflict = await _db.Users.AsNoTracking()
                .Where(u => u.Phone == phone)
                .Select(u => u.AssignedRole!.RoleName)
                .FirstOrDefaultAsync(ct);
            if (phoneConflict != null)
                return ApiResult<ManageTeamRowDto>.Fail(
                    $"This mobile number is already registered as a {phoneConflict}. Please use a different mobile number.");
        }

        var hash = _passwordHasher.Hash(password);
        var user = new User
        {
            AssignedRole = await _db.GetRoleByPrefixAsync(RolePrefixes.DigitalMarketingPartner, ct),
            Email = email,
            Phone = phone,
            FirstName = firstName,
            LastName = lastName,
            Address = new UserAddress { District = district, State = state },
            Password = hash,
            Status = "ACTIVE",
            CreatedAt = AppClock.Now,
            UpdatedAt = AppClock.Now,
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        var message = "Team member added successfully.";
        var name = user.FullName;
        try
        {
            var loginUrl = string.IsNullOrWhiteSpace(_app.SiteBaseUrl)
                ? $"https://{_app.PublicHost}"
                : _app.SiteBaseUrl.TrimEnd('/');
            var vars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["name"] = name,
                ["email"] = email,
                ["password"] = password,
                ["login_url"] = loginUrl,
                ["host"] = _app.PublicHost,
            };
            var rendered = await _templates.TryRenderAsync("team_member_welcome", vars, ct);
            var subject = rendered?.Subject ?? "Welcome to ShareBizz Team";
            var html = rendered?.Html ?? $"""
                <p>Hello {WebUtility.HtmlEncode(name)},</p>
                <p>Your team member account has been created on ShareBizz.</p>
                <p><strong>Login Email:</strong> {WebUtility.HtmlEncode(email)}<br/>
                <strong>Password:</strong> {WebUtility.HtmlEncode(password)}</p>
                <p>Please keep these credentials secure.</p>
                """;
            await _email.SendAsync(email, subject, html, ct);
        }
        catch
        {
            message = "Team member created, but welcome email could not be sent.";
        }

        return ApiResult<ManageTeamRowDto>.Ok(await MapRowAsync(user, ct), message);
    }

    public async Task<ApiResult<ManageTeamRowDto>> UpdateAsync(int id, UpdateTeamMemberRequest request, CancellationToken ct = default)
    {
        var user = await FindTeamAsync(id, tracked: true, ct);
        if (user == null) return ApiResult<ManageTeamRowDto>.Fail("Team member not found");

        var firstName = (request.FirstName ?? "").Trim();
        var lastName = (request.LastName ?? "").Trim();
        var email = NormalizeEmail(request.Email);
        var phone = (request.Phone ?? "").Trim();
        var district = (request.District ?? "").Trim();
        var state = (request.State ?? "").Trim();

        if (firstName.Length == 0) return ApiResult<ManageTeamRowDto>.Fail("Member first name is required.");
        if (email.Length == 0 || !email.Contains('@')) return ApiResult<ManageTeamRowDto>.Fail("A valid member email address is required.");

        var emailConflict = await _db.Users.AsNoTracking()
            .Where(u => u.Id != user.Id && u.Email.ToLower() == email)
            .Select(u => u.AssignedRole!.RoleName)
            .FirstOrDefaultAsync(ct);
        if (emailConflict != null)
            return ApiResult<ManageTeamRowDto>.Fail(
                $"This email address is already registered as a {emailConflict}. Please use a different email.");

        if (phone.Length > 0)
        {
            var phoneConflict = await _db.Users.AsNoTracking()
                .Where(u => u.Id != user.Id && u.Phone == phone)
                .Select(u => u.AssignedRole!.RoleName)
                .FirstOrDefaultAsync(ct);
            if (phoneConflict != null)
                return ApiResult<ManageTeamRowDto>.Fail(
                    $"This mobile number is already registered as a {phoneConflict}. Please use a different mobile number.");
        }

        await _db.StageUserEmailChangeAsync(user.Email, email, ct);
        user.FirstName = firstName;
        user.LastName = lastName;
        user.Email = email;
        user.Phone = phone;
        user.Address.District = district;
        user.Address.State = state;
        user.UpdatedAt = AppClock.Now;
        await _db.SaveChangesAsync(ct);

        return ApiResult<ManageTeamRowDto>.Ok(await MapRowAsync(user, ct), "Team member updated successfully.");
    }

    public async Task<ApiResult<ManageTeamRowDto>> ToggleStatusAsync(int id, ToggleTeamStatusRequest request, CancellationToken ct = default)
    {
        var user = await FindTeamAsync(id, tracked: true, ct);
        if (user == null) return ApiResult<ManageTeamRowDto>.Fail("Team member not found");

        var requested = (request.NewStatus ?? "").Trim().ToUpperInvariant();
        string newStatus;
        if (requested is "ACTIVE" or "INACTIVE")
            newStatus = requested;
        else
            newStatus = string.Equals(user.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase) ? "INACTIVE" : "ACTIVE";

        user.Status = newStatus;
        user.UpdatedAt = AppClock.Now;
        await _db.SaveChangesAsync(ct);
        return ApiResult<ManageTeamRowDto>.Ok(await MapRowAsync(user, ct), "Member status updated.");
    }

    public async Task<ApiResult> ResetPasswordAsync(int id, ResetTeamPasswordRequest request, CancellationToken ct = default)
    {
        var password = (request.NewPassword ?? "").Trim();
        if (password.Length < 6)
            return ApiResult.Fail("Please provide a valid password (minimum 6 characters).");

        var user = await FindTeamAsync(id, tracked: true, ct);
        if (user == null) return ApiResult.Fail("Team member not found");

        var hash = _passwordHasher.Hash(password);
        user.Password = hash;
        user.UpdatedAt = AppClock.Now;
        await _db.SaveChangesAsync(ct);
        return ApiResult.Ok("Password reset successfully.");
    }

    public async Task<ApiResult<TeamReferralsDto>> GetReferralsAsync(int id, CancellationToken ct = default)
    {
        var user = await FindTeamAsync(id, tracked: false, ct);
        if (user == null) return ApiResult<TeamReferralsDto>.Fail("Team member not found");

        try
        {
            var referredUsers = await _db.Users.AsNoTracking()
                .Where(u => u.Referral.ReferrerUserId == user.Id)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.Phone,
                    u.CreatedAt,
                })
                .ToListAsync(ct);

            var earnings = await _db.ReferralEarnings.AsNoTracking()
                .Where(r => r.ReferrerUserId == user.Id)
                .OrderByDescending(r => r.Id)
                .ToListAsync(ct);

            var referredIds = referredUsers.Select(u => u.Id)
                .Concat(earnings.Where(e => e.ReferredUserId != null).Select(e => e.ReferredUserId!.Value))
                .Distinct()
                .ToList();

            var earningByUser = earnings
                .Where(e => e.ReferredUserId != null)
                .GroupBy(e => e.ReferredUserId!.Value)
                .ToDictionary(g => g.Key, g => g.First());

            var usersById = referredIds.Count == 0
                ? []
                : await _db.Users.AsNoTracking()
                    .Where(u => referredIds.Contains(u.Id))
                    .Select(u => new { u.Id, u.Email, Name = (u.FirstName + " " + u.LastName).Trim(), u.Phone })
                    .ToDictionaryAsync(u => u.Id, ct);

            var cards = referredIds.Count == 0
                ? []
                : await _db.Cards.AsNoTracking()
                    .Where(c => c.UserId != null && referredIds.Contains(c.UserId.Value))
                    .OrderByDescending(c => c.Id)
                    .Select(c => new { c.Id, UserId = c.UserId!.Value })
                    .ToListAsync(ct);
            var sales = await CardSales.ByCardIdAsync(_db, cards.Select(c => c.Id).ToList(), ct);
            var cardByUser = cards
                .GroupBy(c => c.UserId)
                .ToDictionary(g => g.Key, g => g.First());

            var mapped = referredIds.Select(referredId =>
            {
                var uInfo = usersById.GetValueOrDefault(referredId);
                var earning = earningByUser.GetValueOrDefault(referredId);
                var card = cardByUser.GetValueOrDefault(referredId);
                var sale = card == null ? CardSale.Unpaid : CardSales.For(sales, card.Id);
                var referredEmail = NormalizeEmail(uInfo?.Email ?? earning?.ReferredEmail);

                var (label, tone) = MapPaymentStatus(card == null ? null : sale.Status, sale.PaidOn);
                var type = string.Equals(earning?.IsCollaboration, "YES", StringComparison.OrdinalIgnoreCase)
                    ? "Franchise"
                    : "ShareBizz";
                var name = !string.IsNullOrWhiteSpace(uInfo?.Name)
                    ? uInfo.Name
                    : referredEmail;
                var referralDate = earning?.ReferralDate ?? referredUsers
                    .FirstOrDefault(r => r.Id == referredId)?.CreatedAt;

                return new TeamReferralRowDto(
                    uInfo?.Id,
                    name,
                    referredEmail,
                    string.IsNullOrWhiteSpace(uInfo?.Phone) ? "—" : uInfo.Phone,
                    type,
                    label,
                    tone,
                    FormatPaidOn(card == null ? null : sale.Status, sale.PaidOn),
                    FormatDate(referralDate));
            })
            .OrderByDescending(r => r.ReferralDateDisplay)
            .Take(300)
            .ToList();

            var paidEmails = cards
                .Where(c => CardSales.For(sales, c.Id).Paid)
                .Select(c => c.UserId)
                .Distinct()
                .Count();

            var totalMw = cards.Count;

            return ApiResult<TeamReferralsDto>.Ok(new TeamReferralsDto(
                user.FullName,
                user.Email,
                paidEmails,
                totalMw,
                mapped));
        }
        catch (Exception ex)
        {
            return ApiResult<TeamReferralsDto>.Fail("Unable to load referrals: " + ex.Message);
        }
    }

    public async Task<ApiResult<TeamTrackerDto>> GetTrackerAsync(int id, CancellationToken ct = default)
    {
        var user = await FindTeamAsync(id, tracked: false, ct);
        if (user == null) return ApiResult<TeamTrackerDto>.Fail("Team member not found");

        try
        {
            var rows = await _db.Customers.AsNoTracking()
                .Where(c => c.OwnerUserId == id)
                .OrderByDescending(c => c.CreatedAt)
                .ThenByDescending(c => c.Id)
                .Select(c => new
                {
                    c.Id,
                    c.CustomerName,
                    c.PhoneNumber,
                    c.ApproachedFor,
                    c.AddressLine1,
                    c.AreaCity,
                    c.Status,
                    c.CreatedAt,
                    c.UpdatedAt,
                    LastFollowup = c.Followups
                        .Select(f => (DateTime?)f.FollowupDatetime)
                        .Max(),
                })
                .ToListAsync(ct);

            var mapped = rows.Select(r =>
            {
                var (tone, _) = MapTrackerStatus(r.Status);
                var address = MergeAddress(r.AddressLine1, r.AreaCity);
                var lastUpdated = r.LastFollowup ?? r.UpdatedAt ?? r.CreatedAt;
                return new TeamTrackerRowDto(
                    r.Id,
                    r.CustomerName ?? "",
                    string.IsNullOrWhiteSpace(r.PhoneNumber) ? "-" : r.PhoneNumber,
                    string.IsNullOrWhiteSpace(r.ApproachedFor) ? "-" : r.ApproachedFor,
                    string.IsNullOrWhiteSpace(address) ? "-" : address,
                    FormatDate(r.CreatedAt),
                    r.Status ?? "",
                    tone,
                    FormatDateTime(lastUpdated));
            }).ToList();

            return ApiResult<TeamTrackerDto>.Ok(new TeamTrackerDto(user.FullName, user.Email, mapped));
        }
        catch (Exception ex)
        {
            return ApiResult<TeamTrackerDto>.Fail("Unable to load tracker: " + ex.Message);
        }
    }

    public async Task<(byte[] Content, string FileName)?> ExportTrackerCsvAsync(int id, CancellationToken ct = default)
    {
        var result = await GetTrackerAsync(id, ct);
        if (!result.Success || result.Data == null) return null;

        var sb = new StringBuilder();
        sb.Append('\uFEFF');
        sb.AppendLine("Shop/Person Name,Contact Number,Approached For,Address,Date Visited,Final Status,Last Updated");
        foreach (var r in result.Data.Rows)
        {
            sb.AppendLine(string.Join(',', new[]
            {
                EscapeCsv(r.ShopName),
                EscapeCsv(r.ContactNumber),
                EscapeCsv(r.ApproachedFor),
                EscapeCsv(r.Address),
                EscapeCsv(r.DateVisitedDisplay),
                EscapeCsv(r.FinalStatus),
                EscapeCsv(r.LastUpdatedDisplay),
            }));
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var safeName = Regex.Replace(result.Data.MemberName, @"[^a-zA-Z0-9_-]+", "_");
        return (bytes, $"tracker_{safeName}_{AppClock.Now:yyyyMMdd_HHmmss}.csv");
    }

    private async Task<ManageTeamRowDto> MapRowAsync(User u, CancellationToken ct)
    {
        var referredIds = await _db.Users.AsNoTracking()
            .Where(x => x.Referral.ReferrerUserId == u.Id)
            .Select(x => x.Id)
            .ToListAsync(ct);

        var earningReferredIds = await _db.ReferralEarnings.AsNoTracking()
            .Where(r => r.ReferrerUserId == u.Id)
            .Select(r => r.ReferredUserId)
            .ToListAsync(ct);

        var allReferred = referredIds
            .Concat(earningReferredIds.Where(id => id != null).Select(id => id!.Value))
            .Distinct()
            .ToList();

        var sales = allReferred.Count == 0
            ? 0
            : await (
                from c in _db.Cards.AsNoTracking()
                join i in _db.Invoices.AsNoTracking() on c.Id equals i.CardId
                where c.UserId != null
                      && allReferred.Contains(c.UserId.Value)
                      && (i.PaymentStatus == "Success" || i.PaymentStatus == "Paid")
                      && i.PaymentDate != null
                select c.UserId
            ).Distinct().CountAsync(ct);

        var trackerCount = await _db.Customers.AsNoTracking()
            .CountAsync(c => c.OwnerUserId == u.Id, ct);

        var referralEarningsCount = earningReferredIds.Count;
        var referredByCount = referredIds.Count;
        var referralCount = Math.Max(referralEarningsCount, referredByCount);

        var ownMwIds = await _db.Cards.AsNoTracking()
            .Where(c => c.UserId == u.Id)
            .OrderByDescending(c => c.Id)
            .Select(c => c.Id)
            .ToListAsync(ct);

        var status = (u.Status ?? "ACTIVE").ToUpperInvariant();
        var tone = status == "ACTIVE" ? "ok" : "danger";

        return new ManageTeamRowDto(
            u.Id,
            null,
            u.FirstName,
            u.LastName,
            u.FullName,
            u.Email,
            u.Phone ?? "",
            u.Address.District ?? "",
            u.Address.State ?? "",
            status,
            tone,
            FormatDate(u.CreatedAt),
            sales,
            ownMwIds.Count,
            referralCount,
            trackerCount,
            ownMwIds);
    }

    private async Task<User?> FindTeamAsync(int id, bool tracked, CancellationToken ct)
    {
        var q = tracked ? _db.Users : _db.Users.AsNoTracking();
        return await q.WhereRole(UserRole.Team).FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    private static string? NormalizeStatusFilter(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("all", StringComparison.OrdinalIgnoreCase))
            return null;
        var v = value.Trim().ToUpperInvariant();
        return v is "ACTIVE" or "INACTIVE" ? v : null;
    }

    private static string NormalizeEmail(string? email) => (email ?? "").Trim().ToLowerInvariant();

    private static (string Label, string Tone) MapPaymentStatus(string? status, DateTime? paidOn)
    {
        if (string.IsNullOrWhiteSpace(status)) return ("N/A", "neutral");
        if (CardSales.IsPaid(status)
            && paidOn is { } d && d.Year > 1)
            return ("Paid", "ok");
        if (string.Equals(status, "Failed", StringComparison.OrdinalIgnoreCase))
            return ("Failed", "danger");
        if (status is "Created" or "Pending" or "")
            return ("Pending", "warn");
        return (status, "neutral");
    }

    private static string FormatPaidOn(string? status, DateTime? paidOn)
    {
        if (!CardSales.IsPaid(status)) return "-";
        return FormatDate(paidOn);
    }

    private static (string Tone, string _) MapTrackerStatus(string? status)
    {
        if (string.Equals(status, "Joined", StringComparison.OrdinalIgnoreCase)) return ("ok", status ?? "");
        if (string.Equals(status, "Not Interested", StringComparison.OrdinalIgnoreCase)) return ("danger", status ?? "");
        return ("warn", status ?? "");
    }

    private static string FormatDate(DateTime? value)
    {
        if (value is not { } d || d.Year < 2) return "—";
        return d.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
    }

    private static string FormatDateTime(DateTime? value)
    {
        if (value is not { } d || d.Year < 2) return "-";
        return d.ToString("dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture);
    }

    private static string? MergeAddress(string? a1, string? ac)
    {
        a1 = (a1 ?? "").Trim();
        ac = (ac ?? "").Trim();
        var merged = (a1 + ((a1 != "" && ac != "") ? ", " : "") + ac).Trim();
        return string.IsNullOrEmpty(merged) ? null : merged;
    }

    private static string EscapeCsv(string? value)
    {
        var v = value ?? "";
        if (v.Contains('"') || v.Contains(',') || v.Contains('\n') || v.Contains('\r'))
            return "\"" + v.Replace("\"", "\"\"") + "\"";
        return v;
    }
}
