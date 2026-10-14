using System.Globalization;
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
    private const decimal GstRate = 1.18m;
    private const int PlanNameMaxLength = 200;
    private const string AllPlanType = "All";

    private readonly IApplicationDbContext _db;

    public AdminManagePricingService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ApiResult<ManagePricingMetaDto>> GetMetaAsync(CancellationToken ct = default)
    {
        try
        {
            var roles = await _db.Roles.AsNoTracking()
                .Where(r => r.Prefix != RolePrefixes.Administrator)
                .OrderBy(r => r.RoleName)
                .Select(r => new PricingRoleOptionDto(r.RoleId, r.RoleName))
                .ToListAsync(ct);

            var existingPlans = await _db.PricingPlans.AsNoTracking()
                .OrderBy(p => p.RoleId)
                .ThenBy(p => p.PlanName)
                .ThenBy(p => p.Id)
                .Select(p => new PricingPlanKeyDto(p.Id, p.RoleId, p.PlanName))
                .ToListAsync(ct);

            var planNames = existingPlans
                .Select(p => p.PlanName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return ApiResult<ManagePricingMetaDto>.Ok(new ManagePricingMetaDto(
                roles,
                planNames,
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
                _db.PricingPlans.AsNoTracking().Include(p => p.Role),
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
                .Include(p => p.Role)
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
        var validation = await ValidateAsync(request, ct);
        if (validation != null) return ApiResult<ManagePricingRowDto>.Fail(validation);

        try
        {
            var roleId = request.RoleId;
            var planName = request.PlanName.Trim();
            var original = RoundMoney(request.OriginalPrice);
            var offered = RoundMoney(request.OfferedPrice);
            var originalGst = WithGst(original);
            var offeredGst = WithGst(offered);

            var dup = await _db.PricingPlans.AnyAsync(
                p => p.RoleId == roleId && p.PlanName == planName, ct);
            if (dup)
                return ApiResult<ManagePricingRowDto>.Fail("This plan type and plan name combination already exists.");

            var now = AppClock.Now;
            var entity = new PricingPlan
            {
                RoleId = roleId,
                PlanName = planName,
                OriginalPrice = original,
                OriginalPriceGst = originalGst,
                OfferedPrice = offered,
                OfferedPriceGst = offeredGst,
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
        var validation = await ValidateAsync(request, ct);
        if (validation != null) return ApiResult<ManagePricingRowDto>.Fail(validation);

        try
        {
            var entity = await _db.PricingPlans.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (entity == null) return ApiResult<ManagePricingRowDto>.Fail("Pricing plan not found");

            var roleId = request.RoleId;
            var planName = request.PlanName.Trim();
            var original = RoundMoney(request.OriginalPrice);
            var offered = RoundMoney(request.OfferedPrice);
            var originalGst = WithGst(original);
            var offeredGst = WithGst(offered);

            var dup = await _db.PricingPlans.AnyAsync(
                p => p.RoleId == roleId && p.PlanName == planName && p.Id != id, ct);
            if (dup)
                return ApiResult<ManagePricingRowDto>.Fail("This plan type and plan name combination already exists.");

            entity.RoleId = roleId;
            entity.PlanName = planName;
            entity.OriginalPrice = original;
            entity.OriginalPriceGst = originalGst;
            entity.OfferedPrice = offered;
            entity.OfferedPriceGst = offeredGst;
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
        var status = string.IsNullOrWhiteSpace(query.Status)
                     || query.Status.Equals("all", StringComparison.OrdinalIgnoreCase)
            ? null
            : query.Status.Trim();

        if (query.RoleId is > 0)
            q = q.Where(p => p.RoleId == query.RoleId);
        else if (query.RoleId == 0)
            q = q.Where(p => p.RoleId == null);
        if (status != null)
            q = q.Where(p => p.Status == status);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLower();
            q = q.Where(p =>
                (p.Role != null ? p.Role.RoleName.ToLower().Contains(s) : AllPlanType.ToLower().Contains(s))
                || p.PlanName.ToLower().Contains(s));
        }

        return q;
    }

    private async Task<string?> ValidateAsync(UpsertPricingRequest request, CancellationToken ct)
    {
        if (request.RoleId is <= 0) return "Invalid plan type";
        if (string.IsNullOrWhiteSpace(request.PlanName)) return "Plan name is required";
        if (request.PlanName.Trim().Length > PlanNameMaxLength)
            return $"Plan name must be {PlanNameMaxLength} characters or fewer";
        if (request.OriginalPrice < 0) return "Original price cannot be negative";
        if (request.OfferedPrice < 0) return "Offered price cannot be negative";
        if (request.RoleId is { } roleId
            && !await _db.Roles.AnyAsync(r => r.RoleId == roleId && r.Prefix != RolePrefixes.Administrator, ct))
            return "Invalid plan type";
        return null;
    }

    private static ManagePricingRowDto MapEntity(PricingPlan p)
    {
        var original = p.OriginalPrice;
        var originalGst = p.OriginalPriceGst;
        var offered = p.OfferedPrice;
        var offeredGst = p.OfferedPriceGst;
        var status = string.IsNullOrWhiteSpace(p.Status) ? "Active" : p.Status.Trim();
        var tone = string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase) ? "success" : "muted";

        return new ManagePricingRowDto(
            p.Id,
            p.RoleId,
            p.RoleId == null ? AllPlanType : p.Role?.RoleName ?? "-",
            p.PlanName ?? "",
            original,
            FormatMoneyDisplay(original),
            originalGst,
            FormatMoneyDisplay(originalGst),
            offered,
            FormatMoneyDisplay(offered),
            offeredGst,
            FormatMoneyDisplay(offeredGst),
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
