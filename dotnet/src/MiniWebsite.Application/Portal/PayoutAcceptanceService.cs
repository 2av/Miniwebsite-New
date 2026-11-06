using System.Globalization;
using Microsoft.EntityFrameworkCore;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Domain.Common;
using MiniWebsite.Domain.Entities;

namespace MiniWebsite.Application.Portal;

public record PayoutInvoiceDto(
    int EarningId,
    int InvoiceId,
    string InvoiceNumber,
    decimal Amount,
    string Acceptance,
    string? ReferredEmail);

public interface IPayoutAcceptanceService
{
    Task EnsureDueAsync(int referrerUserId, CancellationToken ct = default);
    Task<ApiResult<IReadOnlyList<PayoutInvoiceDto>>> ListAsync(string email, CancellationToken ct = default);
    Task<ApiResult> AcceptAsync(string email, int earningId, CancellationToken ct = default);
    Task<ApiResult<IReadOnlyList<NotificationDto>>> NotificationsAsync(string email, CancellationToken ct = default);
}

public record NotificationDto(int Id, string Title, string Message, bool IsRead, string CreatedAt);

public class PayoutAcceptanceService : IPayoutAcceptanceService
{
    private readonly IApplicationDbContext _db;

    public PayoutAcceptanceService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task EnsureDueAsync(int referrerUserId, CancellationToken ct = default)
    {
        var referrer = await _db.Users.FirstOrDefaultAsync(u => u.Id == referrerUserId, ct);
        if (referrer == null) return;
        if (!CanReceivePayout(referrer)) return;

        var earnings = await _db.ReferralEarnings
            .Where(e => e.ReferrerUserId == referrer.Id
                && e.Status == "Pending"
                && e.Amount > 0
                && e.PayoutAcceptance == null)
            .ToListAsync(ct);
        if (earnings.Count == 0) return;

        var referredIds = earnings.Where(e => e.ReferredUserId != null).Select(e => e.ReferredUserId!.Value).Distinct().ToList();
        var paidUserIds = await _db.Invoices.AsNoTracking()
            .Where(i => i.UserId != null && referredIds.Contains(i.UserId.Value)
                && (i.PaymentStatus == "Success" || i.PaymentStatus == "Paid")
                && i.PaymentType != "Referral Payout")
            .Select(i => i.UserId!.Value)
            .Distinct()
            .ToListAsync(ct);

        var now = AppClock.Now;
        var created = false;
        foreach (var earning in earnings)
        {
            if (earning.ReferredUserId is not int referredId || !paidUserIds.Contains(referredId))
                continue;

            var invoice = new Invoice
            {
                InvoiceNumber = await NextInvoiceNumberAsync(ct),
                InvoiceDate = now,
                UserId = referrer.Id,
                UserEmail = referrer.Email,
                UserName = referrer.FullName,
                UserContact = referrer.Phone ?? "",
                BillingName = referrer.FullName,
                BillingEmail = referrer.Email,
                BillingContact = referrer.Phone ?? "",
                BillingAddress = referrer.Address.AddressLine ?? "",
                BillingState = referrer.Address.State ?? "",
                BillingCity = referrer.Address.City ?? "",
                BillingPincode = referrer.Address.Pincode ?? "",
                OriginalAmount = earning.Amount,
                FinalAmount = earning.Amount,
                SubTotal = earning.Amount,
                UnitPrice = earning.Amount,
                TotalPrice = earning.Amount,
                TotalAmount = earning.Amount,
                PaymentStatus = "Pending",
                PaymentType = "Referral Payout",
                ServiceName = "Referral payout",
                ServiceDescription = "Referral earning " + (earning.CommissionCode ?? earning.Id.ToString(CultureInfo.InvariantCulture)),
                ReferenceNumber = "PAYOUT-" + earning.Id.ToString(CultureInfo.InvariantCulture),
                RazorpayOrderId = "",
                RazorpayPaymentId = "",
                RazorpaySignature = "",
                CreatedAt = now,
                UpdatedAt = now,
            };
            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync(ct);

            earning.PayoutInvoiceId = invoice.Id;
            earning.PayoutAcceptance = PayoutAcceptanceStatus.Awaiting;
            _db.Notifications.Add(new Notification
            {
                Audience = "customer",
                Type = "payout",
                Title = "Invoice ready",
                Message = "A referral payout invoice is ready. Accept it on your dashboard to process payment.",
                UserEmail = referrer.Email,
                UserId = referrer.Id,
                IsRead = false,
                CreatedAt = now,
            });
            created = true;
        }

        if (created) await _db.SaveChangesAsync(ct);
    }

