using System.Globalization;
using Microsoft.EntityFrameworkCore;
using MiniWebsite.Application.Admin.ManageReferrals.Dtos;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Domain.Common;
using MiniWebsite.Domain.Entities;
using MiniWebsite.Domain.Enums;
using MiniWebsite.Application.Common.Extensions;

namespace MiniWebsite.Application.Admin.ManageReferrals;

public class AdminManageReferralsService : IAdminManageReferralsService
{
    private const int MaxCommentLength = 1000;

    private readonly IApplicationDbContext _db;

    public AdminManageReferralsService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ApiResult<ManageReferralsPageDto>> ListAsync(ManageReferralsQuery query, CancellationToken ct = default)
    {
        query.Page = query.Page < 1 ? 1 : query.Page;
        query.PageSize = query.PageSize is < 1 or > 100 ? 10 : query.PageSize;

        var q = _db.ReferralEarnings.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLower();
            var compact = s.Replace(" ", "");
            var matchedIds = _db.Users
                .Where(u => u.UserCode.Contains(compact))
                .Select(u => u.Id);
            q = q.Where(r =>
                r.ReferrerEmail.Contains(s)
                || r.ReferredEmail.Contains(s)
                || (r.CommissionCode != null && r.CommissionCode.Contains(s))
                || (r.ReferrerUserId != null && matchedIds.Contains(r.ReferrerUserId.Value))
                || (r.ReferredUserId != null && matchedIds.Contains(r.ReferredUserId.Value)));
        }

        var totalCount = await q.CountAsync(ct);
        var earnings = await q
            .OrderByDescending(r => r.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        if (earnings.Count == 0)
        {
            return ApiResult<ManageReferralsPageDto>.Ok(new ManageReferralsPageDto(
                [], totalCount, query.Page, query.PageSize));
        }

        var referredIds = earnings.Where(e => e.ReferredUserId != null).Select(e => e.ReferredUserId!.Value).Distinct().ToList();
        var allIds = earnings.Where(e => e.ReferrerUserId != null).Select(e => e.ReferrerUserId!.Value)
            .Concat(referredIds).Distinct().ToList();

        var codeById = await _db.Users.AsNoTracking()
            .Where(u => allIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => UserCodeGenerator.FormatDisplay(u.UserCode), ct);

        var linkedInvoiceIds = earnings.Where(e => e.InvoiceId != null).Select(e => e.InvoiceId!.Value).Distinct().ToList();
        var invoices = await _db.Invoices.AsNoTracking()
                .Where(i => (i.UserId != null && referredIds.Contains(i.UserId.Value)) || linkedInvoiceIds.Contains(i.Id))
                .Select(i => new OrderFacts(
                    i.Id,
                    i.UserId,
                    i.InvoiceDate,
                    i.ServiceName,
                    i.PlanName,
                    i.PaymentType,
                    i.PaymentStatus,
                    i.OriginalAmount,
                    i.PromoDiscount,
                    i.DiscountAmount,
                    i.SubTotal,
                    i.IgstAmount + i.CgstAmount + i.SgstAmount,
                    i.TotalAmount))
                .ToListAsync(ct);
        var invoiceById = invoices.ToDictionary(i => i.Id);
        var invoicesByUser = invoices
            .Where(i => i.UserId != null)
            .GroupBy(i => i.UserId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(i => (i.Id, i.InvoiceDate)).ToList());

        var paidCardUsers = (await _db.Cards.AsNoTracking()
                .Where(c => c.UserId != null
                            && referredIds.Contains(c.UserId.Value)
                            && c.PaymentStatus == "Success")
                .Select(c => c.UserId!.Value)
                .Distinct()
                .ToListAsync(ct))
            .ToHashSet();

        var earningIds = earnings.Select(e => e.Id).ToList();
        var payments = await _db.ReferralPayments.AsNoTracking()
            .Where(p => earningIds.Contains(p.ReferralEarningId))
            .Select(p => new { p.ReferralEarningId, p.Amount, p.PaymentDate })
            .ToListAsync(ct);
        var paymentsByEarning = payments
            .GroupBy(p => p.ReferralEarningId)
            .ToDictionary(g => g.Key, g => (Paid: g.Sum(x => x.Amount), Last: g.Max(x => x.PaymentDate)));

        var rows = earnings.Select(e =>
        {
            var candidates = e.ReferredUserId is { } referredId ? invoicesByUser.GetValueOrDefault(referredId) : null;
            var orderId = ReferralOrderMatching.ResolveInvoiceId(e.InvoiceId, e.ReferralDate, candidates ?? []);
            var order = orderId is int id ? invoiceById.GetValueOrDefault(id) : null;

            paymentsByEarning.TryGetValue(e.Id, out var pay);
            var pending = Math.Max(0, e.Amount - pay.Paid);
            var isRejected = string.Equals(e.Status, "Rejected", StringComparison.OrdinalIgnoreCase);
            var isPaid = string.Equals(e.Status, "Paid", StringComparison.OrdinalIgnoreCase)
                         || (e.Amount > 0 && pending <= 0);
            var customerPaid = (e.ReferredUserId is { } paidCheckId && paidCardUsers.Contains(paidCheckId))
                               || (order != null && IsPaid(order.PaymentStatus))
                               || isPaid
                               || pay.Paid > 0;

            var commission = isRejected
                ? "Rejected"
                : e.Amount <= 0 || !customerPaid
                    ? "Not Eligible"
                    : "Eligible";

            string sbStatus;
            string sbTone;
            if (isRejected)
            {
                sbStatus = "Rejected";
                sbTone = "danger";
            }
            else if (isPaid)
            {
                var paidOn = e.PaymentDate ?? (pay.Paid > 0 ? pay.Last : (DateTime?)null);
                sbStatus = paidOn.HasValue
                    ? "Paid on " + paidOn.Value.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)
                    : "Paid";
                sbTone = "ok";
            }
            else
            {
                sbStatus = "Pending";
                sbTone = "warn";
            }

            var isRenewal = order?.PaymentType?.Contains("renew", StringComparison.OrdinalIgnoreCase) == true;
            var net = order == null
                ? 0
                : order.SubTotal > 0
                    ? order.SubTotal
                    : Math.Max(0, order.OriginalAmount - order.PromoDiscount - order.DiscountAmount);

            return new ManageReferralRowDto(
                e.Id,
                e.CommissionCode ?? "",
                e.ReferrerUserId is { } referrerCodeId ? codeById.GetValueOrDefault(referrerCodeId, "") : "",
                e.ReferrerEmail,
                e.ReferredUserId is { } referredCodeId ? codeById.GetValueOrDefault(referredCodeId, e.ReferredEmail) : e.ReferredEmail,
                e.ReferredEmail,
                order == null ? "" : Describe(order.ServiceName, order.PlanName),
                order == null ? "" : FormatNumber(order.OriginalAmount),
                order == null ? "" : FormatNumber(order.PromoDiscount),
                order == null ? "" : FormatNumber(net),
                order == null ? "" : FormatRupees(order.Gst),
                order == null ? "" : FormatRupees(order.TotalAmount),
                e.Amount,
                FormatRupees(isRenewal ? 0 : e.Amount),
                isRenewal ? FormatRupees(e.Amount) : "Not Applicable",
                commission,
                sbStatus,
                sbTone,
                pay.Paid,
                pending,
                commission == "Eligible" && pending > 0,
                pay.Paid > 0,
                e.AdminComment ?? "");
        }).ToList();

