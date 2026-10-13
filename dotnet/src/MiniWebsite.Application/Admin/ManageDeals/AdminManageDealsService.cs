using System.Globalization;
using Microsoft.EntityFrameworkCore;
using MiniWebsite.Application.Admin.ManageDeals.Dtos;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Domain.Entities;
using MiniWebsite.Domain.Common;

namespace MiniWebsite.Application.Admin.ManageDeals;

public class AdminManageDealsService : IAdminManageDealsService
{
    private static readonly string[] IndiaStates =
    [
        "Andhra Pradesh", "Arunachal Pradesh", "Assam", "Bihar", "Chhattisgarh",
        "Goa", "Gujarat", "Haryana", "Himachal Pradesh", "Jharkhand", "Karnataka",
        "Kerala", "Madhya Pradesh", "Maharashtra", "Manipur", "Meghalaya", "Mizoram",
        "Nagaland", "Odisha", "Punjab", "Rajasthan", "Sikkim", "Tamil Nadu",
        "Telangana", "Tripura", "Uttar Pradesh", "Uttarakhand", "West Bengal",
        "Andaman and Nicobar Islands", "Chandigarh",
        "Dadra and Nagar Haveli and Daman and Diu", "Delhi", "Jammu and Kashmir",
        "Ladakh", "Lakshadweep", "Puducherry",
    ];

    private readonly IApplicationDbContext _db;

    public AdminManageDealsService(IApplicationDbContext db)
    {
        _db = db;
    }

    public ManageDealsMetaDto GetMeta() => new(IndiaStates);

    public async Task<ApiResult<ManageDealsPageDto>> ListAsync(ManageDealsQuery query, CancellationToken ct = default)
    {
        query.Page = query.Page < 1 ? 1 : query.Page;
        query.PageSize = query.PageSize is < 1 or > 100 ? 10 : query.PageSize;

        try
        {
            var q = ApplyFilters(_db.Deals.AsNoTracking(), query);
            var total = await q.CountAsync(ct);
            var rows = await q
                .OrderByDescending(d => d.Id)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(ct);

            return ApiResult<ManageDealsPageDto>.Ok(new ManageDealsPageDto(
                rows.Select(MapEntity).ToList(),
                total,
                query.Page,
                query.PageSize));
        }
        catch (Exception ex)
        {
            return ApiResult<ManageDealsPageDto>.Fail("Unable to load deals: " + ex.Message);
        }
    }

