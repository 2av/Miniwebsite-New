using System.Globalization;
using Microsoft.EntityFrameworkCore;
using MiniWebsite.Application.Admin.Dashboard.Dtos;
using MiniWebsite.Application.Common;
using MiniWebsite.Application.Common.Extensions;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Domain.Common;
using MiniWebsite.Domain.Enums;

namespace MiniWebsite.Application.Admin.Dashboard;

public class AdminDashboardService : IAdminDashboardService
{
    private const int ExpiringWithinDays = 30;

    private static readonly (string Slug, string Prefix, string Label)[] Partners =
    [
        ("franchisee-partner", RolePrefixes.FranchisePartner, "Franchisee Partner"),
        ("print-shop-partner", RolePrefixes.PrintShopPartner, "Print Shop Partner"),
        ("digital-marketing-partner", RolePrefixes.DigitalMarketingPartner, "Digital Marketing Partner"),
        ("social-media-partner", RolePrefixes.SocialMediaPartner, "Social Media Partner"),
        ("field-sales-partner", RolePrefixes.FieldSalesPartner, "Field Sales Partner"),
        ("field-sales-team", RolePrefixes.FieldSalesTeam, "Field Sales Team"),
    ];

    private readonly IApplicationDbContext _db;

    public AdminDashboardService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ApiResult<AdminDashboardDto>> GetAsync(CancellationToken ct = default)
    {
        var today = AppClock.Today;
        var tomorrow = today.AddDays(1);
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var nextMonth = monthStart.AddMonths(1);
        var expiringUntil = today.AddDays(ExpiringWithinDays);
        var teamIds = _db.Users.WhereRole(UserRole.Team).Select(u => u.Id);
        var profileCards = _db.Cards.AsNoTracking()
            .Where(c => c.UserId == null || !teamIds.Contains(c.UserId.Value));

        var monthOrders = await _db.Invoices.AsNoTracking()
            .Where(i => i.InvoiceDate >= monthStart && i.InvoiceDate < nextMonth)
            .Select(i => i.InvoiceDate)
            .ToListAsync(ct);
        var paidRows = await _db.Invoices.AsNoTracking()
            .Where(i => i.PaymentStatus == "Success" || i.PaymentStatus == "Paid")
            .Where(i => (i.PaymentDate ?? i.InvoiceDate) >= monthStart && (i.PaymentDate ?? i.InvoiceDate) < nextMonth)
            .Select(i => new { When = i.PaymentDate ?? i.InvoiceDate, i.TotalAmount })
            .ToListAsync(ct);
        var newProfiles = await profileCards
            .Where(c => c.CreatedAt >= monthStart && c.CreatedAt < nextMonth)
            .Select(c => c.CreatedAt!.Value)
            .ToListAsync(ct);

        var todayOrders = monthOrders.Count(d => d >= today && d < tomorrow);
        var todayCollected = paidRows.Where(r => r.When >= today && r.When < tomorrow).Sum(r => r.TotalAmount);
        var todayProfiles = newProfiles.Count(d => d >= today && d < tomorrow);

        var cards = await profileCards
            .Select(c => new CardSnap(c.Id, c.CardStatus, c.ValidityDate, c.CreatedAt, c.CompanyName, c.DbCode, c.Slug))
            .ToListAsync(ct);
        var classified = cards.Select(c => Classify(c, today)).ToList();
        var activeProfiles = classified.Count(s => s == "Active");
        var suspendedProfiles = classified.Count(s => s == "Suspended");
        var expiredProfiles = classified.Count(s => s == "Expired");
        var expiringProfiles = cards.Count(c =>
        {
            if (Classify(c, today) != "Active") return false;
            var expiry = Expiry(c);
            return expiry.HasValue && expiry.Value >= today && expiry.Value <= expiringUntil;
        });

        var monthCommission = await _db.ReferralEarnings.AsNoTracking()
            .Where(e => e.ReferralDate >= monthStart && e.ReferralDate < nextMonth && e.Status != "Rejected")
            .SumAsync(e => (decimal?)e.Amount, ct) ?? 0;
        var openEarnings = await _db.ReferralEarnings.AsNoTracking()
            .Where(e => (e.Status == "Pending" || e.Status == "Partial") && e.Amount > 0)
            .Select(e => new { e.Amount, e.ReferredUserId })
            .ToListAsync(ct);
        var referredIds = openEarnings.Where(e => e.ReferredUserId != null).Select(e => e.ReferredUserId!.Value).Distinct().ToList();
        var paidUsers = await CardSales.PaidUserIdsAsync(_db, referredIds, ct);
        var eligible = openEarnings.Where(e => e.ReferredUserId is { } id && paidUsers.Contains(id)).ToList();

        var partnerPrefixes = Partners.Select(p => p.Prefix).ToArray();
        var partnerRoles = await _db.Roles.AsNoTracking()
            .Where(r => r.Prefix != null && partnerPrefixes.Contains(r.Prefix))
            .Select(r => new { r.RoleId, r.Prefix })
            .ToListAsync(ct);
        var partnerRoleIds = partnerRoles.Select(r => r.RoleId).ToList();
        var partnerCounts = partnerRoleIds.Count == 0
            ? []
            : await _db.Users.AsNoTracking()
                .Where(u => partnerRoleIds.Contains(u.RoleId))
                .GroupBy(u => u.RoleId)
                .Select(g => new { RoleId = g.Key, Count = g.Count() })
                .ToListAsync(ct);
        var countByRole = partnerCounts.ToDictionary(x => x.RoleId, x => x.Count);
        var partners = Partners.Select(p =>
        {
            var roleId = partnerRoles.FirstOrDefault(r => r.Prefix == p.Prefix)?.RoleId;
            var count = roleId is int id && countByRole.TryGetValue(id, out var n) ? n : 0;
            return new PartnerCountDto(p.Slug, p.Label, count);
        }).ToList();

        var partnerInvoices = partnerRoleIds.Count == 0
            ? 0
            : await _db.Invoices.AsNoTracking().CountAsync(i =>
                i.UserId != null
                && _db.Users.Any(u => u.Id == i.UserId && partnerRoleIds.Contains(u.RoleId))
                && (i.PaymentStatus == "Pending" || i.PaymentStatus == "Created")
                && i.PaymentType != PlanCommissionValues.CategoryDbProfile, ct);

        var verificationRows = await _db.FranchiseVerifications.AsNoTracking()
            .OrderByDescending(v => v.Id)
            .Select(v => new { v.UserId, v.Status })
            .ToListAsync(ct);
        var seenUsers = new HashSet<int>();
        var documentsPending = 0;
        foreach (var row in verificationRows)
        {
            if (!seenUsers.Add(row.UserId)) continue;
            if (IsPendingReview(row.Status)) documentsPending++;
        }

        var customers = _db.Users.AsNoTracking().WhereRole(UserRole.Customer);
        var customerTotal = await customers.CountAsync(ct);
        var customerActive = await customers.CountAsync(u => u.Status == "ACTIVE" || u.Status == "Active", ct);

        var recentOrders = await _db.Invoices.AsNoTracking()
            .OrderByDescending(i => i.Id)
            .Take(6)
            .Select(i => new
            {
                i.Id,
                i.OrderCode,
                i.UserName,
                i.UserEmail,
                i.TotalAmount,
                i.PaymentStatus,
                i.PaymentDate,
                i.InvoiceDate,
            })
            .ToListAsync(ct);
        var recentProfiles = cards
            .OrderByDescending(c => c.CreatedAt ?? DateTime.MinValue)
            .ThenByDescending(c => c.Id)
            .Take(6)
            .Select(c => new RecentProfileDto(
                c.Id,
                DbId(c),
                string.IsNullOrWhiteSpace(c.CompanyName) ? "Unnamed" : c.CompanyName.Trim(),
                Classify(c, today),
                FormatDay(c.CreatedAt)))
            .ToList();

        return ApiResult<AdminDashboardDto>.Ok(new AdminDashboardDto(
            today.ToString("dd MMM yyyy", CultureInfo.InvariantCulture),
            new PeriodStatsDto(todayOrders, todayCollected, todayProfiles),
            new PeriodStatsDto(monthOrders.Count, paidRows.Sum(r => r.TotalAmount), newProfiles.Count),
            monthCommission,
            openEarnings.Sum(e => e.Amount),
            new DashboardQueueDto(
                partnerInvoices,
                eligible.Count,
                eligible.Sum(e => e.Amount),
                documentsPending,
                suspendedProfiles,
                expiringProfiles),
            new UserSplitDto(customerActive, Math.Max(0, customerTotal - customerActive)),
            new ProfileSplitDto(activeProfiles, suspendedProfiles, expiredProfiles),
            partners,
            recentOrders.Select(i => new RecentOrderDto(
                i.Id,
                UserCodeGenerator.FormatDisplay(i.OrderCode),
                FirstName(i.UserName, i.UserEmail),
                i.TotalAmount,
                PaymentLabel(i.PaymentStatus, i.PaymentDate),
                FormatDay(i.InvoiceDate))).ToList(),
            recentProfiles));
    }