        return ApiResult<ManageReferralsPageDto>.Ok(new ManageReferralsPageDto(
            rows, totalCount, query.Page, query.PageSize));
    }

    public async Task<ApiResult> UpdateCommentAsync(int referralId, string? comment, CancellationToken ct = default)
    {
        var text = (comment ?? "").Trim();
        if (text.Length > MaxCommentLength)
            return ApiResult.Fail($"Comment must be {MaxCommentLength} characters or fewer.");

        var earning = await _db.ReferralEarnings.FirstOrDefaultAsync(r => r.Id == referralId, ct);
        if (earning == null)
            return ApiResult.Fail("Commission not found.");

        earning.AdminComment = text.Length == 0 ? null : text;
        await _db.SaveChangesAsync(ct);
        return ApiResult.Ok("Comment saved.");
    }

    public async Task<ApiResult<ReferrerPaymentDetailsDto>> GetReferrerDetailsAsync(
        string referrerEmail,
        CancellationToken ct = default)
    {
        var email = Normalize(referrerEmail);
        if (string.IsNullOrWhiteSpace(email))
            return ApiResult<ReferrerPaymentDetailsDto>.Fail("Missing referrer email");

        var referrer = await _db.Users.AsNoTracking()
            .WhereRole(UserRole.Customer)
            .FirstOrDefaultAsync(u => u.Email == email, ct);

        var referrerId = await _db.FindUserIdAsync(email, ct);
        var earnings = referrerId == null
            ? []
            : await _db.ReferralEarnings.AsNoTracking()
                .Where(r => r.ReferrerUserId == referrerId)
                .OrderByDescending(r => r.ReferralDate)
                .ToListAsync(ct);

        var paidMap = await GetPaidAmountsByReferralIdsAsync(earnings.Select(e => e.Id).ToList(), ct);
        var referredIds = earnings.Where(e => e.ReferredUserId != null).Select(e => e.ReferredUserId!.Value).Distinct().ToList();

        var nameMap = await _db.Users.AsNoTracking()
            .Where(u => referredIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

        var cards = await _db.Cards.AsNoTracking()
            .Where(c => c.UserId != null && referredIds.Contains(c.UserId.Value))
            .OrderByDescending(c => c.Id)
            .ToListAsync(ct);
        var cardMap = new Dictionary<int, Card>();
        foreach (var c in cards)
            cardMap.TryAdd(c.UserId!.Value, c);

        var lines = earnings.Select(e =>
        {
            paidMap.TryGetValue(e.Id, out var paid);
            var pending = Math.Max(0, e.Amount - paid);
            Card? card = null;
            string? name = null;
            if (e.ReferredUserId is { } referredId)
            {
                cardMap.TryGetValue(referredId, out card);
                nameMap.TryGetValue(referredId, out name);
            }

            string userPayLabel;
            string userPayTone;
            if (string.Equals(card?.DPaymentStatus, "Success", StringComparison.OrdinalIgnoreCase))
            {
                userPayTone = "ok";
                userPayLabel = card?.DPaymentDate is { } d
                    ? "Paid on " + d.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)
                    : "Paid";
            }
            else
            {
                userPayTone = "warn";
                userPayLabel = "Not Paid";
            }

            string statusLabel;
            string statusTone;
            if (pending <= 0)
            {
                statusLabel = "Fully Paid";
                statusTone = "ok";
            }
            else if (paid > 0)
            {
                statusLabel = "Partial";
                statusTone = "warn";
            }
            else
            {
                statusLabel = "Pending";
                statusTone = "warn";
            }

            return new ReferrerPaymentLineDto(
                e.Id,
                e.ReferredEmail,
                name ?? "Unknown",
                e.ReferralDate,
                userPayLabel,
                userPayTone,
                e.Amount,
                paid,
                pending,
                statusLabel,
                statusTone,
                pending > 0,
                paid > 0);
        }).ToList();

        return ApiResult<ReferrerPaymentDetailsDto>.Ok(new ReferrerPaymentDetailsDto(
            email,
            referrer?.FullName ?? email,
            lines));
    }

    public async Task<ApiResult<ReferralPaymentHistoryDto>> GetPaymentHistoryAsync(
        int referralId,
        CancellationToken ct = default)
    {
        var earning = await _db.ReferralEarnings.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == referralId, ct);
        if (earning == null)
            return ApiResult<ReferralPaymentHistoryDto>.Fail("Referral not found");

        var referrer = earning.ReferrerUserId == null ? null : await _db.Users.AsNoTracking()
            .WhereRole(UserRole.Customer)
            .FirstOrDefaultAsync(u => u.Id == earning.ReferrerUserId, ct);
        var referred = earning.ReferredUserId == null ? null : await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == earning.ReferredUserId, ct);

        var items = await LoadPaymentHistoryItemsAsync(referralId, ct);

        return ApiResult<ReferralPaymentHistoryDto>.Ok(new ReferralPaymentHistoryDto(
            referralId,
            referrer?.FullName ?? earning.ReferrerEmail,
            referred?.FullName ?? earning.ReferredEmail,
            earning.Amount,
            items));
    }

    public async Task<ApiResult> ProcessPaymentAsync(
        ProcessReferralPaymentRequest request,
        CancellationToken ct = default)
    {
        if (request.ReferralId <= 0)
            return ApiResult.Fail("Invalid referral id");
        if (request.Amount <= 0)
            return ApiResult.Fail("Amount must be greater than zero");
        if (string.IsNullOrWhiteSpace(request.TransactionNumber))
            return ApiResult.Fail("Transaction number is required");
        if (string.IsNullOrWhiteSpace(request.PaymentMethod))
            return ApiResult.Fail("Payment method is required");

        var earning = await _db.ReferralEarnings
            .FirstOrDefaultAsync(r => r.Id == request.ReferralId, ct);
        if (earning == null)
            return ApiResult.Fail("Referral not found");

        var processedBy = string.IsNullOrWhiteSpace(request.ProcessedBy) ? "admin" : request.ProcessedBy.Trim();
        var now = AppClock.Now;

        _db.ReferralPayments.Add(new ReferralPayment
        {
            ReferralEarningId = request.ReferralId,
            Amount = request.Amount,
            TransactionNumber = request.TransactionNumber.Trim(),
            PaymentMethod = request.PaymentMethod.Trim(),
            PaymentNotes = request.PaymentNotes ?? "",
            PaymentDate = now,
            ProcessedBy = processedBy,
        });

        await _db.SaveChangesAsync(ct);

        var paidMap = await GetPaidAmountsByReferralIdsAsync([request.ReferralId], ct);
        paidMap.TryGetValue(request.ReferralId, out var totalPaid);

        if (totalPaid >= earning.Amount)
        {
            earning.Status = "Paid";
            earning.PaymentDate = now;
        }
        else
        {
            earning.Status = "Partial";
            earning.PaymentDate = null;
        }

        await _db.SaveChangesAsync(ct);
        return ApiResult.Ok($"Payment of ₹{request.Amount:N0} processed successfully");
    }

    public async Task<ApiResult<ManageReferralBankDetailsDto>> GetBankDetailsAsync(
        string userEmail,
        CancellationToken ct = default)
    {
        var email = Normalize(userEmail);
        if (string.IsNullOrWhiteSpace(email))
            return ApiResult<ManageReferralBankDetailsDto>.Fail("Missing user email");

        try
        {
            var userId = await _db.FindUserIdAsync(email, ct);
            var row = userId == null ? null : await _db.UserBankDetails.AsNoTracking()
                .FirstOrDefaultAsync(b => b.UserId == userId, ct);

            return ApiResult<ManageReferralBankDetailsDto>.Ok(new ManageReferralBankDetailsDto(
                email,
                row?.AccountHolderName,
                row?.AccountNumber,
                row?.IfscCode,
                row?.BankName,
                row?.UpiId,
                row?.UpiName));
        }
        catch (Exception ex)
        {
            return ApiResult<ManageReferralBankDetailsDto>.Fail("Unable to load bank details: " + ex.Message);
        }
    }

    private async Task<Dictionary<int, decimal>> GetPaidAmountsByReferralIdsAsync(
        List<int> referralIds,
        CancellationToken ct)
    {
        var map = referralIds.ToDictionary(id => id, _ => 0m);
        if (referralIds.Count == 0) return map;

        try
        {
            var rows = await _db.ReferralPayments.AsNoTracking()
                .Where(p => referralIds.Contains(p.ReferralEarningId))
                .GroupBy(p => p.ReferralEarningId)
                .Select(g => new { ReferralId = g.Key, Paid = g.Sum(x => x.Amount) })
                .ToListAsync(ct);

            foreach (var r in rows)
                map[r.ReferralId] = r.Paid;
        }
        catch
        {
            // table may be missing in some envs
        }

        return map;
    }

    private async Task<IReadOnlyList<ReferralPaymentHistoryItemDto>> LoadPaymentHistoryItemsAsync(
        int referralId,
        CancellationToken ct)
    {
        try
        {
            var rows = await _db.ReferralPayments.AsNoTracking()
                .Where(p => p.ReferralEarningId == referralId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync(ct);

            return rows.Select(r => new ReferralPaymentHistoryItemDto(
                r.PaymentDate,
                r.Amount,
                r.TransactionNumber ?? "",
                r.PaymentMethod ?? "",
                r.PaymentNotes,
                r.ProcessedBy ?? "")).ToList();
        }
        catch
        {
            return [];
        }
    }

    private static bool IsPaid(string? status) =>
        string.Equals(status, "Success", StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, "Paid", StringComparison.OrdinalIgnoreCase);

    private static string Describe(string? serviceName, string? planName)
    {
        var name = (serviceName ?? "").Trim();
        var plan = (planName ?? "").Trim();
        if (plan.Length == 0 || name.Contains(plan, StringComparison.OrdinalIgnoreCase))
            return name;
        return name.Length == 0 ? plan : $"{name} ({plan})";
    }

    private static string FormatNumber(decimal value)
    {
        var rounded = decimal.Round(value, 2);
        return rounded == decimal.Truncate(rounded)
            ? decimal.Truncate(rounded).ToString(CultureInfo.InvariantCulture)
            : rounded.ToString("0.00", CultureInfo.InvariantCulture);
    }

    private static string FormatRupees(decimal value) => "₹ " + FormatNumber(value);

    private static string Normalize(string? value) => (value ?? "").Trim().ToLowerInvariant();

    private sealed record OrderFacts(
        int Id,
        int? UserId,
        DateTime InvoiceDate,
        string? ServiceName,
        string? PlanName,
        string? PaymentType,
        string? PaymentStatus,
        decimal OriginalAmount,
        decimal PromoDiscount,
        decimal DiscountAmount,
        decimal SubTotal,
        decimal Gst,
        decimal TotalAmount);
}
