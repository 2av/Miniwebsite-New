using Microsoft.EntityFrameworkCore;
using MiniWebsite.Application.Admin.ManagePricing;
using MiniWebsite.Application.Admin.PlanCommissions.Dtos;
using MiniWebsite.Application.Common.Extensions;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Domain.Common;
using MiniWebsite.Domain.Entities;

namespace MiniWebsite.Application.Admin.PlanCommissions;

public class AdminPlanCommissionsService : IAdminPlanCommissionsService
{
    private readonly IApplicationDbContext _db;

    public AdminPlanCommissionsService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ApiResult<PlanCommissionsMetaDto>> GetMetaAsync(CancellationToken ct = default)
    {
        try
        {
            var roles = await _db.Roles.AsNoTracking()
                .Where(r => r.Prefix != RolePrefixes.Administrator)
                .OrderBy(r => r.RoleName)
                .Select(r => new PlanCommissionRoleOptionDto(r.RoleId, r.RoleName))
                .ToListAsync(ct);

            var plans = await _db.PricingPlans.AsNoTracking()
                .OrderBy(p => p.PlanCategory)
                .ThenBy(p => p.ValidityMonths == null)
                .ThenBy(p => p.ValidityMonths)
                .ThenBy(p => p.PlanName)
                .ToListAsync(ct);

            return ApiResult<PlanCommissionsMetaDto>.Ok(new PlanCommissionsMetaDto(
                roles,
                plans.Select(p => new PlanCommissionPlanOptionDto(
                    p.Id,
                    p.PlanName,
                    p.PlanCategory,
                    PricingFormat.Validity(p.ValidityMonths),
                    p.OfferedPrice,
                    p.Status)).ToList(),
                PlanCommissionValues.DiscountTypes,
                PlanCommissionValues.EarningTypes,
                PlanCommissionValues.GstRate));
        }
        catch (Exception ex)
        {
            return ApiResult<PlanCommissionsMetaDto>.Fail("Unable to load plan commission meta: " + ex.Message);
        }
    }

    public async Task<ApiResult<PlanCommissionsPageDto>> ListAsync(PlanCommissionsQuery query, CancellationToken ct = default)
    {
        query.Page = query.Page < 1 ? 1 : query.Page;
        query.PageSize = query.PageSize is < 1 or > 100 ? 10 : query.PageSize;

        try
        {
            var q = ApplyFilters(
                _db.PlanCommissions.AsNoTracking().Include(c => c.Role).Include(c => c.PricingPlan),
                query);

            var total = await q.CountAsync(ct);
            var rows = await q
                .OrderBy(c => c.Role.RoleName)
                .ThenBy(c => c.PricingPlan.PlanCategory)
                .ThenBy(c => c.PricingPlan.ValidityMonths == null)
                .ThenBy(c => c.PricingPlan.ValidityMonths)
                .ThenBy(c => c.Id)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(ct);

            return ApiResult<PlanCommissionsPageDto>.Ok(new PlanCommissionsPageDto(
                rows.Select(MapEntity).ToList(),
                total,
                query.Page,
                query.PageSize));
        }
        catch (Exception ex)
        {
            return ApiResult<PlanCommissionsPageDto>.Fail("Unable to load plan commissions: " + ex.Message);
        }
    }

