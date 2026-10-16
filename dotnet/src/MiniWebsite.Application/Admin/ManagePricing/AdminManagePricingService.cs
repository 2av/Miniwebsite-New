using Microsoft.EntityFrameworkCore;
using MiniWebsite.Application.Admin.ManagePricing.Dtos;
using MiniWebsite.Application.Common.Extensions;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Domain.Entities;
using MiniWebsite.Domain.Common;

namespace MiniWebsite.Application.Admin.ManagePricing;

public class AdminManagePricingService : IAdminManagePricingService
{
    private const int PlanNameMaxLength = 200;
    private const int ValidityMonthsMax = 600;

    private readonly IApplicationDbContext _db;

    public AdminManagePricingService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ApiResult<ManagePricingMetaDto>> GetMetaAsync(CancellationToken ct = default)
    {
        try
        {
            var existingPlans = await _db.PricingPlans.AsNoTracking()
                .OrderBy(p => p.PlanName)
                .Select(p => new PricingPlanKeyDto(p.Id, p.PlanName))
                .ToListAsync(ct);

            return ApiResult<ManagePricingMetaDto>.Ok(new ManagePricingMetaDto(
                PlanCommissionValues.Categories,
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
            var q = ApplyFilters(_db.PricingPlans.AsNoTracking(), query);

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
            var row = await _db.PricingPlans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
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
            var planName = request.PlanName.Trim();
            if (await _db.PricingPlans.AnyAsync(p => p.PlanName == planName, ct))
                return ApiResult<ManagePricingRowDto>.Fail("A plan with this name already exists.");

            var now = AppClock.Now;
            var entity = new PricingPlan { Status = "Active", CreatedAt = now };
            Apply(entity, request);
            entity.UpdatedAt = now;
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

            var planName = request.PlanName.Trim();
            if (await _db.PricingPlans.AnyAsync(p => p.PlanName == planName && p.Id != id, ct))
                return ApiResult<ManagePricingRowDto>.Fail("A plan with this name already exists.");

            Apply(entity, request);
            if (!string.IsNullOrWhiteSpace(request.Status))
                entity.Status = NormalizeStatus(request.Status);
            entity.UpdatedAt = AppClock.Now;
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
            entity.UpdatedAt = AppClock.Now;
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

            if (await _db.PlanCommissions.AnyAsync(c => c.PricingPlanId == id, ct))
                return ApiResult.Fail("This plan has plan commissions. Delete those first.");

            _db.PricingPlans.Remove(entity);
            await _db.SaveChangesAsync(ct);
            return ApiResult.Ok("Pricing plan deleted!");
        }
        catch (Exception ex)
        {
            return ApiResult.Fail("Unable to delete pricing plan: " + ex.Message);
        }
    }

    private static void Apply(PricingPlan entity, UpsertPricingRequest request)
    {
        var original = PricingFormat.RoundMoney(request.OriginalPrice);
        var offered = PricingFormat.RoundMoney(request.OfferedPrice);

        entity.PlanName = request.PlanName.Trim();
        entity.PlanCategory = NormalizeCategory(request.PlanCategory)!;
        entity.ValidityMonths = request.ValidityMonths;
        entity.OriginalPrice = original;
        entity.OriginalPriceGst = PlanCommissionValues.WithGst(original);
        entity.OfferedPrice = offered;
        entity.OfferedPriceGst = PlanCommissionValues.WithGst(offered);
    }

    private static IQueryable<PricingPlan> ApplyFilters(IQueryable<PricingPlan> q, ManagePricingQuery query)
    {
        var status = string.IsNullOrWhiteSpace(query.Status)
                     || query.Status.Equals("all", StringComparison.OrdinalIgnoreCase)
            ? null
            : query.Status.Trim();
        var category = NormalizeCategory(query.Category);

        if (category != null)
            q = q.Where(p => p.PlanCategory == category);
        if (status != null)
            q = q.Where(p => p.Status == status);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLower();
            q = q.Where(p => p.PlanName.ToLower().Contains(s) || p.PlanCategory.ToLower().Contains(s));
        }

        return q;
    }

    private static string? Validate(UpsertPricingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PlanName)) return "Plan name is required";
        if (request.PlanName.Trim().Length > PlanNameMaxLength)
            return $"Plan name must be {PlanNameMaxLength} characters or fewer";
        if (NormalizeCategory(request.PlanCategory) == null) return "Invalid plan category";
        if (request.ValidityMonths is < 1 or > ValidityMonthsMax)
            return $"Validity must be between 1 and {ValidityMonthsMax} months, or Lifetime";
        if (request.OriginalPrice < 0) return "Original price cannot be negative";
        if (request.OfferedPrice < 0) return "Offered price cannot be negative";
        return null;
    }

    private static ManagePricingRowDto MapEntity(PricingPlan p)
    {
        var status = string.IsNullOrWhiteSpace(p.Status) ? "Active" : p.Status.Trim();
        var tone = string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase) ? "success" : "muted";

        return new ManagePricingRowDto(
            p.Id,
            p.PlanName ?? "",
            p.PlanCategory ?? "",
            p.ValidityMonths,
            PricingFormat.Validity(p.ValidityMonths),
            p.OriginalPrice,
            PricingFormat.Money(p.OriginalPrice),
            p.OriginalPriceGst,
            PricingFormat.Money(p.OriginalPriceGst),
            p.OfferedPrice,
            PricingFormat.Money(p.OfferedPrice),
            p.OfferedPriceGst,
            PricingFormat.Money(p.OfferedPriceGst),
            status,
            tone,
            PricingFormat.Timestamp(p.CreatedAt));
    }

    private static string? NormalizeCategory(string? category) =>
        string.IsNullOrWhiteSpace(category)
            ? null
            : PlanCommissionValues.Categories.FirstOrDefault(c => c.Equals(category.Trim(), StringComparison.OrdinalIgnoreCase));

    private static string NormalizeStatus(string? status)
        => string.Equals(status, "Inactive", StringComparison.OrdinalIgnoreCase) ? "Inactive" : "Active";
}
