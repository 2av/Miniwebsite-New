using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Application.Payments.Dtos;
using MiniWebsite.Application.Portal;
using MiniWebsite.Application.Website;
using MiniWebsite.Application.Website.Dtos;
using MiniWebsite.Domain.Common;
using MiniWebsite.Domain.Entities;

namespace MiniWebsite.Application.Payments;

public class DbProfileCheckoutService : IDbProfileCheckoutService
{
    /// <summary>Company GST is registered in Haryana (06). Same-state bills split CGST and SGST.</summary>
    public const string SellerState = "Haryana";

    private const decimal CgstRate = 0.09m;
    private const decimal SgstRate = 0.09m;
    private const decimal IgstRate = 0.18m;

    private static readonly Regex GstinPattern = new(
        @"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private const string PendingPrefix = "db-profile-pending\n";

    private readonly IApplicationDbContext _db;
    private readonly IPaymentGateway _gateway;
    private readonly IWebsiteService _website;

    public DbProfileCheckoutService(IApplicationDbContext db, IPaymentGateway gateway, IWebsiteService website)
    {
        _db = db;
        _gateway = gateway;
        _website = website;
    }

    public async Task<ApiResult<DbProfileCheckoutPageDto>> GetPageAsync(string email, int cardId, CancellationToken ct = default)
    {
        if (cardId <= 0)
        {
            var payer = await FindUserAsync(email, ct);
            if (payer == null) return ApiResult<DbProfileCheckoutPageDto>.Fail("User not found.");
            return ApiResult<DbProfileCheckoutPageDto>.Ok(new DbProfileCheckoutPageDto(
                0,
                null,
                false,
                SellerState,
                BillingFromUser(payer),
                await LoadPlansAsync(ct)));
        }

        var loaded = await LoadOwnedCardAsync(email, cardId, ct);
        if (!loaded.Success) return ApiResult<DbProfileCheckoutPageDto>.Fail(loaded.Message ?? "DB Profile not found.");

        var (user, card) = loaded.Data;
        var plans = await LoadPlansAsync(ct);
        return ApiResult<DbProfileCheckoutPageDto>.Ok(new DbProfileCheckoutPageDto(
            card.Id,
            card.CompanyName,
            await IsCardPaidAsync(card.Id, ct),
            SellerState,
            BillingFrom(user, card),
            plans));
    }

    public async Task<ApiResult<DbProfilePromoResultDto>> ApplyPromoAsync(DbProfilePromoRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.PromoCode))
            return ApiResult<DbProfilePromoResultDto>.Fail("Enter a promo code.");

        var priced = await PriceAsync(request.Email, request.CardId, request.PlanId, request.PromoCode, request.State, ct);
        if (!priced.Success || priced.Data is null || priced.Data.Quote is null)
            return ApiResult<DbProfilePromoResultDto>.Fail(priced.Message ?? "Unable to apply this promo code.");

