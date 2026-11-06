using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Application.Payments.Dtos;
using MiniWebsite.Application.Portal;
using MiniWebsite.Domain.Common;
using MiniWebsite.Domain.Entities;

namespace MiniWebsite.Application.Payments;

public interface IMembershipCheckoutService
{
    Task<ApiResult<MembershipCheckoutPageDto>> GetPageAsync(string email, CancellationToken ct = default);
    Task<ApiResult<DbProfileCheckoutResultDto>> CheckoutAsync(MembershipCheckoutRequest request, CancellationToken ct = default);
    Task<ApiResult> VerifyAsync(MembershipVerifyRequest request, CancellationToken ct = default);
}

public record MembershipCheckoutPageDto(
    string FeeLabel,
    bool AlreadyPaid,
    string SellerState,
    DbProfileBillingDto Billing,
    IReadOnlyList<DbProfilePlanDto> Plans);

public record MembershipCheckoutRequest(
    string Email,
    int PlanId,
    string? GstNumber,
    string Name,
    string BillingEmail,
    string Phone,
    string Address,
    string State,
    string City,
    string Pincode,
    bool Agreed);

public record MembershipVerifyRequest(
    string Email,
    int InvoiceId,
    string RazorpayOrderId,
    string RazorpayPaymentId,
    string RazorpaySignature);