    public async Task<ApiResult<PlanCommissionRowDto>> GetAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var row = await _db.PlanCommissions.AsNoTracking()
                .Include(c => c.Role)
                .Include(c => c.PricingPlan)
                .FirstOrDefaultAsync(c => c.Id == id, ct);
            if (row == null) return ApiResult<PlanCommissionRowDto>.Fail("Plan commission not found");
            return ApiResult<PlanCommissionRowDto>.Ok(MapEntity(row));
        }
        catch (Exception ex)
        {
            return ApiResult<PlanCommissionRowDto>.Fail("Unable to load plan commission: " + ex.Message);
        }
    }

    public async Task<ApiResult<IReadOnlyList<PlanCommissionRowDto>>> CreateAsync(UpsertPlanCommissionRequest request, CancellationToken ct = default)
    {
        var roleIds = (request.RoleIds ?? []).Distinct().ToList();
        if (roleIds.Count == 0) return ApiResult<IReadOnlyList<PlanCommissionRowDto>>.Fail("Select at least one role");

        try
        {
            var validation = await ValidateAsync(roleIds, request, ct);
            if (validation != null) return ApiResult<IReadOnlyList<PlanCommissionRowDto>>.Fail(validation);

            var existing = await _db.PlanCommissions.AsNoTracking()
                .Where(c => c.PricingPlanId == request.PricingPlanId && roleIds.Contains(c.RoleId))
                .Select(c => c.Role.RoleName)
                .ToListAsync(ct);
            if (existing.Count > 0)
                return ApiResult<IReadOnlyList<PlanCommissionRowDto>>.Fail(
                    "Commission already exists for this plan and role: " + string.Join(", ", existing));

            var now = AppClock.Now;
            var entities = roleIds.Select(roleId =>
            {
                var e = new PlanCommission
                {
                    RoleId = roleId,
                    Status = PlanCommissionValues.StatusActive,
                    CreatedAt = now,
                    UpdatedAt = now,
                };
                Apply(e, request);
                return e;
            }).ToList();
            _db.PlanCommissions.AddRange(entities);
            await _db.SaveChangesAsync(ct);

            var ids = entities.Select(e => e.Id).ToList();
            var rows = await _db.PlanCommissions.AsNoTracking()
                .Include(c => c.Role)
                .Include(c => c.PricingPlan)
                .Where(c => ids.Contains(c.Id))
                .ToListAsync(ct);

            var message = entities.Count == 1
                ? "Plan commission added successfully!"
                : $"{entities.Count} plan commissions added successfully!";
            return ApiResult<IReadOnlyList<PlanCommissionRowDto>>.Ok(rows.Select(MapEntity).ToList(), message);
        }
        catch (Exception ex)
        {
            return ApiResult<IReadOnlyList<PlanCommissionRowDto>>.Fail("Error adding plan commission: " + ex.Message);
        }
    }

    public async Task<ApiResult<PlanCommissionRowDto>> UpdateAsync(int id, UpsertPlanCommissionRequest request, CancellationToken ct = default)
    {
        if (request.RoleIds is not { Count: 1 }) return ApiResult<PlanCommissionRowDto>.Fail("Select one role");
        var roleId = request.RoleIds[0];

        try
        {
            var entity = await _db.PlanCommissions.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (entity == null) return ApiResult<PlanCommissionRowDto>.Fail("Plan commission not found");

            var validation = await ValidateAsync([roleId], request, ct);
            if (validation != null) return ApiResult<PlanCommissionRowDto>.Fail(validation);

            if (await _db.PlanCommissions.AnyAsync(
                    c => c.RoleId == roleId && c.PricingPlanId == request.PricingPlanId && c.Id != id, ct))
                return ApiResult<PlanCommissionRowDto>.Fail("Commission already exists for this plan and role.");

            entity.RoleId = roleId;
            Apply(entity, request);
            if (!string.IsNullOrWhiteSpace(request.Status))
                entity.Status = NormalizeStatus(request.Status);
            entity.UpdatedAt = AppClock.Now;
            await _db.SaveChangesAsync(ct);

            return await GetAsync(id, ct) switch
            {
                { Success: true, Data: { } data } => ApiResult<PlanCommissionRowDto>.Ok(data, "Plan commission updated successfully!"),
                var r => r
            };
        }
        catch (Exception ex)
        {
            return ApiResult<PlanCommissionRowDto>.Fail("Error updating plan commission: " + ex.Message);
        }
    }

    public async Task<ApiResult> ToggleStatusAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var entity = await _db.PlanCommissions.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (entity == null) return ApiResult.Fail("Plan commission not found");

            entity.Status = entity.Status == PlanCommissionValues.StatusActive
                ? PlanCommissionValues.StatusInActive
                : PlanCommissionValues.StatusActive;
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
            var entity = await _db.PlanCommissions.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (entity == null) return ApiResult.Fail("Plan commission not found");

            _db.PlanCommissions.Remove(entity);
            await _db.SaveChangesAsync(ct);
            return ApiResult.Ok("Plan commission deleted!");
        }
        catch (Exception ex)
        {
            return ApiResult.Fail("Unable to delete plan commission: " + ex.Message);
        }
    }

    private static void Apply(PlanCommission entity, UpsertPlanCommissionRequest request)
    {
        var discountType = Normalize(request.DiscountType, PlanCommissionValues.DiscountTypes)!;
        var earningType = Normalize(request.EarningType, PlanCommissionValues.EarningTypes)!;

        entity.PricingPlanId = request.PricingPlanId;
        entity.DiscountType = discountType;
        entity.DiscountAmount = discountType == PlanCommissionValues.DiscountFixed
            ? PricingFormat.RoundMoney(request.DiscountAmount)
            : 0;
        entity.EarningType = earningType;
        entity.EarningAmount = earningType == PlanCommissionValues.EarningFixed
            ? PricingFormat.RoundMoney(request.EarningAmount)
            : 0;
        entity.RenewalPrice = request.RenewalPrice is { } renewal ? PricingFormat.RoundMoney(renewal) : null;
        entity.RenewalPriceGst = entity.RenewalPrice is { } r ? PlanCommissionValues.WithGst(r) : null;
        entity.RenewalEarningPercent = request.RenewalEarningPercent is { } pct
            ? Math.Round(pct, 2, MidpointRounding.AwayFromZero)
            : null;
        entity.DeductFromWallet = request.DeductFromWallet;
    }

    private async Task<string?> ValidateAsync(IReadOnlyList<int> roleIds, UpsertPlanCommissionRequest request, CancellationToken ct)
    {
        var discountType = Normalize(request.DiscountType, PlanCommissionValues.DiscountTypes);
        var earningType = Normalize(request.EarningType, PlanCommissionValues.EarningTypes);
        if (discountType == null) return "Invalid discount type";
        if (earningType == null) return "Invalid earning type";
        if (request.DiscountAmount < 0) return "Discount cannot be negative";
        if (request.EarningAmount < 0) return "Earning cannot be negative";
        if (request.RenewalPrice < 0) return "Renewal price cannot be negative";
        if (request.RenewalEarningPercent is <= 0 or > 100) return "Renewal earning must be between 0 and 100%";
        if (request.RenewalEarningPercent != null && request.RenewalPrice == null)
            return "Enter the renewal price to set a renewal earning";

        var validRoles = await _db.Roles.CountAsync(
            r => roleIds.Contains(r.RoleId) && r.Prefix != RolePrefixes.Administrator, ct);
        if (validRoles != roleIds.Count) return "Invalid role";

        var plan = await _db.PricingPlans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.PricingPlanId, ct);
        if (plan == null) return "Select a plan";

        var discount = discountType == PlanCommissionValues.DiscountFixed ? request.DiscountAmount : 0;
        var earning = earningType == PlanCommissionValues.EarningFixed ? request.EarningAmount : 0;
        var deducted = request.DeductFromWallet ? earning : discount + earning;
        if (deducted > plan.OfferedPrice)
            return request.DeductFromWallet
                ? "Earning cannot be more than the plan price"
                : "Discount plus earning cannot be more than the plan price";

        return null;
    }

    private static IQueryable<PlanCommission> ApplyFilters(IQueryable<PlanCommission> q, PlanCommissionsQuery query)
    {
        var status = string.IsNullOrWhiteSpace(query.Status)
                     || query.Status.Equals("all", StringComparison.OrdinalIgnoreCase)
            ? null
            : query.Status.Trim();

        if (query.RoleId is > 0) q = q.Where(c => c.RoleId == query.RoleId);
        if (query.PricingPlanId is > 0) q = q.Where(c => c.PricingPlanId == query.PricingPlanId);
        if (status != null) q = q.Where(c => c.Status == status);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLower();
            q = q.Where(c => c.Role.RoleName.ToLower().Contains(s) || c.PricingPlan.PlanName.ToLower().Contains(s));
        }

        return q;
    }

    private static PlanCommissionRowDto MapEntity(PlanCommission c)
    {
        var plan = c.PricingPlan;
        var price = plan.OfferedPrice;
        var isFixedDiscount = c.DiscountType == PlanCommissionValues.DiscountFixed;
        var isFixedEarning = c.EarningType == PlanCommissionValues.EarningFixed;
        var discount = isFixedDiscount ? c.DiscountAmount : 0;

        decimal? earningGst = null, sbRevenue = null, wallet = null;
        if (isFixedEarning)
        {
            earningGst = PlanCommissionValues.WithGst(c.EarningAmount);
            sbRevenue = c.DeductFromWallet ? price - c.EarningAmount : price - discount - c.EarningAmount;
            if (c.DeductFromWallet) wallet = PlanCommissionValues.WithGst(sbRevenue.Value);
        }

        decimal? renewalEarning = c.RenewalEarningPercent is { } pct && c.RenewalPrice is { } renewal
            ? Math.Round(renewal * pct / 100m, 2, MidpointRounding.AwayFromZero)
            : null;

        var status = c.Status == PlanCommissionValues.StatusActive
            ? PlanCommissionValues.StatusActive
            : PlanCommissionValues.StatusInActive;

        return new PlanCommissionRowDto(
            c.Id,
            c.RoleId,
            c.Role?.RoleName ?? "-",
            c.PricingPlanId,
            plan.PlanName,
            plan.PlanCategory,
            PricingFormat.Validity(plan.ValidityMonths),
            price,
            PricingFormat.Money(price),
            c.DiscountType,
            c.DiscountAmount,
            isFixedDiscount ? PricingFormat.Money(c.DiscountAmount) : c.DiscountType,
            c.EarningType,
            c.EarningAmount,
            isFixedEarning ? PricingFormat.Money(c.EarningAmount) : c.EarningType,
            earningGst,
            PricingFormat.Money(earningGst),
            sbRevenue,
            PricingFormat.Money(sbRevenue),
            c.DeductFromWallet,
            wallet,
            PricingFormat.Money(wallet),
            c.RenewalPrice,
            PricingFormat.Money(c.RenewalPrice),
            c.RenewalPriceGst,
            PricingFormat.Money(c.RenewalPriceGst),
            c.RenewalEarningPercent,
            renewalEarning,
            PricingFormat.Money(renewalEarning),
            status,
            status == PlanCommissionValues.StatusActive ? "success" : "muted",
            PricingFormat.Timestamp(c.CreatedAt));
    }

    private static string? Normalize(string? value, IReadOnlyList<string> allowed) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : allowed.FirstOrDefault(a => a.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase));

    private static string NormalizeStatus(string? status) =>
        string.Equals(status?.Trim(), PlanCommissionValues.StatusInActive, StringComparison.OrdinalIgnoreCase)
            ? PlanCommissionValues.StatusInActive
            : PlanCommissionValues.StatusActive;
}
