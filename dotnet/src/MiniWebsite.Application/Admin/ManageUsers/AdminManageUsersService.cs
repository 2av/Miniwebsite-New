using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MiniWebsite.Application.Admin.EmailTemplates;
using MiniWebsite.Application.Admin.ManageUsers.Dtos;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Domain.Common;
using MiniWebsite.Domain.Entities;
using MiniWebsite.Domain.Enums;
using MiniWebsite.Application.Common;
using MiniWebsite.Application.Common.Extensions;

namespace MiniWebsite.Application.Admin.ManageUsers;

public class AdminManageUsersService : IAdminManageUsersService
{
    private static readonly string[] AllowedRefund =
        ["None", "Refund Claimed", "Refund Settled"];

    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailSender _email;
    private readonly IEmailTemplateRenderer _templates;

    public AdminManageUsersService(
        IApplicationDbContext db,
        IPasswordHasher passwordHasher,
        IEmailSender email,
        IEmailTemplateRenderer templates)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _email = email;
        _templates = templates;
    }

    public Task<ApiResult<ManageUsersPageDto>> ListAsync(ManageUsersQuery query, CancellationToken ct = default)
    {
        query.Page = query.Page < 1 ? 1 : query.Page;
        query.PageSize = query.PageSize is < 1 or > 100 ? 10 : query.PageSize;
        return LoadPageAsync(query, ct);
    }

    private async Task<ApiResult<ManageUsersPageDto>> LoadPageAsync(ManageUsersQuery query, CancellationToken ct)
    {

        var customers = ApplyPostFilters(ApplyCustomerFilters(_db.Users.AsNoTracking(), query), query);

        var totalWebsites = await _db.Cards.AsNoTracking().CountAsync(ct);
        var totalUsers = await customers.CountAsync(ct);
        var activeUsers = await customers.CountAsync(u => u.Status == "ACTIVE", ct);
        var usersWithDeals = await customers.CountAsync(HasDealMapping(), ct);

        var pageUsers = await customers
            .OrderByDescending(u => u.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        var userIds = pageUsers.Select(u => u.Id).ToList();
        var websiteCounts = await GetWebsiteCountsAsync(userIds, ct);
        var mwDealsByUser = await GetMappedDealsAsync(userIds, RolePrefixes.ShareBizzUser, ct);
        var franchiseDealsByUser = await GetMappedDealsAsync(userIds, RolePrefixes.FranchisePartner, ct);
        var refSummaries = await GetReferralSummariesAsync(userIds, ct);
        var referralSources = await BuildReferralSourcesAsync(pageUsers, ct);
        var documents = await GetDocumentStatusesAsync(userIds, ct);
        var invoices = await GetLatestInvoiceIdsAsync(userIds, ct);

        var rows = new List<ManageUserRowDto>(pageUsers.Count);
        foreach (var u in pageUsers)
        {
            websiteCounts.TryGetValue(u.Id, out var wc);
            refSummaries.TryGetValue(u.Id, out var summary);
            summary ??= new ReferralSummary(0, 0, null, 0, 0);

            var pending = Math.Max(0, summary.TotalAmount - summary.PaidAmount);
            mwDealsByUser.TryGetValue(u.Id, out var mwDeal);
            franchiseDealsByUser.TryGetValue(u.Id, out var frDeal);
            referralSources.TryGetValue(u.Id, out var referralSource);
            if (!documents.TryGetValue(u.Id, out var document))
                document = ("Not Submitted", "neutral");
            invoices.TryGetValue(u.Id, out var invoiceId);

            rows.Add(new ManageUserRowDto(
                u.Id,
                UserCodeGenerator.FormatDisplay(u.UserCode),
                u.Email,
                u.FullName,
                u.Phone,
                u.Address.State,
                u.Address.District,
                FormatUserStatus(u.Status),
                u.CreatedAt,
                u.Referral.CollaborationEnabled ? "YES" : "NO",
                u.Referral.SaleskitEnabled ? "YES" : "NO",
                string.IsNullOrWhiteSpace(u.RefundStatus) ? "None" : u.RefundStatus,
                u.RefundStatusDate,
                u.Referral.ReferredBy,
                referralSource ?? "Direct",
                wc,
                pending,
                summary.TotalAmount,
                summary.PaidAmount,
                summary.LastPaymentDate,
                BuildMwPaymentLabel(summary),
                BuildCommissionStatus(summary),
                mwDeal,
                frDeal,
                document.Status,
                document.Tone,
                summary.EarningCount,
                BuildPayoutStatus(summary),
                invoiceId == 0 ? null : invoiceId));
        }

        var mwDeals = await ListActiveDealsAsync(RolePrefixes.ShareBizzUser, ct);
        var frDeals = await ListActiveDealsAsync(RolePrefixes.FranchisePartner, ct);

        var page = new ManageUsersPageDto(
            new ManageUsersStatsDto(totalUsers, activeUsers, usersWithDeals, totalWebsites),
            mwDeals.Count,
            frDeals.Count,
            mwDeals,
            frDeals,
            rows,
            totalUsers,
            query.Page,
            query.PageSize);

        return ApiResult<ManageUsersPageDto>.Ok(page);
    }

    public async Task<ApiResult> MapDealAsync(MapDealRequest request, CancellationToken ct = default)
    {
        var email = Normalize(request.UserEmail);
        if (string.IsNullOrWhiteSpace(email) || request.DealId <= 0)
            return ApiResult.Fail("userEmail and dealId are required.");

        var userId = await _db.Users.AsNoTracking()
            .Where(u => u.Email == email)
            .Select(u => (int?)u.Id)
            .FirstOrDefaultAsync(ct);
        if (userId == null)
            return ApiResult.Fail("No account found for this email.");

        var exists = await _db.DealCustomerMappings.AnyAsync(
            m => m.CouponId == request.DealId && m.UserId == userId, ct);

        if (exists)
            return ApiResult.Fail("Deal already mapped to this user!");

        var createdBy = string.IsNullOrWhiteSpace(request.CreatedBy) ? "admin" : request.CreatedBy!.Trim();

        _db.DealCustomerMappings.Add(new DealCustomerMapping
        {
            CouponId = request.DealId,
            CustomerEmail = email,
            UserId = userId,
            CreatedBy = createdBy,
            CreatedAt = AppClock.Now,
        });
        await _db.SaveChangesAsync(ct);

        return ApiResult.Ok("Deal mapped successfully!");
    }

    public async Task<ApiResult> RemoveDealAsync(int mappingId, CancellationToken ct = default)
    {
        if (mappingId <= 0)
            return ApiResult.Fail("Invalid mapping id.");

        var mapping = await _db.DealCustomerMappings.FirstOrDefaultAsync(m => m.Id == mappingId, ct);
        if (mapping == null)
            return ApiResult.Fail("Deal mapping not found.");

        _db.DealCustomerMappings.Remove(mapping);
        await _db.SaveChangesAsync(ct);
        return ApiResult.Ok("Deal mapping removed successfully!");
    }

    public async Task<ApiResult> SetUserStatusAsync(int id, string? status, CancellationToken ct = default)
    {
        var requested = (status ?? "").Trim().ToUpperInvariant();
        if (requested is not ("ACTIVE" or "INACTIVE"))
            return ApiResult.Fail("Status must be ACTIVE or INACTIVE.");

        var user = await _db.Users.WhereRole(UserRole.Customer)
            .FirstOrDefaultAsync(u => u.Id == id && !u.Referral.CollaborationEnabled, ct);
        if (user == null)
            return ApiResult.Fail("User not found.");

        user.Status = requested;
        user.UpdatedAt = AppClock.Now;
        await _db.SaveChangesAsync(ct);
        return ApiResult.Ok(requested == "ACTIVE" ? "User set to Active." : "User set to InActive.");
    }

    public async Task<ApiResult> SetCollaborationAsync(ToggleStatusRequest request, CancellationToken ct = default)
    {
        var email = Normalize(request.Email);
        var status = NormalizeYesNo(request.Status);
        if (status is not ("YES" or "NO"))
            return ApiResult.Fail("Status must be YES or NO.");

        var user = await _db.Users.WhereRole(UserRole.Customer).FirstOrDefaultAsync(
            u => u.Email == email, ct);
        if (user == null)
            return ApiResult.Fail("User not found.");

        user.Referral.CollaborationEnabled = status == "YES";
        user.UpdatedAt = AppClock.Now;
        await _db.SaveChangesAsync(ct);

        if (status == "YES")
        {
            try
            {
                var vars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["name"] = user.FullName,
                    ["email"] = email,
                };
                var rendered = await _templates.TryRenderAsync("collaboration_upgraded", vars, ct);
                var subject = rendered?.Subject
                    ?? "Congratulations! Your Account Has Been Upgraded with Collaborator Feature";
                var html = rendered?.Html ?? BuildCollaborationEmailHtml();
                await _email.SendAsync(email, subject, html, ct);
                return ApiResult.Ok("User collaboration status updated successfully and congratulatory email sent!");
            }
            catch
            {
                return ApiResult.Ok("User collaboration status updated successfully, but email could not be sent.");
            }
        }

        return ApiResult.Ok("User collaboration status updated successfully");
    }

    public async Task<ApiResult> SetSaleskitAsync(ToggleStatusRequest request, CancellationToken ct = default)
    {
        var email = Normalize(request.Email);
        var status = NormalizeYesNo(request.Status);
        if (status is not ("YES" or "NO"))
            return ApiResult.Fail("Status must be YES or NO.");

        var user = await _db.Users.WhereRole(UserRole.Customer).FirstOrDefaultAsync(
            u => u.Email == email, ct);
        if (user == null)
            return ApiResult.Fail("User not found.");

        user.Referral.SaleskitEnabled = status == "YES";
        user.UpdatedAt = AppClock.Now;
        await _db.SaveChangesAsync(ct);
        return ApiResult.Ok("User sales kit status updated successfully");
    }

    public async Task<ApiResult> SetRefundAsync(SetRefundRequest request, CancellationToken ct = default)
    {
        var email = Normalize(request.Email);
        var refundStatus = (request.RefundStatus ?? "None").Trim();
        if (!AllowedRefund.Contains(refundStatus, StringComparer.Ordinal))
            return ApiResult.Fail("Invalid refund status.");

        var user = await _db.Users.WhereRole(UserRole.Customer).FirstOrDefaultAsync(
            u => u.Email == email, ct);
        if (user == null)
            return ApiResult.Fail("User not found.");

        user.RefundStatus = refundStatus;
        if (refundStatus == "None")
            user.RefundStatusDate = null;
        else if (user.RefundStatusDate == null)
            user.RefundStatusDate = AppClock.Now;

        user.UpdatedAt = AppClock.Now;
        await _db.SaveChangesAsync(ct);
        return ApiResult.Ok("success");
    }

    public async Task<ApiResult> ResetPasswordAsync(AdminResetPasswordRequest request, CancellationToken ct = default)
    {
        var email = Normalize(request.Email);
        var role = ParseRole(request.Role);
        if (role == null)
            return ApiResult.Fail("Invalid role.");
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            return ApiResult.Fail("Password must be at least 6 characters.");

        var user = await _db.Users.WhereRole(role.Value).FirstOrDefaultAsync(
            u => u.Email == email, ct);
        if (user == null)
            return ApiResult.Fail("User not found for this role.");

        var hash = _passwordHasher.Hash(request.NewPassword);
        user.Password = hash;
        user.UpdatedAt = AppClock.Now;
        await _db.SaveChangesAsync(ct);
        return ApiResult.Ok("Password updated successfully for " + email);
    }

    public async Task<ApiResult<DashboardDetailsDto>> GetDashboardDetailsAsync(string userEmail, CancellationToken ct = default)
    {
        var email = Normalize(userEmail);
        if (string.IsNullOrWhiteSpace(email))
            return ApiResult<DashboardDetailsDto>.Fail("Invalid user email provided.");

        var userId = await _db.FindUserIdAsync(email, ct);
        var cards = userId == null
            ? []
            : await _db.Cards.AsNoTracking()
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync(ct);

        var cardIds = cards.Select(c => c.Id).ToList();
        var sales = await CardSales.ByCardIdAsync(_db, cardIds, ct);
        var invoiceRows = cardIds.Count == 0
            ? []
            : await _db.Invoices.AsNoTracking()
                .Where(i => i.CardId != null && cardIds.Contains(i.CardId.Value))
                .Select(i => new { CardId = i.CardId!.Value, i.Id, i.OrderCode })
                .ToListAsync(ct);
        var orderIdByCard = new Dictionary<int, string>();
        foreach (var invoice in invoiceRows.OrderByDescending(i => i.Id))
            orderIdByCard.TryAdd(invoice.CardId, UserCodeGenerator.FormatDisplay(invoice.OrderCode));

        var sites = cards.Select(c =>
        {
            var sale = CardSales.For(sales, c.Id);
            var status = ResolveDisplayStatus(c, sale);
            return new DashboardWebsiteDto(
                c.Id,
                c.DCompName,
                c.CardId,
                c.DCardStatus,
                sale.Status,
                sale.PaidOn,
                c.UploadedDate,
                c.ValidityDate,
                c.ComplimentaryEnabled,
                c.FUserEmail,
                status.Class,
                status.Text,
                FormatValidityDisplay(c, sale),
                FormatPaymentLabel(c, sale),
                orderIdByCard.GetValueOrDefault(c.Id) ?? "");
        }).ToList();

        return ApiResult<DashboardDetailsDto>.Ok(new DashboardDetailsDto(email, sites));
    }

    public async Task<ApiResult<ReferralDetailsDto>> GetReferralDetailsAsync(string referrerEmail, CancellationToken ct = default)
    {
        var email = Normalize(referrerEmail);
        if (string.IsNullOrWhiteSpace(email))
            return ApiResult<ReferralDetailsDto>.Fail("Missing referrer email");

        var user = await _db.Users.AsNoTracking()
            .WhereRole(UserRole.Customer)
            .FirstOrDefaultAsync(u => u.Email == email, ct);

        var referrerId = await _db.FindUserIdAsync(email, ct);
        var earnings = referrerId == null
            ? []
            : await _db.ReferralEarnings.AsNoTracking()
                .Where(r => r.ReferrerUserId == referrerId)
                .OrderByDescending(r => r.ReferralDate)
                .ToListAsync(ct);

        var referredIds = earnings
            .Where(e => e.ReferredUserId != null)
            .Select(e => e.ReferredUserId!.Value)
            .Distinct()
            .ToList();
        var paidSet = await CardSales.PaidUserIdsAsync(_db, referredIds, ct);

        var totalReferral = earnings
            .Where(e => e.ReferredUserId is { } id && paidSet.Contains(id))
            .Sum(e => e.Amount);

        var paidAmount = await GetTotalPaidForReferrerAsync(referrerId, ct);
        var pending = Math.Max(0, totalReferral - paidAmount);

        var regular = earnings.Count(e =>
            !string.Equals(e.IsCollaboration, "YES", StringComparison.OrdinalIgnoreCase));
        var collab = earnings.Count(e =>
            string.Equals(e.IsCollaboration, "YES", StringComparison.OrdinalIgnoreCase));

        var bank = await GetBankDetailsAsync(referrerId, ct);

        var customerMap = await _db.Users.AsNoTracking()
            .WhereRole(UserRole.Customer)
            .Where(u => referredIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u, ct);

        var franchiseeMap = await _db.Users.AsNoTracking()
            .WhereRole(UserRole.Franchisee)
            .Where(u => referredIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u, ct);

        var allCards = await _db.Cards.AsNoTracking()
            .Where(c => c.UserId != null && referredIds.Contains(c.UserId.Value))
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);
        var cardMap = new Dictionary<int, Card>();
        foreach (var c in allCards)
            cardMap.TryAdd(c.UserId!.Value, c);
        var cardSales = await CardSales.ByCardIdAsync(_db, allCards.Select(c => c.Id).ToList(), ct);

        var referred = new List<ReferredUserDto>();
        foreach (var e in earnings)
        {
            var isCollab = string.Equals(e.IsCollaboration, "YES", StringComparison.OrdinalIgnoreCase);
            User? cust = null, fran = null;
            Card? card = null;
            if (e.ReferredUserId is { } referredId)
            {
                customerMap.TryGetValue(referredId, out cust);
                franchiseeMap.TryGetValue(referredId, out fran);
                cardMap.TryGetValue(referredId, out card);
            }

            var sale = card == null ? CardSale.Unpaid : CardSales.For(cardSales, card.Id);
            var statusText = card == null ? "-" : ResolveShortStatus(card, sale);
            referred.Add(new ReferredUserDto(
                e.Id,
                e.ReferredEmail,
                e.ReferralDate,
                e.Amount,
                e.IsCollaboration,
                cust?.Id,
                fran?.Id,
                isCollab ? fran?.FullName ?? cust?.FullName : cust?.FullName ?? fran?.FullName,
                isCollab ? fran?.Phone ?? cust?.Phone : cust?.Phone ?? fran?.Phone,
                card?.Id,
                card?.UploadedDate,
                card?.ValidityDate,
                card?.ComplimentaryEnabled,
                card == null ? null : sale.Status,
                card == null ? null : sale.PaidOn,
                card?.FUserEmail,
                statusText,
                card == null ? "-" : FormatValidityDisplay(card, sale)));
        }

        return ApiResult<ReferralDetailsDto>.Ok(new ReferralDetailsDto(
            email,
            user?.FullName ?? email,
            user?.Referral.CollaborationEnabled == true ? "YES" : "NO",
            user?.Referral.SaleskitEnabled == true ? "YES" : "NO",
            totalReferral,
            paidAmount,
            pending,
            regular,
            collab,
            bank,
            referred));
    }

    public async Task<ApiResult> UpsertBankDetailsAsync(UpsertBankDetailsRequest request, CancellationToken ct = default)
    {
        var email = Normalize(request.UserEmail);
        if (string.IsNullOrWhiteSpace(email))
            return ApiResult.Fail("user_email is required.");

        var userId = await _db.FindUserIdAsync(email, ct);
        if (userId == null)
            return ApiResult.Fail("No account found for this email.");

        var entity = await _db.UserBankDetails.FirstOrDefaultAsync(b => b.UserId == userId, ct);
        var now = AppClock.Now;

        if (entity != null)
        {
            entity.AccountHolderName = request.AccountHolderName ?? "";
            entity.AccountNumber = request.AccountNumber ?? "";
            entity.IfscCode = request.IfscCode ?? "";
            entity.BankName = request.BankName ?? "";
            entity.UpiId = request.UpiId ?? "";
            entity.UpiName = request.UpiName ?? "";
            entity.UpdatedAt = now;
        }
        else
        {
            _db.UserBankDetails.Add(new UserBankDetails
            {
                UserId = userId.Value,
                AccountHolderName = request.AccountHolderName ?? "",
                AccountNumber = request.AccountNumber ?? "",
                IfscCode = request.IfscCode ?? "",
                BankName = request.BankName ?? "",
                UpiId = request.UpiId ?? "",
                UpiName = request.UpiName ?? "",
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        await _db.SaveChangesAsync(ct);
        return ApiResult.Ok("success");
    }

    public async Task<(byte[] Content, string FileName)> ExportCsvAsync(ManageUsersQuery query, CancellationToken ct = default)
    {
        query.Page = 1;
        query.PageSize = int.MaxValue;
        var result = await LoadPageAsync(query, ct);
        var rows = result.Data?.Users ?? [];

        var sb = new StringBuilder();
        sb.AppendLine("User ID,User Email,User Name,User Number,Joined On,State,District,Referral Source,User Status,No. of DB,Documents Status,Total Referred,Total Commission Earning,Referrer Commission Status,SB Payout Status");
        foreach (var u in rows)
        {
            sb.AppendLine(string.Join(",",
                Csv(u.UserCode),
                Csv(u.Email),
                Csv(u.Name),
                Csv(u.Phone),
                Csv(u.CreatedAt.ToString("dd-MMM-yy", CultureInfo.InvariantCulture)),
                Csv(u.State),
                Csv(u.District),
                Csv(u.ReferralSourceDisplay),
                Csv(u.Status),
                u.WebsiteCount,
                Csv(u.DocumentStatus),
                u.TotalReferred,
                u.TotalReferralAmount.ToString("0", CultureInfo.InvariantCulture),
                Csv(u.CommissionStatus),
                Csv(u.PayoutStatus)));
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return (bytes, $"manage_users_{AppClock.Now:yyyyMMdd_HHmmss}.csv");
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private IQueryable<User> ApplyCustomerFilters(IQueryable<User> query, ManageUsersQuery q)
    {
        query = query.WhereRole(UserRole.Customer);
        query = query.Where(u => !u.Referral.CollaborationEnabled);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim().ToLowerInvariant();
            var compact = s.Replace(" ", "");
            query = query.Where(u =>
                (u.FirstName + " " + u.LastName).Contains(s)
                || u.Email.Contains(s)
                || u.UserCode.Contains(compact)
                || (u.Phone != null && u.Phone.Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(q.StatusFilter))
        {
            var status = q.StatusFilter.Equals("YES", StringComparison.OrdinalIgnoreCase)
                ? "ACTIVE"
                : "INACTIVE";
            query = query.Where(u => u.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(q.DateFilter))
        {
            var now = AppClock.Now;
            switch (q.DateFilter.ToLowerInvariant())
            {
                case "today":
                    var today = now.Date;
                    query = query.Where(u => u.CreatedAt >= today);
                    break;
                case "week":
                    query = query.Where(u => u.CreatedAt >= now.AddDays(-7));
                    break;
                case "month":
                    query = query.Where(u => u.CreatedAt >= now.AddMonths(-1));
                    break;
                case "year":
                    query = query.Where(u => u.CreatedAt >= now.AddYears(-1));
                    break;
            }
        }

        return query;
    }

    private IQueryable<User> ApplyPostFilters(IQueryable<User> query, ManageUsersQuery q)
    {
        if (string.Equals(q.DealFilter, "mapped", StringComparison.OrdinalIgnoreCase))
            query = query.Where(HasDealMapping());
        else if (string.Equals(q.DealFilter, "unmapped", StringComparison.OrdinalIgnoreCase))
            query = query.Where(u => !_db.DealCustomerMappings.Any(m => m.UserId == u.Id));

        var (min, max) = q.WebsiteFilter switch
        {
            "0" => (0, 0),
            "1-5" => (1, 5),
            "6-10" => (6, 10),
            "10+" => (11, int.MaxValue),
            _ => (-1, -1)
        };
        if (min < 0) return query;

        return query
            .Select(u => new
            {
                User = u,
                Websites = _db.Cards.Count(c => c.UserId == u.Id)
            })
            .Where(x => x.Websites >= min && x.Websites <= max)
            .Select(x => x.User);
    }

    private System.Linq.Expressions.Expression<Func<User, bool>> HasDealMapping() =>
        u => _db.DealCustomerMappings.Any(m => m.UserId == u.Id);

    private async Task<Dictionary<int, int>> GetWebsiteCountsAsync(
        IReadOnlyList<int> userIds, CancellationToken ct)
    {
        if (userIds.Count == 0) return new Dictionary<int, int>();

        return await _db.Cards.AsNoTracking()
            .Where(c => c.UserId != null && userIds.Contains(c.UserId.Value))
            .GroupBy(c => c.UserId!.Value)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.UserId, g => g.Count, ct);
    }

    private async Task<Dictionary<int, MappedDealDto>> GetMappedDealsAsync(
        IReadOnlyList<int> userIds, string rolePrefix, CancellationToken ct)
    {
        var result = new Dictionary<int, MappedDealDto>();
        if (userIds.Count == 0) return result;

        var rows = await (
            from m in _db.DealCustomerMappings.AsNoTracking()
            join d in _db.Coupons.AsNoTracking().ForRolePrefix(rolePrefix) on m.CouponId equals d.Id
            where m.UserId != null
                  && userIds.Contains(m.UserId.Value)
            orderby m.Id descending
            select new
            {
                MappingId = m.Id,
                m.CouponId,
                UserId = m.UserId!.Value,
                d.CouponName,
                d.CouponCode,
            }).ToListAsync(ct);

        foreach (var row in rows)
            result.TryAdd(row.UserId, new MappedDealDto(row.MappingId, row.CouponId, row.CouponName ?? "", row.CouponCode));

        return result;
    }

    private async Task<List<DealOptionDto>> ListActiveDealsAsync(string rolePrefix, CancellationToken ct)
    {
        return await _db.Coupons.AsNoTracking()
            .ForRolePrefix(rolePrefix)
            .Usable()
            .OrderBy(d => d.CouponName)
            .Select(d => new DealOptionDto(d.Id, d.CouponName ?? "", d.CouponCode, d.Role!.RoleName))
            .ToListAsync(ct);
    }

    private async Task<Dictionary<int, ReferralSummary>> GetReferralSummariesAsync(
        IReadOnlyList<int> userIds, CancellationToken ct)
    {
        var map = new Dictionary<int, ReferralSummary>();
        if (userIds.Count == 0) return map;

        var earnings = await _db.ReferralEarnings.AsNoTracking()
            .Where(r => r.ReferrerUserId != null && userIds.Contains(r.ReferrerUserId.Value))
            .Select(r => new { UserId = r.ReferrerUserId!.Value, r.Amount, r.Status })
            .ToListAsync(ct);

        var payments = await GetPaymentTotalsByReferrerAsync(userIds, ct);

        foreach (var group in earnings.GroupBy(r => r.UserId))
        {
            payments.TryGetValue(group.Key, out var pay);
            map[group.Key] = new ReferralSummary(
                group.Sum(x => x.Amount),
                pay.Paid,
                pay.Last,
                group.Count(),
                group.Count(x => string.Equals(x.Status, "Rejected", StringComparison.OrdinalIgnoreCase)));
        }

        return map;
    }

    private async Task<decimal> GetTotalPaidForReferrerAsync(int? referrerId, CancellationToken ct)
    {
        if (referrerId == null) return 0;
        try
        {
            return await (
                from p in _db.ReferralPayments.AsNoTracking()
                join e in _db.ReferralEarnings.AsNoTracking() on p.ReferralEarningId equals e.Id
                where e.ReferrerUserId == referrerId
                select (decimal?)p.Amount).SumAsync(ct) ?? 0m;
        }
        catch
        {
            return 0;
        }
    }

    private async Task<Dictionary<int, (decimal Paid, DateTime? Last)>> GetPaymentTotalsByReferrerAsync(
        IReadOnlyList<int> userIds, CancellationToken ct)
    {
        var map = new Dictionary<int, (decimal Paid, DateTime? Last)>();
        try
        {
            var rows = await (
                from p in _db.ReferralPayments.AsNoTracking()
                join e in _db.ReferralEarnings.AsNoTracking() on p.ReferralEarningId equals e.Id
                where e.ReferrerUserId != null && userIds.Contains(e.ReferrerUserId.Value)
                group p by e.ReferrerUserId!.Value into g
                select new { UserId = g.Key, Paid = g.Sum(x => x.Amount), Last = g.Max(x => x.PaymentDate) })
                .ToListAsync(ct);
            foreach (var row in rows)
                map[row.UserId] = (row.Paid, row.Last);
        }
        catch
        {
            // referral_payments may be missing in some environments
        }

        return map;
    }

    private async Task<BankDetailsDto> GetBankDetailsAsync(int? userId, CancellationToken ct)
    {
        try
        {
            var row = userId == null ? null : await _db.UserBankDetails.AsNoTracking()
                .FirstOrDefaultAsync(b => b.UserId == userId, ct);

            return row == null
                ? new BankDetailsDto(null, null, null, null, null, null)
                : new BankDetailsDto(row.AccountHolderName, row.AccountNumber, row.IfscCode, row.BankName, row.UpiId, row.UpiName);
        }
        catch
        {
            return new BankDetailsDto(null, null, null, null, null, null);
        }
    }

    private async Task<Dictionary<int, string>> BuildReferralSourcesAsync(
        IReadOnlyList<User> users,
        CancellationToken ct)
    {
        var referrerIds = users
            .Where(u => u.Referral.ReferrerUserId != null)
            .Select(u => u.Referral.ReferrerUserId!.Value)
            .Distinct()
            .ToList();

        var referrerCodes = referrerIds.Count == 0
            ? new Dictionary<int, string>()
            : await _db.Users.AsNoTracking()
                .Where(u => referrerIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => UserCodeGenerator.FormatDisplay(u.UserCode), ct);

        var map = new Dictionary<int, string>();
        foreach (var user in users)
        {
            if (user.Referral.ReferrerUserId is { } referrerId
                && referrerCodes.TryGetValue(referrerId, out var code)
                && !string.IsNullOrWhiteSpace(code))
                map[user.Id] = code;
            else if (!string.IsNullOrWhiteSpace(user.Referral.ReferredBy))
                map[user.Id] = "Ref: " + user.Referral.ReferredBy.Trim();
            else
                map[user.Id] = "Direct";
        }

        return map;
    }

    private static string FormatUserStatus(string? status)
    {
        if (string.Equals(status, "ACTIVE", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
            return "Active";

        if (string.Equals(status, "INACTIVE", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "InActive", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Inactive", StringComparison.OrdinalIgnoreCase))
            return "InActive";

        return string.IsNullOrWhiteSpace(status) ? "InActive" : status.Trim();
    }

    private async Task<Dictionary<int, (string Status, string Tone)>> GetDocumentStatusesAsync(
        IReadOnlyList<int> userIds, CancellationToken ct)
    {
        var map = new Dictionary<int, (string Status, string Tone)>();
        if (userIds.Count == 0) return map;

        var docs = await _db.FranchiseVerifications.AsNoTracking()
            .Where(v => userIds.Contains(v.UserId))
            .Select(v => new { v.UserId, v.Id, v.Status })
            .ToListAsync(ct);

        foreach (var group in docs.GroupBy(d => d.UserId))
        {
            var latest = group.OrderByDescending(d => d.Id).First();
            var status = MapDocumentStatus(latest.Status);
            map[group.Key] = (status, MapDocumentTone(status));
        }

        return map;
    }

    private async Task<Dictionary<int, int>> GetLatestInvoiceIdsAsync(
        IReadOnlyList<int> userIds, CancellationToken ct)
    {
        var map = new Dictionary<int, int>();
        if (userIds.Count == 0) return map;

        var invoices = await _db.Invoices.AsNoTracking()
            .Where(i => i.UserId != null && userIds.Contains(i.UserId.Value))
            .Select(i => new { UserId = i.UserId!.Value, i.Id })
            .ToListAsync(ct);

        foreach (var group in invoices.GroupBy(i => i.UserId))
            map[group.Key] = group.Max(i => i.Id);

        return map;
    }

    private static string MapDocumentStatus(string? status) => (status ?? "").Trim().ToLowerInvariant() switch
    {
        "submitted" or "pending" or "pending review" => "Pending Review",
        "approved" or "verified" => "Approved",
        "resubmission" or "resubmission required" or "resubmit" => "Resubmission Required",
        "rejected" => "Rejected",
        _ => "Not Submitted",
    };

    private static string MapDocumentTone(string label) => label switch
    {
        "Approved" => "ok",
        "Rejected" => "danger",
        "Pending Review" or "Resubmission Required" => "warn",
        _ => "neutral",
    };

    /// <summary>SB Payment Details: Pending, Paid on {date}, or Rejected.</summary>
    private static string BuildPayoutStatus(ReferralSummary s)
    {
        if (s.TotalAmount > 0 && s.PaidAmount >= s.TotalAmount)
        {
            var date = s.LastPaymentDate?.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)
                       ?? AppClock.Now.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
            return "Paid on " + date;
        }

        if (s.EarningCount > 0 && s.RejectedCount == s.EarningCount && s.PaidAmount <= 0)
            return "Rejected";

        return "Pending";
    }

    private static string BuildCommissionStatus(ReferralSummary s)
    {
        if (s.TotalAmount <= 0) return "Not Eligible";
        if (s.EarningCount > 0 && s.RejectedCount == s.EarningCount) return "Rejected";
        return "Eligible";
    }

    private static string BuildMwPaymentLabel(ReferralSummary s)
    {
        if (s.TotalAmount <= 0) return "Not Eligible";
        if (s.PaidAmount >= s.TotalAmount)
        {
            var d = s.LastPaymentDate?.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)
                    ?? AppClock.Now.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
            return "Paid on " + d;
        }
        if (s.PaidAmount > 0) return "Partial Payment";
        return "Pending";
    }

    private static (string Class, string Text) ResolveDisplayStatus(Card c, CardSale sale)
    {
        if (string.Equals(c.ComplimentaryEnabled, "Yes", StringComparison.OrdinalIgnoreCase))
            return ("bg-success", "Active");

        if (sale.Paid)
        {
            if (c.ValidityDate.HasValue && c.ValidityDate.Value < AppClock.Now)
                return ("bg-secondary", "Expired on " + c.ValidityDate.Value.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture));
            return ("bg-success", "Active");
        }

        var uploaded = c.UploadedDate;
        var trialEnd = uploaded?.AddDays(7);
        var trialExpired = trialEnd.HasValue && trialEnd.Value < AppClock.Now;
        var franchiseLinked = !string.IsNullOrWhiteSpace(c.FUserEmail) && c.FUserEmail.Trim().Length >= 3;

        if (franchiseLinked)
            return trialExpired ? ("bg-secondary", "Inactive") : ("bg-pending", "Pending Payment");

        if (trialExpired)
            return ("bg-secondary", "Inactive");

        return ("bg-pending", "7 Day Trial");
    }

    private static string ResolveShortStatus(Card c, CardSale sale) => ResolveDisplayStatus(c, sale).Text;

    private static string FormatValidityDisplay(Card c, CardSale sale)
    {
        if (c.ValidityDate.HasValue)
            return c.ValidityDate.Value.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);

        if (sale.Paid && sale.PaidOn.HasValue)
            return sale.PaidOn.Value.AddYears(1).ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);

        if (c.UploadedDate.HasValue)
            return c.UploadedDate.Value.AddDays(7).ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);

        return "-";
    }

    private static string FormatPaymentLabel(Card c, CardSale sale)
    {
        if (string.Equals(c.ComplimentaryEnabled, "Yes", StringComparison.OrdinalIgnoreCase))
            return "Complimentary";
        if (sale.Paid)
        {
            if (sale.PaidOn.HasValue)
                return "Paid on " + sale.PaidOn.Value.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
            return "Paid";
        }
        return "Pending";
    }

    private static string Normalize(string? value) => (value ?? string.Empty).Trim().ToLowerInvariant();

    private static string NormalizeYesNo(string? value)
    {
        var v = (value ?? "NO").Trim().ToUpperInvariant();
        return v == "YES" ? "YES" : "NO";
    }

    private static UserRole? ParseRole(string? role) =>
        (role ?? "").Trim().ToUpperInvariant() switch
        {
            "CUSTOMER" => UserRole.Customer,
            "FRANCHISEE" => UserRole.Franchisee,
            "TEAM" => UserRole.Team,
            "ADMIN" => UserRole.Admin,
            _ => null
        };

    private static string Csv(string? v)
    {
        var s = v ?? "";
        if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }

    private static string BuildCollaborationEmailHtml() =>
        """
        <!DOCTYPE html><html><body style="font-family:Arial,sans-serif;">
        <h2>Congratulations!</h2>
        <p>Your ShareBizz account has been upgraded with the <strong>Collaborator Feature</strong>.</p>
        <p><a href="https://sharebizz.com/login/customer.php">Login to Your Account</a></p>
        <p>Best regards,<br>ShareBizz Team</p>
        </body></html>
        """;

    private sealed record ReferralSummary(
        decimal TotalAmount,
        decimal PaidAmount,
        DateTime? LastPaymentDate,
        int EarningCount,
        int RejectedCount);
}
