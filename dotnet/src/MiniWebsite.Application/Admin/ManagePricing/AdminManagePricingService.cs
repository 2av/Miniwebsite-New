using System.Globalization;
using Microsoft.EntityFrameworkCore;
using MiniWebsite.Application.Admin.ManagePricing.Dtos;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Domain.Entities;

namespace MiniWebsite.Application.Admin.ManagePricing;

public class AdminManagePricingService : IAdminManagePricingService
{
    private const decimal GstRate = 1.18m;

    private static readonly IReadOnlyDictionary<string, string[]> PlanCatalog =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["MW Plan (For MW Users)"] =
            [
                "MiniWebsite 1 Year Plan",
                "MiniWebsite 2 Year Plan",
                "MiniWebsite 3 Year Plan",
            ],
            ["MW Plan (For Franchise Partner)"] =
            [
                "MiniWebsite 1 Year Plan",
                "MiniWebsite 2 Year Plan",
                "MiniWebsite 3 Year Plan",
            ],
            ["Franchise Fees"] =
            [
                "MW Full Franchise (Franchise Fees)",
            ],
            ["Digital Marketer Partnership Fees"] =
            [
                "MW Digital Marketing (Annual Membership Fees)",
            ],
        };

    private readonly IApplicationDbContext _db;

    public AdminManagePricingService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ApiResult<ManagePricingMetaDto>> GetMetaAsync(CancellationToken ct = default)
    {
        try
        {
            var deals = await _db.Deals.AsNoTracking()
                .Where(d => d.DealStatus == "Active")
                .OrderBy(d => d.DealName)
                .ThenByDescending(d => d.Id)
                .Select(d => new PricingDealOptionDto(d.Id, d.DealName ?? "", d.CouponCode))
                .ToListAsync(ct);

            var catalog = PlanCatalog
                .Select(kv => new PricingPlanTypeDto(kv.Key, kv.Value))
                .ToList();

            var existingPlans = await _db.PricingPlans.AsNoTracking()
                .OrderBy(p => p.PlanType)
                .ThenBy(p => p.PlanName)
                .ThenBy(p => p.Id)
                .Select(p => new PricingPlanKeyDto(p.Id, p.PlanType, p.PlanName))
                .ToListAsync(ct);

            return ApiResult<ManagePricingMetaDto>.Ok(new ManagePricingMetaDto(
                catalog,
                deals,
                existingPlans));
        }
        catch (Exception ex)
        {
            return ApiResult<ManagePricingMetaDto>.Fail("Unable to load pricing meta: " + ex.Message);
        }
    }

    public async Task<ApiResult<ManagePricingPageDto>> ListAsync(ManagePricingQuery query, CancellationToken ct = default)
    {
        query.Page = query.Page < 1 ? 1 : query.Page;
        query.PageSize = query.PageSize is < 1 or > 100 ? 10 : query.PageSize;

        try
        {
            var q = ApplyFilters(
                _db.PricingPlans.AsNoTracking().Include(p => p.Deal),
                query);

            var total = await q.CountAsync(ct);
            var rows = await q
                .OrderByDescending(p => p.Id)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(ct);

            return ApiResult<ManagePricingPageDto>.Ok(new ManagePricingPageDto(
                rows.Select(MapEntity).ToList(),
                total,
                query.Page,
                query.PageSize));
        }
        catch (Exception ex)
        {
            return ApiResult<ManagePricingPageDto>.Fail("Unable to load pricing: " + ex.Message);
        }
    }

    public async Task<ApiResult<ManagePricingRowDto>> GetAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var row = await _db.PricingPlans.AsNoTracking()
                .Include(p => p.Deal)
                .FirstOrDefaultAsync(p => p.Id == id, ct);
            if (row == null) return ApiResult<ManagePricingRowDto>.Fail("Pricing plan not found");
            return ApiResult<ManagePricingRowDto>.Ok(MapEntity(row));
        }
        catch (Exception ex)
        {
            return ApiResult<ManagePricingRowDto>.Fail("Unable to load pricing plan: " + ex.Message);
        }
    }

    public async Task<ApiResult<ManagePricingRowDto>> CreateAsync(UpsertPricingRequest request, CancellationToken ct = default)
    {
        var validation = Validate(request);
        if (validation != null) return ApiResult<ManagePricingRowDto>.Fail(validation);

        try
        {
            var planType = request.PlanType.Trim();
            var planName = request.PlanName.Trim();
            var original = RoundMoney(request.OriginalPrice);
            var offered = RoundMoney(request.OfferedPrice);
            var originalGst = WithGst(original);
            var offeredGst = WithGst(offered);
            var dealId = await NormalizeDealIdAsync(request.DealId, ct);
            if (dealId < 0) return ApiResult<ManagePricingRowDto>.Fail("Selected deal / coupon was not found.");

            var dup = await _db.PricingPlans.AnyAsync(
                p => p.PlanType == planType && p.PlanName == planName, ct);
            if (dup)
                return ApiResult<ManagePricingRowDto>.Fail("This plan type and plan name combination already exists.");

            var now = DateTime.UtcNow;
            var entity = new PricingPlan
            {
                PlanType = planType,
                PlanName = planName,
                OriginalPrice = original,
                OriginalPriceGst = originalGst,
                OfferedPrice = offered,
                OfferedPriceGst = offeredGst,
                DealId = dealId > 0 ? dealId : null,
                Status = "Active",
                CreatedAt = now,
                UpdatedAt = now,
            };
            _db.PricingPlans.Add(entity);
            await _db.SaveChangesAsync(ct);

            return await GetAsync(entity.Id, ct) switch
            {
                { Success: true, Data: { } data } => ApiResult<ManagePricingRowDto>.Ok(data, "Pricing plan added successfully!"),
                var r => r
            };
        }
        catch (Exception ex)
        {
            return ApiResult<ManagePricingRowDto>.Fail("Error adding pricing plan: " + ex.Message);
        }
    }

    public async Task<ApiResult<ManagePricingRowDto>> UpdateAsync(int id, UpsertPricingRequest request, CancellationToken ct = default)
    {
        var validation = Validate(request);
        if (validation != null) return ApiResult<ManagePricingRowDto>.Fail(validation);

        try
        {
            var entity = await _db.PricingPlans.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (entity == null) return ApiResult<ManagePricingRowDto>.Fail("Pricing plan not found");

            var planType = request.PlanType.Trim();
            var planName = request.PlanName.Trim();
            var original = RoundMoney(request.OriginalPrice);
            var offered = RoundMoney(request.OfferedPrice);
            var originalGst = WithGst(original);
            var offeredGst = WithGst(offered);
            var dealId = await NormalizeDealIdAsync(request.DealId, ct);
            if (dealId < 0) return ApiResult<ManagePricingRowDto>.Fail("Selected deal / coupon was not found.");

            var dup = await _db.PricingPlans.AnyAsync(
                p => p.PlanType == planType && p.PlanName == planName && p.Id != id, ct);
            if (dup)
                return ApiResult<ManagePricingRowDto>.Fail("This plan type and plan name combination already exists.");

            entity.PlanType = planType;
            entity.PlanName = planName;
            entity.OriginalPrice = original;
            entity.OriginalPriceGst = originalGst;
            entity.OfferedPrice = offered;
            entity.OfferedPriceGst = offeredGst;
            entity.DealId = dealId > 0 ? dealId : null;
            if (!string.IsNullOrWhiteSpace(request.Status))
                entity.Status = NormalizeStatus(request.Status);
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return await GetAsync(id, ct) switch
            {
                { Success: true, Data: { } data } => ApiResult<ManagePricingRowDto>.Ok(data, "Pricing plan updated successfully!"),
                var r => r
            };
        }
        catch (Exception ex)
        {
            return ApiResult<ManagePricingRowDto>.Fail("Error updating pricing plan: " + ex.Message);
        }
    }

    public async Task<ApiResult> ToggleStatusAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var entity = await _db.PricingPlans.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (entity == null) return ApiResult.Fail("Pricing plan not found");

            entity.Status = string.Equals(entity.Status, "Active", StringComparison.OrdinalIgnoreCase)
                ? "Inactive"
                : "Active";
            entity.UpdatedAt = DateTime.UtcNow;
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
            var entity = await _db.PricingPlans.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (entity == null) return ApiResult.Fail("Pricing plan not found");

            _db.PricingPlans.Remove(entity);
            await _db.SaveChangesAsync(ct);
            return ApiResult.Ok("Pricing plan deleted!");
        }
        catch (Exception ex)
        {
            return ApiResult.Fail("Unable to delete pricing plan: " + ex.Message);
        }
    }

    private static IQueryable<PricingPlan> ApplyFilters(IQueryable<PricingPlan> q, ManagePricingQuery query)
    {
        var planType = string.IsNullOrWhiteSpace(query.PlanType)
                       || query.PlanType.Equals("all", StringComparison.OrdinalIgnoreCase)
            ? null
            : query.PlanType.Trim();
        var status = string.IsNullOrWhiteSpace(query.Status)
                     || query.Status.Equals("all", StringComparison.OrdinalIgnoreCase)
            ? null
            : query.Status.Trim();

        if (planType != null)
            q = q.Where(p => p.PlanType == planType);
        if (status != null)
            q = q.Where(p => p.Status == status);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLower();
            q = q.Where(p =>
                p.PlanType.ToLower().Contains(s)
                || p.PlanName.ToLower().Contains(s)
                || (p.Deal != null && p.Deal.DealName.ToLower().Contains(s))
                || (p.Deal != null && p.Deal.CouponCode != null && p.Deal.CouponCode.ToLower().Contains(s)));
        }

        return q;
    }

    private static string? Validate(UpsertPricingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PlanType)) return "Plan type is required";
        if (string.IsNullOrWhiteSpace(request.PlanName)) return "Plan name is required";
        if (request.OriginalPrice < 0) return "Original price cannot be negative";
        if (request.OfferedPrice < 0) return "Offered price cannot be negative";

        var planType = request.PlanType.Trim();
        if (!PlanCatalog.TryGetValue(planType, out var names))
            return "Invalid plan type";

        var planName = request.PlanName.Trim();
        if (!names.Any(n => string.Equals(n, planName, StringComparison.Ordinal)))
            return "Invalid plan name for the selected plan type";

        return null;
    }

    /// <returns>0 = none, positive = valid id, -1 = invalid</returns>
    private async Task<int> NormalizeDealIdAsync(int? dealId, CancellationToken ct)
    {
        if (dealId is null or <= 0) return 0;
        var exists = await _db.Deals.AnyAsync(d => d.Id == dealId.Value, ct);
        return exists ? dealId.Value : -1;
    }

    private static ManagePricingRowDto MapEntity(PricingPlan p)
    {
        var original = p.OriginalPrice;
        var originalGst = p.OriginalPriceGst;
        var offered = p.OfferedPrice;
        var offeredGst = p.OfferedPriceGst;
        var status = string.IsNullOrWhiteSpace(p.Status) ? "Active" : p.Status.Trim();
        var tone = string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase) ? "success" : "muted";
        var dealName = p.Deal?.DealName;
        var couponCode = p.Deal?.CouponCode;
        var dealDisplay = !string.IsNullOrWhiteSpace(couponCode)
            ? $"{dealName} ({couponCode})"
            : (dealName ?? "—");

        return new ManagePricingRowDto(
            p.Id,
            p.PlanType ?? "",
            p.PlanName ?? "",
            original,
            FormatMoneyDisplay(original),
            originalGst,
            FormatMoneyDisplay(originalGst),
            offered,
            FormatMoneyDisplay(offered),
            offeredGst,
            FormatMoneyDisplay(offeredGst),
            p.DealId is > 0 ? p.DealId : null,
            dealName,
            couponCode,
            string.IsNullOrWhiteSpace(dealName) && string.IsNullOrWhiteSpace(couponCode) ? "—" : dealDisplay,
            status,
            tone,
            FormatDateTime(p.CreatedAt));
    }

    private static decimal WithGst(decimal withoutGst) =>
        Math.Round(withoutGst * GstRate, 0, MidpointRounding.AwayFromZero);

    private static decimal RoundMoney(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    private static string FormatMoneyDisplay(decimal v) =>
        decimal.Truncate(v) == v
            ? "₹" + v.ToString("N0", CultureInfo.InvariantCulture)
            : "₹" + v.ToString("N2", CultureInfo.InvariantCulture);

    private static string? FormatDateTime(DateTime? dt)
    {
        if (dt is null || dt.Value.Year < 1) return null;
        return dt.Value.ToString("dd MMM yyyy, hh:mm tt", CultureInfo.InvariantCulture);
    }

    private static string NormalizeStatus(string? status)
        => string.Equals(status, "Inactive", StringComparison.OrdinalIgnoreCase) ? "Inactive" : "Active";
}