public class MembershipCheckoutService : IMembershipCheckoutService
{
    private const decimal CgstRate = 0.09m;
    private const decimal SgstRate = 0.09m;
    private const decimal IgstRate = 0.18m;
    private static readonly Regex GstinPattern = new(
        @"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly IApplicationDbContext _db;
    private readonly IPaymentGateway _gateway;

    public MembershipCheckoutService(IApplicationDbContext db, IPaymentGateway gateway)
    {
        _db = db;
        _gateway = gateway;
    }

    public async Task<ApiResult<MembershipCheckoutPageDto>> GetPageAsync(string email, CancellationToken ct = default)
    {
        var user = await FindAsync(email, ct);
        if (user == null) return ApiResult<MembershipCheckoutPageDto>.Fail("User not found.");
        if (!OnboardingAccess.PaysMembership(user.AssignedRole?.Prefix))
            return ApiResult<MembershipCheckoutPageDto>.Fail("This account does not pay a membership fee.");

        var plans = await _db.PricingPlans.AsNoTracking()
            .Where(p => p.PlanCategory == PlanCommissionValues.CategoryMembership && p.Status == PlanCommissionValues.StatusActive)
            .OrderBy(p => p.Id)
            .ToListAsync(ct);

        var paid = user.PaidAt != null
            || user.OnboardingStage is OnboardingStage.DocumentVerification
                or OnboardingStage.WaitingForReview
                or OnboardingStage.Approved
                or OnboardingStage.ReadyToSell;

        return ApiResult<MembershipCheckoutPageDto>.Ok(new MembershipCheckoutPageDto(
            OnboardingAccess.MembershipFeeLabel(user.AssignedRole?.Prefix),
            paid && user.OnboardingStage != OnboardingStage.PaymentDue && user.OnboardingStage != OnboardingStage.AccountOnly,
            DbProfileCheckoutService.SellerState,
            new DbProfileBillingDto(
                user.FullName,
                user.Email,
                user.Phone ?? "",
                user.Address.AddressLine ?? "",
                user.Address.State ?? "",
                user.Address.City ?? "",
                user.Address.Pincode ?? "",
                user.Address.Gst ?? ""),
            plans.Select(ToPlan).ToList()));
    }

    public async Task<ApiResult<DbProfileCheckoutResultDto>> CheckoutAsync(MembershipCheckoutRequest request, CancellationToken ct = default)
    {
        var formError = Validate(request);
        if (formError != null) return ApiResult<DbProfileCheckoutResultDto>.Fail(formError);

        var user = await FindAsync(request.Email, ct);
        if (user == null) return ApiResult<DbProfileCheckoutResultDto>.Fail("User not found.");
        if (!OnboardingAccess.PaysMembership(user.AssignedRole?.Prefix))
            return ApiResult<DbProfileCheckoutResultDto>.Fail("This account does not pay a membership fee.");
        if (user.OnboardingStage is not (OnboardingStage.PaymentDue or OnboardingStage.AccountOnly))
            return ApiResult<DbProfileCheckoutResultDto>.Fail("Membership is already paid.");

        var plan = await _db.PricingPlans.FirstOrDefaultAsync(p =>
            p.Id == request.PlanId
            && p.PlanCategory == PlanCommissionValues.CategoryMembership
            && p.Status == PlanCommissionValues.StatusActive, ct);
        if (plan == null) return ApiResult<DbProfileCheckoutResultDto>.Fail("Select a membership plan.");

        var quote = Quote(plan.OriginalPrice, plan.OfferedPrice, request.State);
        if (quote.FinalTotal <= 0)
            return ApiResult<DbProfileCheckoutResultDto>.Fail("This plan does not have a payable amount.");

        CreatePaymentOrderResult order;
        try
        {
            order = await _gateway.CreateOrderAsync(quote.FinalTotal, "mem-" + user.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), ct);
        }
        catch (Exception ex)
        {
            return ApiResult<DbProfileCheckoutResultDto>.Fail(ex.Message);
        }

        var gatewayReady = _gateway.IsConfigured;
        var now = AppClock.Now;
        var label = OnboardingAccess.MembershipFeeLabel(user.AssignedRole?.Prefix);
        var invoice = new Invoice
        {
            InvoiceNumber = await NextInvoiceNumberAsync(ct),
            InvoiceDate = now,
            UserId = user.Id,
            UserEmail = user.Email,
            UserName = user.FullName,
            UserContact = user.Phone ?? "",
            BillingName = request.Name.Trim(),
            BillingEmail = request.BillingEmail.Trim(),
            BillingContact = Digits(request.Phone),
            BillingAddress = request.Address.Trim(),
            BillingState = request.State.Trim(),
            BillingCity = request.City.Trim(),
            BillingPincode = Digits(request.Pincode),
            BillingGstNumber = NormalizeGst(request.GstNumber),
            OriginalAmount = quote.OriginalPrice,
            DiscountAmount = quote.Discount,
            FinalAmount = quote.FinalTotal,
            SubTotal = quote.SubTotal,
            UnitPrice = quote.OriginalPrice,
            TotalPrice = quote.SubTotal,
            IgstPercentage = quote.IntraState ? 0 : 18,
            IgstAmount = quote.IgstAmount,
            CgstAmount = quote.CgstAmount,
            SgstAmount = quote.SgstAmount,
            TotalAmount = quote.FinalTotal,
            RazorpayOrderId = order.OrderId,
            RazorpayPaymentId = gatewayReady ? "" : "bypass_" + Guid.NewGuid().ToString("N")[..12],
            RazorpaySignature = gatewayReady ? "" : "bypass",
            PaymentStatus = gatewayReady ? "Pending" : "Success",
            ServiceName = label,
            ServiceDescription = plan.PlanName,
            HsnSacCode = "998313",
            Quantity = 1,
            PaymentType = PlanCommissionValues.CategoryMembership,
            ReferenceNumber = order.OrderId,
            PlanName = plan.PlanName,
            PaymentDate = gatewayReady ? null : now,
            CreatedAt = now,
            UpdatedAt = now,
        };
        _db.Invoices.Add(invoice);
        if (!gatewayReady)
        {
            invoice.PaymentStatus = "Success";
            invoice.PaymentDate = now;
            OnboardingAccess.MarkMembershipPaid(user, now);
        }

        await _db.SaveChangesAsync(ct);
        return ApiResult<DbProfileCheckoutResultDto>.Ok(new DbProfileCheckoutResultDto(
            invoice.Id, order.OrderId, order.AmountInRupees, order.Currency,
            gatewayReady ? order.KeyId : "", plan.PlanName, !gatewayReady));
    }