        return ApiResult<DbProfilePromoResultDto>.Ok(
            new DbProfilePromoResultDto("Promo code applied.", priced.Data.Quote),
            "Promo code applied.");
    }

    public async Task<ApiResult<DbProfileCheckoutResultDto>> CheckoutAsync(DbProfileCheckoutRequest request, CancellationToken ct = default)
    {
        var formError = ValidateForm(request);
        if (formError != null) return ApiResult<DbProfileCheckoutResultDto>.Fail(formError);

        var priced = await PriceAsync(
            request.Email, request.CardId, request.PlanId, request.PromoCode, request.State, ct);
        if (!priced.Success || priced.Data is null || priced.Data.Quote is null || priced.Data.Plan is null || priced.Data.User is null)
            return ApiResult<DbProfileCheckoutResultDto>.Fail(priced.Message ?? "Unable to start payment.");

        var isNewProfile = request.CardId <= 0;
        if (!isNewProfile && priced.Data.Card is null)
            return ApiResult<DbProfileCheckoutResultDto>.Fail("DB Profile not found.");

        Card? reserved = priced.Data.Card;
        string? pendingComment = null;
        if (isNewProfile)
        {
            if (string.IsNullOrWhiteSpace(request.CompName) || string.IsNullOrWhiteSpace(request.DisplayName))
                return ApiResult<DbProfileCheckoutResultDto>.Fail("Enter the business name and business URL.");

            var held = await HoldNameAsync(priced.Data.User, request.CompName, ct);
            if (!held.Success)
                return ApiResult<DbProfileCheckoutResultDto>.Fail(held.Message ?? "Business URL is not available.");
            reserved = held.Data;
            if (reserved == null)
                pendingComment = EncodePending(request.CompName, request.DisplayName);
        }

        if (priced.Data.Quote.FinalTotal <= 0)
            return ApiResult<DbProfileCheckoutResultDto>.Fail("This plan does not have a payable amount.");

        var gatewayReady = _gateway.IsConfigured;
        CreatePaymentOrderResult order;
        try
        {
            order = await _gateway.CreateOrderAsync(priced.Data.Quote.FinalTotal, $"dbp-{reserved?.Id ?? 0}", ct);
        }
        catch (Exception ex)
        {
            return ApiResult<DbProfileCheckoutResultDto>.Fail(ex.Message);
        }

        var quote = priced.Data.Quote;
        var plan = priced.Data.Plan;
        var user = priced.Data.User;
        var now = AppClock.Now;
        if (!gatewayReady && reserved == null)
        {
            var created = await CreateProfileAsync(user, request.CompName!, request.DisplayName, ct);
            if (!created.Success || created.Data is null)
                return ApiResult<DbProfileCheckoutResultDto>.Fail(created.Message ?? "Unable to save the DB Profile.");
            reserved = created.Data;
            pendingComment = null;
        }

        var invoice = new Invoice
        {
            InvoiceNumber = await NextInvoiceNumberAsync(ct),
            InvoiceDate = now,
            CardId = reserved?.Id,
            AdminComment = pendingComment,
            UserId = user.Id,
            UserEmail = user.Email,
            UserName = user.FullName,
            UserContact = user.Phone ?? "",
            // Frozen at checkout. Later address changes on the user do not rewrite this invoice.
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
            PromoDiscount = quote.PromoDiscount,
            PromoCode = string.IsNullOrWhiteSpace(request.PromoCode) ? null : request.PromoCode.Trim(),
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
            ServiceName = PlanCommissionValues.CategoryDbProfile,
            ServiceDescription = plan.PlanName,
            HsnSacCode = "998313",
            Quantity = 1,
            PaymentType = PlanCommissionValues.CategoryDbProfile,
            ReferenceNumber = order.OrderId,
            PlanName = plan.PlanName,
            PlanValidity = PricingLabel(plan.ValidityMonths),
            PaymentDate = gatewayReady ? null : now,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _db.Invoices.Add(invoice);
        if (!gatewayReady)
            await ApplyPaidEffectsAsync(invoice, reserved!, now, ct);

        await _db.SaveChangesAsync(ct);

        return ApiResult<DbProfileCheckoutResultDto>.Ok(new DbProfileCheckoutResultDto(
            invoice.Id,
            order.OrderId,
            order.AmountInRupees,
            order.Currency,
            gatewayReady ? order.KeyId : "",
            plan.PlanName,
            !gatewayReady,
            reserved?.Id));
    }

    public async Task<ApiResult<DbProfilePaidDto>> VerifyAsync(DbProfileVerifyRequest request, CancellationToken ct = default)
    {
        if (request.InvoiceId <= 0 || string.IsNullOrWhiteSpace(request.RazorpayOrderId)
            || string.IsNullOrWhiteSpace(request.RazorpayPaymentId)
            || string.IsNullOrWhiteSpace(request.RazorpaySignature))
            return ApiResult<DbProfilePaidDto>.Fail("Payment confirmation is incomplete.");

        var user = await FindUserAsync(request.Email, ct);
        if (user == null) return ApiResult<DbProfilePaidDto>.Fail("User not found.");

        var invoice = await _db.Invoices.FirstOrDefaultAsync(i =>
            i.Id == request.InvoiceId
            && i.UserId == user.Id
            && i.RazorpayOrderId == request.RazorpayOrderId.Trim(), ct);

        if (invoice == null) return ApiResult<DbProfilePaidDto>.Fail("Payment record was not found.");
        if (request.CardId > 0 && invoice.CardId is int linked && linked != request.CardId)
            return ApiResult<DbProfilePaidDto>.Fail("Payment record was not found.");

        if (IsInvoicePaid(invoice.PaymentStatus))
        {
            if (invoice.CardId is not int paidCardId)
                return ApiResult<DbProfilePaidDto>.Fail("Payment record was not found.");
            return ApiResult<DbProfilePaidDto>.Ok(new DbProfilePaidDto(paidCardId), "Payment already recorded.");
        }

        var verified = await _gateway.VerifyPaymentAsync(
            request.RazorpayOrderId.Trim(),
            request.RazorpayPaymentId.Trim(),
            request.RazorpaySignature.Trim(),
            ct);
        if (!verified) return ApiResult<DbProfilePaidDto>.Fail("Payment could not be verified.");

        Card? card = null;
        if (invoice.CardId is int cardId && cardId > 0)
        {
            var loaded = await LoadOwnedCardAsync(request.Email, cardId, ct);
            if (!loaded.Success) return ApiResult<DbProfilePaidDto>.Fail(loaded.Message ?? "DB Profile not found.");
            card = loaded.Data.Card;
        }
        else
        {
            var pending = DecodePending(invoice.AdminComment);
            if (pending == null)
                return ApiResult<DbProfilePaidDto>.Fail("Business name was not found for this payment.");
            var created = await CreateProfileAsync(user, pending.Value.CompName, pending.Value.DisplayName, ct);
            if (!created.Success || created.Data is null)
                return ApiResult<DbProfilePaidDto>.Fail(created.Message ?? "Unable to save the DB Profile.");
            card = created.Data;
            invoice.CardId = card.Id;
            invoice.AdminComment = null;
        }

        var now = AppClock.Now;
        invoice.RazorpayPaymentId = request.RazorpayPaymentId.Trim();
        invoice.RazorpaySignature = request.RazorpaySignature.Trim();
        await ApplyPaidEffectsAsync(invoice, card, now, ct);

        await _db.SaveChangesAsync(ct);
        return ApiResult<DbProfilePaidDto>.Ok(new DbProfilePaidDto(card.Id), "Payment received.");
    }

    /// <summary>
    /// Records a successful DB Profile payment: invoice, card status, validity, and promo usage.
    /// Used by live gateway verification and by the temporary no-gateway checkout.
    /// </summary>
    private async Task ApplyPaidEffectsAsync(Invoice invoice, Card card, DateTime now, CancellationToken ct)
    {
        invoice.PaymentStatus = "Success";
        invoice.PaymentDate = now;
        invoice.UpdatedAt = now;

        card.CardStatus = "Active";

        var plan = await _db.PricingPlans.AsNoTracking()
            .FirstOrDefaultAsync(p => p.PlanName == invoice.PlanName && p.PlanCategory == PlanCommissionValues.CategoryDbProfile, ct);
        card.ValidityDate = plan?.ValidityMonths is int months && months > 0
            ? now.AddMonths(months)
            : new DateTime(2099, 12, 31);

        if (!string.IsNullOrWhiteSpace(invoice.PromoCode))
        {
            var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.CouponCode == invoice.PromoCode, ct);
            if (coupon != null) coupon.CurrentUsage += 1;
        }

        if (invoice.UserId is int payerId)
        {
            var payer = await _db.Users.FirstOrDefaultAsync(u => u.Id == payerId, ct);
            if (payer != null) OnboardingAccess.MarkDbProfilePaid(payer, now);
        }
    }

    private async Task<ApiResult<Priced>> PriceAsync(
        string email, int cardId, int planId, string? promoCode, string? state, CancellationToken ct)
    {
        if (cardId <= 0)
        {
            var payer = await FindUserAsync(email, ct);
            if (payer == null) return ApiResult<Priced>.Fail("User not found.");

            var openPlan = await _db.PricingPlans.AsNoTracking().FirstOrDefaultAsync(p =>
                p.Id == planId
                && p.PlanCategory == PlanCommissionValues.CategoryDbProfile
                && p.Status == PlanCommissionValues.StatusActive, ct);
            if (openPlan == null) return ApiResult<Priced>.Fail("Select a DB Profile plan.");

            var openPromo = await PromoDiscountAsync(payer, openPlan.OfferedPrice, promoCode, ct);
            if (!openPromo.Success) return ApiResult<Priced>.Fail(openPromo.Message ?? "Unable to apply this promo code.");

            return ApiResult<Priced>.Ok(new Priced(
                payer, null, openPlan, BuildQuote(openPlan.OriginalPrice, openPlan.OfferedPrice, openPromo.Data, state), openPromo.Data));
        }

        var loaded = await LoadOwnedCardAsync(email, cardId, ct);
        if (!loaded.Success) return ApiResult<Priced>.Fail(loaded.Message ?? "DB Profile not found.");
        if (await IsCardPaidAsync(loaded.Data.Card.Id, ct))
            return ApiResult<Priced>.Fail("This DB Profile is already paid.");

        var plan = await _db.PricingPlans.AsNoTracking().FirstOrDefaultAsync(p =>
            p.Id == planId
            && p.PlanCategory == PlanCommissionValues.CategoryDbProfile
            && p.Status == PlanCommissionValues.StatusActive, ct);
        if (plan == null) return ApiResult<Priced>.Fail("Select a DB Profile plan.");

        var promo = await PromoDiscountAsync(loaded.Data.User, plan.OfferedPrice, promoCode, ct);
        if (!promo.Success) return ApiResult<Priced>.Fail(promo.Message ?? "Unable to apply this promo code.");

        var quote = BuildQuote(plan.OriginalPrice, plan.OfferedPrice, promo.Data, state);
        return ApiResult<Priced>.Ok(new Priced(loaded.Data.User, loaded.Data.Card, plan, quote, promo.Data));
    }

    private async Task<ApiResult<(User User, Card Card)>> LoadOwnedCardAsync(string email, int cardId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email) || cardId <= 0)
            return ApiResult<(User, Card)>.Fail("DB Profile not found.");

        var key = email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == key, ct);
        if (user == null) return ApiResult<(User, Card)>.Fail("User not found.");

        var card = await _db.Cards.FirstOrDefaultAsync(c => c.Id == cardId, ct);
        if (card == null || !Owns(user, card))
            return ApiResult<(User, Card)>.Fail("DB Profile not found.");

        return ApiResult<(User, Card)>.Ok((user, card));
    }

    private async Task<IReadOnlyList<DbProfilePlanDto>> LoadPlansAsync(CancellationToken ct)
    {
        var rows = await _db.PricingPlans.AsNoTracking()
            .Where(p => p.PlanCategory == PlanCommissionValues.CategoryDbProfile && p.Status == PlanCommissionValues.StatusActive)
            .OrderBy(p => p.ValidityMonths ?? int.MaxValue)
            .ThenBy(p => p.Id)
            .ToListAsync(ct);

        return rows.Select(ToPlan).ToList();
    }

    private static DbProfilePlanDto ToPlan(PricingPlan plan)
    {
        var originalGst = Inclusive(plan.OriginalPrice, plan.OriginalPriceGst);
        var offeredGst = Inclusive(plan.OfferedPrice, plan.OfferedPriceGst);
        var save = originalGst > offeredGst ? originalGst - offeredGst : 0;
        decimal? perMonth = plan.ValidityMonths is int months and > 0
            ? Math.Round(offeredGst / months, 0, MidpointRounding.AwayFromZero)
            : null;

        string? badge = null;
        string? lockLabel = null;
        if (plan.ValidityMonths is int m && m % 12 == 0)
        {
            var years = m / 12;
            if (years == 2) badge = "BEST VALUE";
            if (years >= 3)
            {
                badge = "MAXIMUM SAVINGS";
                lockLabel = $"LOCK YOUR PRICE FOR {years} YEARS";
            }
        }

        return new DbProfilePlanDto(
            plan.Id,
            plan.PlanName,
            plan.ValidityMonths,
            PricingLabel(plan.ValidityMonths),
            Round2(plan.OriginalPrice),
            Round2(plan.OfferedPrice),
            originalGst,
            offeredGst,
            save,
            perMonth,
            badge,
            lockLabel);
    }

    private async Task<ApiResult<decimal>> PromoDiscountAsync(User user, decimal offeredPrice, string? promoCode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(promoCode)) return ApiResult<decimal>.Ok(0);

        var code = promoCode.Trim();
        var coupon = await _db.Coupons
            .Include(c => c.CustomerMappings)
            .FirstOrDefaultAsync(c => c.CouponCode.ToLower() == code.ToLower(), ct);
        if (coupon == null) return ApiResult<decimal>.Fail("Promo code is not valid.");

        if (CouponValues.DisplayStatus(coupon.Status, coupon.ValidityDate) != CouponValues.StatusActive)
            return ApiResult<decimal>.Fail("This promo code is not active.");

        if (coupon.RoleId is int roleId && roleId != user.RoleId)
            return ApiResult<decimal>.Fail("This promo code does not apply to your account.");

        if (coupon.CustomerMappings.Count > 0
            && !coupon.CustomerMappings.Any(m =>
                m.UserId == user.Id
                || string.Equals(m.CustomerEmail, user.Email, StringComparison.OrdinalIgnoreCase)))
            return ApiResult<decimal>.Fail("This promo code is not available for your account.");

        if (coupon.UsageLimitPerCoupon > 0 && coupon.CurrentUsage >= coupon.UsageLimitPerCoupon)
            return ApiResult<decimal>.Fail("This promo code has reached its usage limit.");

        if (coupon.UsageLimitPerUser > 0)
        {
            var used = await _db.Invoices.CountAsync(i =>
                i.UserId == user.Id
                && i.PromoCode == coupon.CouponCode
                && (i.PaymentStatus == "Success" || i.PaymentStatus == "Paid"), ct);
            if (used >= coupon.UsageLimitPerUser)
                return ApiResult<decimal>.Fail("You have already used this promo code.");
        }

        var discount = coupon.PromoDiscountType.Equals(CouponValues.DiscountPercentage, StringComparison.OrdinalIgnoreCase)
            ? Round2(offeredPrice * coupon.PromoDiscountValue / 100m)
            : Round2(coupon.PromoDiscountValue);
        if (discount < 0) discount = 0;
        if (discount > offeredPrice) discount = offeredPrice;
        return ApiResult<decimal>.Ok(discount);
    }

    private static DbProfileQuoteDto BuildQuote(decimal originalPrice, decimal offeredPrice, decimal promoDiscount, string? state)
    {
        var original = Round2(originalPrice);
        var offered = Round2(offeredPrice);
        if (offered > original) original = offered;
        var promo = Round2(promoDiscount);
        if (promo < 0) promo = 0;
        if (promo > offered) promo = offered;

        var planDiscount = Round2(original - offered);
        var subTotal = Round2(offered - promo);
        var intra = IsIntraState(state);
        var cgst = intra ? Round2(subTotal * CgstRate) : 0;
        var sgst = intra ? Round2(subTotal * SgstRate) : 0;
        var igst = intra ? 0 : Round2(subTotal * IgstRate);
        return new DbProfileQuoteDto(
            original,
            Round2(planDiscount + promo),
            promo,
            subTotal,
            cgst,
            sgst,
            igst,
            Round2(subTotal + cgst + sgst + igst),
            intra);
    }

    private static string? ValidateForm(DbProfileCheckoutRequest request)
    {
        if (!request.Agreed) return "Please agree to the Terms and Privacy Policy.";
        if (string.IsNullOrWhiteSpace(request.Name)) return "Enter your name.";
        if (string.IsNullOrWhiteSpace(request.BillingEmail) || !request.BillingEmail.Contains('@'))
            return "Enter a valid email.";
        if (Digits(request.Phone).Length < 10) return "Enter a valid phone number.";
        if (string.IsNullOrWhiteSpace(request.Address)) return "Enter your address.";
        if (string.IsNullOrWhiteSpace(request.State)) return "Enter your state.";
        if (string.IsNullOrWhiteSpace(request.City)) return "Enter your city.";
        if (Digits(request.Pincode).Length != 6) return "Enter a 6-digit pin code.";
        var gst = NormalizeGst(request.GstNumber);
        if (gst.Length > 0 && !GstinPattern.IsMatch(gst)) return "Enter a valid GST number or leave it blank.";
        return null;
    }

    private async Task<User?> FindUserAsync(string email, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        var key = email.Trim().ToLowerInvariant();
        return await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == key, ct);
    }

    /// <summary>
    /// A free name is held until payment. A name already saved on this user's unpaid profile is reused.
    /// </summary>
    private async Task<ApiResult<Card?>> HoldNameAsync(User user, string compName, CancellationToken ct)
    {
        var check = await _website.CheckNameAsync(compName, null, ct);
        if (check.Data is { Available: true })
            return ApiResult<Card?>.Ok(null);

        var slug = check.Data?.Slug;
        if (!string.IsNullOrWhiteSpace(slug))
        {
            var card = await _db.Cards.FirstOrDefaultAsync(c => c.UserId == user.Id && c.Slug == slug, ct);
            if (card != null && !await IsCardPaidAsync(card.Id, ct))
                return ApiResult<Card?>.Ok(card);
        }

        return ApiResult<Card?>.Fail(check.Data?.Message ?? "Business URL is not available.");
    }

    private async Task<ApiResult<Card>> CreateProfileAsync(User user, string compName, string? displayName, CancellationToken ct)
    {
        var held = await HoldNameAsync(user, compName, ct);
        if (!held.Success)
            return ApiResult<Card>.Fail(held.Message ?? "Business URL is not available.");
        if (held.Data != null)
            return ApiResult<Card>.Ok(held.Data);

        var created = await _website.CreateBusinessNameAsync(
            new UpsertBusinessNameRequest(compName, displayName, user.Email), ct);
        if (!created.Success || created.Data is null)
            return ApiResult<Card>.Fail(created.Message ?? "Unable to save the DB Profile.");

        var card = await _db.Cards.FirstOrDefaultAsync(c => c.Id == created.Data.Id, ct);
        return card == null
            ? ApiResult<Card>.Fail("Unable to save the DB Profile.")
            : ApiResult<Card>.Ok(card);
    }

    private static string EncodePending(string compName, string displayName)
    {
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(compName.Trim() + "\n" + displayName.Trim()));
        return PendingPrefix + payload;
    }

    private static (string CompName, string DisplayName)? DecodePending(string? comment)
    {
        if (string.IsNullOrWhiteSpace(comment) || !comment.StartsWith(PendingPrefix, StringComparison.Ordinal))
            return null;
        try
        {
            var text = Encoding.UTF8.GetString(Convert.FromBase64String(comment[PendingPrefix.Length..]));
            var split = text.Split('\n', 2);
            if (split.Length < 2 || string.IsNullOrWhiteSpace(split[0]) || string.IsNullOrWhiteSpace(split[1]))
                return null;
            return (split[0], split[1]);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static DbProfileBillingDto BillingFromUser(User user) =>
        new(
            user.FullName,
            user.Email,
            user.Phone ?? "",
            user.Address.AddressLine ?? "",
            user.Address.State ?? "",
            user.Address.City ?? "",
            user.Address.Pincode ?? "",
            user.Address.Gst ?? "");

    private static DbProfileBillingDto BillingFrom(User user, Card card)
    {
        static string First(params string?[] values) =>
            values.Select(v => v?.Trim()).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? "";

        return new DbProfileBillingDto(
            First(user.FullName, $"{card.FirstName} {card.LastName}"),
            First(user.Email, card.Email),
            First(card.Contact, user.Phone),
            user.Address.AddressLine ?? "",
            user.Address.State ?? "",
            user.Address.City ?? "",
            user.Address.Pincode ?? "",
            user.Address.Gst ?? "");
    }

    private static bool Owns(User user, Card card) =>
        card.UserId == user.Id
        || card.FranchiseeUserId == user.Id
        || string.Equals(card.UserEmail, user.Email, StringComparison.OrdinalIgnoreCase)
        || string.Equals(card.FranchiseeEmail, user.Email, StringComparison.OrdinalIgnoreCase);

    private Task<bool> IsCardPaidAsync(int cardId, CancellationToken ct) =>
        _db.Invoices.AnyAsync(i =>
            i.CardId == cardId
            && (i.PaymentStatus == "Success" || i.PaymentStatus == "Paid"), ct);

    private static bool IsInvoicePaid(string? status) =>
        string.Equals(status, "Success", StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, "Paid", StringComparison.OrdinalIgnoreCase);

    private static bool IsIntraState(string? state) =>
        string.Equals(state?.Trim(), SellerState, StringComparison.OrdinalIgnoreCase);

    private static decimal Inclusive(decimal exclusive, decimal storedInclusive) =>
        storedInclusive > 0 ? storedInclusive : PlanCommissionValues.WithGst(exclusive);

    private static decimal Round2(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static string Digits(string? value) =>
        new string((value ?? "").Where(char.IsDigit).ToArray());

    private static string NormalizeGst(string? value) =>
        (value ?? "").Trim().ToUpperInvariant();

    private static string PricingLabel(int? months) => months switch
    {
        null => "Lifetime",
        var m when m % 12 == 0 => m / 12 == 1 ? "1 Year" : $"{m / 12} Years",
        1 => "1 Month",
        var m => $"{m} Months",
    };

    private async Task<string> NextInvoiceNumberAsync(CancellationToken ct)
    {
        string number;
        do number = UserCodeGenerator.Generate("INV");
        while (await _db.Invoices.AnyAsync(i => i.InvoiceNumber == number, ct));
        return number;
    }

    private sealed record Priced(User? User, Card? Card, PricingPlan? Plan, DbProfileQuoteDto? Quote, decimal PromoDiscount);
}
