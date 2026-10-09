using System.Globalization;
using Microsoft.EntityFrameworkCore;
using MiniWebsite.Application.Admin.AllOrders.Dtos;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Domain.Enums;
using MiniWebsite.Domain.Common;

namespace MiniWebsite.Application.Admin.AllOrders;

public class AdminAllOrdersService : IAdminAllOrdersService
{
    private readonly IApplicationDbContext _db;

    public AdminAllOrdersService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ApiResult<AllOrdersPageDto>> ListAsync(AllOrdersQuery query, CancellationToken ct = default)
    {
        query.Page = query.Page < 1 ? 1 : query.Page;
        query.PageSize = query.PageSize is < 1 or > 100 ? 10 : query.PageSize;
        var offset = (query.Page - 1) * query.PageSize;
        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();

        try
        {
            var q = _db.Invoices.AsNoTracking().AsQueryable();
            if (search != null)
            {
                var term = search.ToLowerInvariant();
                q = q.Where(i =>
                    i.UserEmail.ToLower().Contains(term)
                    || i.UserName.ToLower().Contains(term)
                    || i.InvoiceNumber.ToLower().Contains(term));
            }

            var totalCount = await q.CountAsync(ct);
            var invoices = await q
                .OrderByDescending(i => i.Id)
                .Skip(offset)
                .Take(query.PageSize)
                .Select(i => new InvoiceLite(
                    i.Id,
                    i.UserEmail,
                    i.UserName,
                    i.InvoiceNumber,
                    i.CardId,
                    i.ServiceName,
                    i.PaymentType,
                    i.PaymentStatus,
                    i.InvoiceDate,
                    i.TotalAmount))
                .ToListAsync(ct);

            var emails = invoices
                .Select(i => Normalize(i.UserEmail))
                .Where(e => e.Length > 0)
                .Distinct()
                .ToList();

            var users = emails.Count == 0
                ? []
                : await _db.Users.AsNoTracking()
                    .Where(u => emails.Contains(u.Email.ToLower()))
                    .Select(u => new { u.Id, Email = u.Email.ToLower(), u.AssignedRole!.Prefix })
                    .ToListAsync(ct);

            var customerByEmail = users
                .Where(u => RolePrefixes.ToUserRole(u.Prefix) == UserRole.Customer)
                .GroupBy(u => u.Email)
                .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

            var franchiseeByEmail = users
                .Where(u => RolePrefixes.ToUserRole(u.Prefix) == UserRole.Franchisee)
                .GroupBy(u => u.Email)
                .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

            var rows = invoices.Select(inv =>
            {
                var email = Normalize(inv.UserEmail);
                var isFranchisee = IsFranchiseeInvoice(inv);
                string userIdDisplay = "-";

                if (isFranchisee && franchiseeByEmail.TryGetValue(email, out var frId))
                    userIdDisplay = "FR - " + frId.ToString(CultureInfo.InvariantCulture);
                else if (customerByEmail.TryGetValue(email, out var customerId))
                    userIdDisplay = customerId.ToString("D5", CultureInfo.InvariantCulture);

                var mwId = inv.CardId is > 0 ? inv.CardId : null;
                var amount = inv.TotalAmount;
                var (label, tone, paidOn) = ResolvePaymentStatus(inv.PaymentStatus, inv.InvoiceDate);

                return new AllOrderRowDto(
                    inv.Id,
                    userIdDisplay,
                    mwId?.ToString(CultureInfo.InvariantCulture),
                    label,
                    tone,
                    paidOn,
                    amount,
                    "₹" + amount.ToString("N2", CultureInfo.CreateSpecificCulture("en-IN")));
            }).ToList();

            return ApiResult<AllOrdersPageDto>.Ok(new AllOrdersPageDto(
                rows,
                totalCount,
                query.Page,
                query.PageSize));
        }
        catch (Exception ex)
        {
            return ApiResult<AllOrdersPageDto>.Fail("Unable to load orders: " + ex.Message);
        }
    }

    private static bool IsFranchiseeInvoice(InvoiceLite inv)
    {
        if (string.Equals(inv.PaymentType, "Franchisee", StringComparison.OrdinalIgnoreCase))
            return true;
        return !string.IsNullOrWhiteSpace(inv.ServiceName)
               && inv.ServiceName.Contains("Franchisee", StringComparison.OrdinalIgnoreCase);
    }

    private static (string Label, string Tone, string? PaidOn) ResolvePaymentStatus(
        string? paymentStatus, DateTime? invoiceDate)
    {
        var paidDateNorm = FormatInvoiceDate(invoiceDate);
        var ps = (paymentStatus ?? string.Empty).Trim();
        if (ps.Length == 0)
            return ("-", "neutral", null);

        if (ps.StartsWith("Paid on", StringComparison.OrdinalIgnoreCase))
        {
            var paidDate = ps["Paid on".Length..].Trim();
            if (paidDate.Length == 0) paidDate = paidDateNorm ?? "";
            return ("Paid", "ok", string.IsNullOrWhiteSpace(paidDate) ? null : paidDate);
        }

        if (ps.Equals("Success", StringComparison.OrdinalIgnoreCase)
            || ps.Equals("Paid", StringComparison.OrdinalIgnoreCase))
        {
            return ("Paid", "ok", paidDateNorm);
        }

        if (ps.Equals("Failed", StringComparison.OrdinalIgnoreCase)
            || ps.Equals("Failure", StringComparison.OrdinalIgnoreCase))
            return ("Failed", "danger", null);

        if (ps.Equals("Pending", StringComparison.OrdinalIgnoreCase)
            || ps.Equals("Initiated", StringComparison.OrdinalIgnoreCase))
            return ("Pending", "warn", null);

        if (ps.Equals("Refunded", StringComparison.OrdinalIgnoreCase))
            return ("Refunded", "neutral", null);

        return (ps, "neutral", null);
    }

    private static string? FormatInvoiceDate(DateTime? invoiceDate)
    {
        if (!invoiceDate.HasValue) return null;
        if (invoiceDate.Value.Year <= 1) return null;
        return invoiceDate.Value.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
    }

    private static string Normalize(string? value) => (value ?? string.Empty).Trim().ToLowerInvariant();

    private sealed record InvoiceLite(
        int Id,
        string? UserEmail,
        string? UserName,
        string? InvoiceNumber,
        int? CardId,
        string? ServiceName,
        string? PaymentType,
        string? PaymentStatus,
        DateTime InvoiceDate,
        decimal TotalAmount);
}