    public async Task<ApiResult<ManageDealRowDto>> GetAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var row = await _db.Deals.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct);
            if (row == null) return ApiResult<ManageDealRowDto>.Fail("Deal not found");
            return ApiResult<ManageDealRowDto>.Ok(MapEntity(row));
        }
        catch (Exception ex)
        {
            return ApiResult<ManageDealRowDto>.Fail("Unable to load deal: " + ex.Message);
        }
    }

    public async Task<ApiResult<ManageDealRowDto>> CreateAsync(UpsertDealRequest request, CancellationToken ct = default)
    {
        var validation = Validate(request);
        if (validation != null) return ApiResult<ManageDealRowDto>.Fail(validation);

        var coupon = NormalizeCoupon(request.CouponCode);

        try
        {
            if (await _db.Deals.AnyAsync(d => d.CouponCode == coupon, ct))
                return ApiResult<ManageDealRowDto>.Fail("Coupon code already exists!");

            var entity = new Deal
            {
                PlanName = request.PlanName.Trim(),
                PlanType = request.PlanType.Trim(),
                DealName = request.DealName.Trim(),
                CouponCode = coupon,
                CouponType = NormalizeCouponType(request.CouponType),
                BonusAmount = Math.Max(0, request.BonusAmount),
                DiscountAmount = Math.Max(0, request.DiscountAmount),
                DiscountPercentage = ClampPercent(request.DiscountPercentage),
                RenewalBonusAmount = Math.Max(0, request.RenewalCommission),
                ValidityDate = request.ValidityDate.Date,
                MaxUsage = Math.Max(0, request.MaxUsage),
                CurrentUsage = 0,
                DealState = string.IsNullOrWhiteSpace(request.DealState) ? "" : request.DealState.Trim(),
                CreatedBy = string.IsNullOrWhiteSpace(request.CreatedBy) ? "admin" : request.CreatedBy.Trim(),
                DealStatus = "Active",
                CreatedAt = AppClock.Now,
            };

            _db.Deals.Add(entity);
            await _db.SaveChangesAsync(ct);

            return ApiResult<ManageDealRowDto>.Ok(MapEntity(entity), "Deal Added Successfully!");
        }
        catch (Exception ex)
        {
            return ApiResult<ManageDealRowDto>.Fail("Error adding deal: " + ex.Message);
        }
    }

    public async Task<ApiResult<ManageDealRowDto>> UpdateAsync(int id, UpsertDealRequest request, CancellationToken ct = default)
    {
        var validation = Validate(request);
        if (validation != null) return ApiResult<ManageDealRowDto>.Fail(validation);

        var coupon = NormalizeCoupon(request.CouponCode);

        try
        {
            var entity = await _db.Deals.FirstOrDefaultAsync(d => d.Id == id, ct);
            if (entity == null) return ApiResult<ManageDealRowDto>.Fail("Deal not found");

            if (await _db.Deals.AnyAsync(d => d.CouponCode == coupon && d.Id != id, ct))
                return ApiResult<ManageDealRowDto>.Fail("Coupon code already exists!");

            entity.PlanName = request.PlanName.Trim();
            entity.PlanType = request.PlanType.Trim();
            entity.DealName = request.DealName.Trim();
            entity.CouponCode = coupon;
            entity.CouponType = NormalizeCouponType(request.CouponType);
            entity.BonusAmount = Math.Max(0, request.BonusAmount);
            entity.DiscountAmount = Math.Max(0, request.DiscountAmount);
            entity.DiscountPercentage = ClampPercent(request.DiscountPercentage);
            entity.RenewalBonusAmount = Math.Max(0, request.RenewalCommission);
            entity.ValidityDate = request.ValidityDate.Date;
            entity.MaxUsage = Math.Max(0, request.MaxUsage);
            entity.DealState = string.IsNullOrWhiteSpace(request.DealState) ? "" : request.DealState.Trim();

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                entity.DealStatus = string.Equals(request.Status, "Inactive", StringComparison.OrdinalIgnoreCase)
                    ? "Inactive"
                    : "Active";
            }

            await _db.SaveChangesAsync(ct);
            return ApiResult<ManageDealRowDto>.Ok(MapEntity(entity), "Deal Updated Successfully!");
        }
        catch (Exception ex)
        {
            return ApiResult<ManageDealRowDto>.Fail("Error updating deal: " + ex.Message);
        }
    }

    public async Task<ApiResult> ToggleStatusAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var entity = await _db.Deals.FirstOrDefaultAsync(d => d.Id == id, ct);
            if (entity == null) return ApiResult.Fail("Deal not found");

            entity.DealStatus = string.Equals(entity.DealStatus, "Active", StringComparison.OrdinalIgnoreCase)
                ? "Inactive"
                : "Active";
            await _db.SaveChangesAsync(ct);
            return ApiResult.Ok("Status updated!");
        }
        catch (Exception ex)
        {
            return ApiResult.Fail("Unable to update status: " + ex.Message);
        }
    }

    public async Task<ApiResult> DeleteAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var entity = await _db.Deals.FirstOrDefaultAsync(d => d.Id == id, ct);
            if (entity == null) return ApiResult.Fail("Deal not found");

            _db.Deals.Remove(entity);
            await _db.SaveChangesAsync(ct);
            return ApiResult.Ok("Deal deleted!");
        }
        catch (Exception ex)
        {
            return ApiResult.Fail("Unable to delete deal: " + ex.Message);
        }
    }

    private static IQueryable<Deal> ApplyFilters(IQueryable<Deal> q, ManageDealsQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLower();
            q = q.Where(d =>
                d.DealName.ToLower().Contains(s)
                || d.CouponCode.ToLower().Contains(s)
                || d.PlanName.ToLower().Contains(s)
                || (d.DealState != null && d.DealState.ToLower().Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(query.PlanType)
            && !query.PlanType.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            var planType = query.PlanType.Trim();
            q = q.Where(d => d.PlanType == planType);
        }

        if (!string.IsNullOrWhiteSpace(query.Status)
            && !query.Status.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            var status = query.Status.Trim();
            q = q.Where(d => d.DealStatus == status);
        }

        return q;
    }

    private static string? Validate(UpsertDealRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PlanName)) return "Plan name is required";
        if (string.IsNullOrWhiteSpace(request.PlanType)) return "Plan type is required";
        if (string.IsNullOrWhiteSpace(request.DealName)) return "Deal name is required";
        if (string.IsNullOrWhiteSpace(request.CouponCode)) return "Coupon code is required";
        if (request.ValidityDate == default) return "Validity date is required";

        var plan = request.PlanName.Trim();
        if (plan is not ("Basic" or "Premium" or "Enterprise"))
            return "Invalid plan name";

        var type = request.PlanType.Trim();
        if (type is not ("MiniWebsite" or "Franchise"))
            return "Invalid plan type";

        var couponType = NormalizeCouponType(request.CouponType);
        if (couponType is not ("Default" or "Festive Discount" or "Promotional Discount"))
            return "Invalid coupon type";

        return null;
    }

    private static string NormalizeCoupon(string code) => code.Trim().ToUpperInvariant();

    private static string NormalizeCouponType(string? couponType)
    {
        var raw = (couponType ?? "").Trim();
        if (raw.Equals("Festive Discount", StringComparison.OrdinalIgnoreCase) ||
            raw.Equals("Festive", StringComparison.OrdinalIgnoreCase))
            return "Festive Discount";
        if (raw.Equals("Promotional Discount", StringComparison.OrdinalIgnoreCase) ||
            raw.Equals("Promotional", StringComparison.OrdinalIgnoreCase) ||
            raw.Equals("Promo", StringComparison.OrdinalIgnoreCase))
            return "Promotional Discount";
        return "Default";
    }

    private static decimal ClampPercent(decimal value) =>
        value < 0 ? 0 : value > 100 ? 100 : value;

    private static ManageDealRowDto MapEntity(Deal d)
    {
        var culture = CultureInfo.InvariantCulture;
        var bonus = d.BonusAmount;
        var discountAmt = d.DiscountAmount;
        var discountPct = d.DiscountPercentage;
        var renewalBonus = d.RenewalBonusAmount;
        var maxUsage = d.MaxUsage;
        var currentUsage = d.CurrentUsage;
        var validity = d.ValidityDate?.Date ?? AppClock.Today;
        var uploaded = d.CreatedAt;
        var expired = validity.Date < AppClock.Today;
        var hasState = !string.IsNullOrWhiteSpace(d.DealState);

        string discountDisplay;
        if (discountAmt > 0)
            discountDisplay = "₹" + discountAmt.ToString("N0", culture);
        else if (discountPct > 0)
            discountDisplay = discountPct.ToString("0.##", culture) + "%";
        else
            discountDisplay = "-";

        var couponTypeDisplay = NormalizeCouponType(d.CouponType);

        var renewalCommissionDisplay = renewalBonus > 0
            ? "₹" + renewalBonus.ToString("N0", culture)
            : "-";

        var usage = maxUsage == 0
            ? currentUsage + "/∞"
            : currentUsage + "/" + maxUsage;

        var statusTone = string.Equals(d.DealStatus, "Active", StringComparison.OrdinalIgnoreCase)
            ? "ok"
            : "neutral";

        return new ManageDealRowDto(
            d.Id,
            d.PlanName ?? "",
            d.PlanType ?? "",
            d.DealState,
            hasState ? d.DealState! : "All",
            d.DealName ?? "",
            d.CouponCode ?? "",
            couponTypeDisplay,
            uploaded,
            uploaded?.ToString("dd-MM-yyyy", culture) ?? "-",
            bonus,
            "₹" + bonus.ToString("N0", culture),
            discountAmt,
            discountPct,
            discountDisplay,
            renewalBonus,
            renewalCommissionDisplay,
            validity,
            validity.ToString("dd-MM-yyyy", culture),
            expired,
            maxUsage,
            currentUsage,
            usage,
            d.DealStatus ?? "",
            statusTone);
    }
}