    private static string Classify(CardSnap card, DateTime today)
    {
        var raw = (card.CardStatus ?? "").Trim();
        if (raw.Equals("Suspended", StringComparison.OrdinalIgnoreCase)
            || raw.Equals("Inactive", StringComparison.OrdinalIgnoreCase)
            || raw.Equals("InActive", StringComparison.OrdinalIgnoreCase))
            return "Suspended";

        var expiry = Expiry(card);
        if (expiry.HasValue && expiry.Value < today) return "Expired";
        return "Active";
    }

    private static DateTime? Expiry(CardSnap card)
    {
        if (card.ValidityDate.HasValue) return card.ValidityDate.Value.Date;
        if (card.CreatedAt.HasValue) return card.CreatedAt.Value.Date.AddYears(1);
        return null;
    }

    private static bool IsPendingReview(string? status)
    {
        var value = (status ?? "").Trim().ToLowerInvariant();
        return value is "pending" or "submitted" or "pending review";
    }

    private static string PaymentLabel(string? status, DateTime? paidOn)
    {
        if (string.Equals(status, "Rejected", StringComparison.OrdinalIgnoreCase)) return "Rejected";
        if (string.Equals(status, "Success", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Paid", StringComparison.OrdinalIgnoreCase))
        {
            var when = paidOn is { Year: > 1 } d ? d : AppClock.Today;
            return "Paid on " + when.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
        }

        return "Pending";
    }

    private static string DbId(CardSnap card)
    {
        var code = UserCodeGenerator.FormatDbId(string.IsNullOrWhiteSpace(card.DbCode) ? card.Slug : card.DbCode);
        return string.IsNullOrWhiteSpace(code) ? "—" : code;
    }

    private static string FirstName(string? name, string? email)
    {
        if (!string.IsNullOrWhiteSpace(name)) return name.Trim();
        return string.IsNullOrWhiteSpace(email) ? "—" : email.Trim();
    }

    private static string FormatDay(DateTime? value) =>
        value is { Year: > 1 } d ? d.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture) : "—";

    private sealed record CardSnap(
        int Id,
        string? CardStatus,
        DateTime? ValidityDate,
        DateTime? CreatedAt,
        string? CompanyName,
        string? DbCode,
        string? Slug);
}