    public async Task<ApiResult<IReadOnlyList<PayoutInvoiceDto>>> ListAsync(string email, CancellationToken ct = default)
    {
        var user = await FindAsync(email, ct);
        if (user == null) return ApiResult<IReadOnlyList<PayoutInvoiceDto>>.Fail("User not found.");
        await EnsureDueAsync(user.Id, ct);

        var rows = await _db.ReferralEarnings.AsNoTracking()
            .Where(e => e.ReferrerUserId == user.Id && e.PayoutInvoiceId != null && e.PayoutAcceptance == PayoutAcceptanceStatus.Awaiting)
            .Join(_db.Invoices.AsNoTracking(), e => e.PayoutInvoiceId, i => i.Id, (e, i) => new PayoutInvoiceDto(
                e.Id, i.Id, i.InvoiceNumber, e.Amount, e.PayoutAcceptance ?? PayoutAcceptanceStatus.Awaiting, e.ReferredEmail))
            .ToListAsync(ct);
        return ApiResult<IReadOnlyList<PayoutInvoiceDto>>.Ok(rows);
    }

    public async Task<ApiResult> AcceptAsync(string email, int earningId, CancellationToken ct = default)
    {
        var user = await FindAsync(email, ct);
        if (user == null) return ApiResult.Fail("User not found.");
        if (!CanReceivePayout(user))
            return ApiResult.Fail("Payout opens after your documents are approved.");

        var earning = await _db.ReferralEarnings.FirstOrDefaultAsync(
            e => e.Id == earningId && e.ReferrerUserId == user.Id, ct);
        if (earning == null || earning.PayoutAcceptance != PayoutAcceptanceStatus.Awaiting || earning.PayoutInvoiceId is not int invoiceId)
            return ApiResult.Fail("This invoice is not waiting for your acceptance.");

        var invoice = await _db.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId, ct);
        if (invoice == null) return ApiResult.Fail("Invoice was not found.");

        var now = AppClock.Now;
        earning.PayoutAcceptance = PayoutAcceptanceStatus.Processed;
        earning.Status = "Paid";
        earning.PaymentDate = now;
        invoice.PaymentStatus = "Success";
        invoice.PaymentDate = now;
        invoice.UpdatedAt = now;
        _db.ReferralPayments.Add(new ReferralPayment
        {
            ReferralEarningId = earning.Id,
            Amount = earning.Amount,
            PaymentMethod = "Dashboard acceptance",
            PaymentNotes = "User accepted the invoice",
            PaymentDate = now,
            ProcessedBy = user.Email,
        });
        _db.Notifications.Add(new Notification
        {
            Audience = "customer",
            Type = "payout",
            Title = "Payment processed",
            Message = "Your referral payout was processed after you accepted the invoice.",
            UserEmail = user.Email,
            UserId = user.Id,
            IsRead = false,
            CreatedAt = now,
        });
        await _db.SaveChangesAsync(ct);
        return ApiResult.Ok("Invoice accepted. Payment processed.");
    }

    public async Task<ApiResult<IReadOnlyList<NotificationDto>>> NotificationsAsync(string email, CancellationToken ct = default)
    {
        var user = await FindAsync(email, ct);
        if (user == null) return ApiResult<IReadOnlyList<NotificationDto>>.Fail("User not found.");
        var rows = await _db.Notifications.AsNoTracking()
            .Where(n => n.UserId == user.Id)
            .OrderByDescending(n => n.Id)
            .Take(20)
            .Select(n => new NotificationDto(
                n.Id, n.Title, n.Message, n.IsRead, n.CreatedAt.ToString("o", CultureInfo.InvariantCulture)))
            .ToListAsync(ct);
        return ApiResult<IReadOnlyList<NotificationDto>>.Ok(rows);
    }

    private static bool CanReceivePayout(User user)
    {
        var prefix = user.AssignedRole?.Prefix;
        if (string.Equals(prefix, RolePrefixes.ShareBizzUser, StringComparison.OrdinalIgnoreCase))
        {
            return user.OnboardingStage == OnboardingStage.Approved
                && OnboardingAccess.ReferralUnlocked(prefix, user.ReferralEligibleAt, AppClock.Now);
        }

        return OnboardingAccess.PaysMembership(prefix) && user.OnboardingStage == OnboardingStage.ReadyToSell;
    }

    private Task<User?> FindAsync(string email, CancellationToken ct)
    {
        var key = (email ?? "").Trim().ToLowerInvariant();
        return _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == key, ct);
    }

    private async Task<string> NextInvoiceNumberAsync(CancellationToken ct)
    {
        string number;
        do number = UserCodeGenerator.Generate("INV");
        while (await _db.Invoices.AnyAsync(i => i.InvoiceNumber == number, ct));
        return number;
    }
}