    public async Task<ApiResult> VerifyAsync(MembershipVerifyRequest request, CancellationToken ct = default)
    {
        var user = await FindAsync(request.Email, ct);
        if (user == null) return ApiResult.Fail("User not found.");
        var invoice = await _db.Invoices.FirstOrDefaultAsync(i =>
            i.Id == request.InvoiceId && i.UserId == user.Id && i.RazorpayOrderId == request.RazorpayOrderId.Trim(), ct);
        if (invoice == null) return ApiResult.Fail("Payment record was not found.");
        if (invoice.PaymentStatus is "Success" or "Paid") return ApiResult.Ok("Payment already recorded.");

        var verified = await _gateway.VerifyPaymentAsync(
            request.RazorpayOrderId.Trim(), request.RazorpayPaymentId.Trim(), request.RazorpaySignature.Trim(), ct);
        if (!verified) return ApiResult.Fail("Payment could not be verified.");

        var now = AppClock.Now;
        invoice.RazorpayPaymentId = request.RazorpayPaymentId.Trim();
        invoice.RazorpaySignature = request.RazorpaySignature.Trim();
        invoice.PaymentStatus = "Success";
        invoice.PaymentDate = now;
        invoice.UpdatedAt = now;
        OnboardingAccess.MarkMembershipPaid(user, now);
        await _db.SaveChangesAsync(ct);
        return ApiResult.Ok("Payment received. Document verification is now open.");
    }

    private static DbProfilePlanDto ToPlan(PricingPlan plan)
    {
        var offeredGst = plan.OfferedPriceGst > 0 ? plan.OfferedPriceGst : PlanCommissionValues.WithGst(plan.OfferedPrice);
        var originalGst = plan.OriginalPriceGst > 0 ? plan.OriginalPriceGst : PlanCommissionValues.WithGst(plan.OriginalPrice);
        return new DbProfilePlanDto(
            plan.Id, plan.PlanName, plan.ValidityMonths, plan.PlanName,
            Round2(plan.OriginalPrice), Round2(plan.OfferedPrice), originalGst, offeredGst,
            originalGst > offeredGst ? originalGst - offeredGst : 0, null, null, null);
    }

    private static DbProfileQuoteDto Quote(decimal originalPrice, decimal offeredPrice, string? state)
    {
        var original = Round2(originalPrice);
        var offered = Round2(offeredPrice);
        if (offered > original) original = offered;
        var intra = string.Equals(state?.Trim(), DbProfileCheckoutService.SellerState, StringComparison.OrdinalIgnoreCase);
        var cgst = intra ? Round2(offered * CgstRate) : 0;
        var sgst = intra ? Round2(offered * SgstRate) : 0;
        var igst = intra ? 0 : Round2(offered * IgstRate);
        return new DbProfileQuoteDto(original, Round2(original - offered), 0, offered, cgst, sgst, igst, Round2(offered + cgst + sgst + igst), intra);
    }

    private static string? Validate(MembershipCheckoutRequest request)
    {
        if (!request.Agreed) return "Please agree to the Terms and Privacy Policy.";
        if (string.IsNullOrWhiteSpace(request.Name)) return "Enter your name.";
        if (string.IsNullOrWhiteSpace(request.BillingEmail) || !request.BillingEmail.Contains('@')) return "Enter a valid email.";
        if (Digits(request.Phone).Length < 10) return "Enter a valid phone number.";
        if (string.IsNullOrWhiteSpace(request.Address)) return "Enter your address.";
        if (string.IsNullOrWhiteSpace(request.State)) return "Enter your state.";
        if (string.IsNullOrWhiteSpace(request.City)) return "Enter your city.";
        if (Digits(request.Pincode).Length != 6) return "Enter a 6-digit pin code.";
        var gst = NormalizeGst(request.GstNumber);
        if (gst.Length > 0 && !GstinPattern.IsMatch(gst)) return "Enter a valid GST number or leave it blank.";
        return null;
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

    private static decimal Round2(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    private static string Digits(string? value) => new((value ?? "").Where(char.IsDigit).ToArray());
    private static string NormalizeGst(string? value) => (value ?? "").Trim().ToUpperInvariant();
}
