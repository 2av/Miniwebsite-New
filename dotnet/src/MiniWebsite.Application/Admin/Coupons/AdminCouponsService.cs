using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MiniWebsite.Application.Admin.Coupons.Dtos;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Domain.Common;
using MiniWebsite.Domain.Entities;

namespace MiniWebsite.Application.Admin.Coupons;

public partial class AdminCouponsService : IAdminCouponsService
{
    private const int MaxGenerateAttempts = 20;

    private readonly IApplicationDbContext _db;

    public AdminCouponsService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ApiResult<CouponsMetaDto>> GetMetaAsync(CancellationToken ct = default)
    {
        try
        {
            var roles = await _db.Roles.AsNoTracking()
                .OrderBy(r => r.RoleName)
                .Select(r => new CouponRoleOptionDto(r.RoleId, r.RoleName))
                .ToListAsync(ct);

            return ApiResult<CouponsMetaDto>.Ok(new CouponsMetaDto(
                roles,
                CouponValues.Types,
                [CouponValues.StatusActive, CouponValues.StatusInActive, CouponValues.StatusExpired]));
        }
        catch (Exception ex)
        {
            return ApiResult<CouponsMetaDto>.Fail("Unable to load coupon options: " + ex.Message);
        }
    }

    public async Task<ApiResult<CouponsPageDto>> ListAsync(CouponsQuery query, CancellationToken ct = default)
    {
        query.Page = query.Page < 1 ? 1 : query.Page;
        query.PageSize = query.PageSize is < 1 or > 100 ? 10 : query.PageSize;

        try
        {
            var q = ApplyFilters(_db.Coupons.AsNoTracking().Include(c => c.Role), query);
            var total = await q.CountAsync(ct);
            var rows = await q
                .OrderByDescending(c => c.Id)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(ct);

            return ApiResult<CouponsPageDto>.Ok(new CouponsPageDto(
                rows.Select(MapEntity).ToList(),
                total,
                query.Page,
                query.PageSize));
        }
        catch (Exception ex)
        {
            return ApiResult<CouponsPageDto>.Fail("Unable to load coupons: " + ex.Message);
        }
    }

    public async Task<ApiResult<CouponRowDto>> GetAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var row = await _db.Coupons.AsNoTracking().Include(c => c.Role).FirstOrDefaultAsync(c => c.Id == id, ct);
            if (row == null) return ApiResult<CouponRowDto>.Fail("Coupon not found");
            return ApiResult<CouponRowDto>.Ok(MapEntity(row));
        }
        catch (Exception ex)
        {
            return ApiResult<CouponRowDto>.Fail("Unable to load coupon: " + ex.Message);
        }
    }

    public async Task<ApiResult<CouponCodeDto>> GenerateCodeAsync(CancellationToken ct = default)
    {
        for (var attempt = 0; attempt < MaxGenerateAttempts; attempt++)
        {
            var code = CouponValues.GenerateCode();
            if (!await _db.Coupons.AnyAsync(c => c.CouponCode == code, ct))
                return ApiResult<CouponCodeDto>.Ok(new CouponCodeDto(code));
        }

        return ApiResult<CouponCodeDto>.Fail("Unable to generate a unique coupon code. Please try again.");
    }

    public async Task<ApiResult<CouponRowDto>> CreateAsync(UpsertCouponRequest request, CancellationToken ct = default)
    {
        var validation = await ValidateAsync(request, ct);
        if (validation != null) return ApiResult<CouponRowDto>.Fail(validation);

        var code = NormalizeCode(request.CouponCode);

        try
        {
            if (await _db.Coupons.AnyAsync(c => c.CouponCode == code, ct))
                return ApiResult<CouponRowDto>.Fail("Coupon code already exists!");

            var entity = new Coupon
            {
                CreatedBy = string.IsNullOrWhiteSpace(request.CreatedBy) ? "admin" : request.CreatedBy.Trim(),
                CreatedAt = AppClock.Now,
                CurrentUsage = 0,
            };
            Apply(entity, request, code);
            entity.Status = NormalizeStatus(request.Status) ?? CouponValues.StatusActive;

            _db.Coupons.Add(entity);
            await _db.SaveChangesAsync(ct);

            return await ReloadAsync(entity.Id, "Coupon Added Successfully!", ct);
        }
        catch (Exception ex)
        {
            return ApiResult<CouponRowDto>.Fail("Error adding coupon: " + ex.Message);
        }
    }

    public async Task<ApiResult<CouponRowDto>> UpdateAsync(int id, UpsertCouponRequest request, CancellationToken ct = default)
    {
        var validation = await ValidateAsync(request, ct);
        if (validation != null) return ApiResult<CouponRowDto>.Fail(validation);

        var code = NormalizeCode(request.CouponCode);

        try
        {
            var entity = await _db.Coupons.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (entity == null) return ApiResult<CouponRowDto>.Fail("Coupon not found");

            if (await _db.Coupons.AnyAsync(c => c.CouponCode == code && c.Id != id, ct))
                return ApiResult<CouponRowDto>.Fail("Coupon code already exists!");

            Apply(entity, request, code);
            var status = NormalizeStatus(request.Status);
            if (status != null) entity.Status = status;

            await _db.SaveChangesAsync(ct);
            return await ReloadAsync(entity.Id, "Coupon Updated Successfully!", ct);
        }
        catch (Exception ex)
        {
            return ApiResult<CouponRowDto>.Fail("Error updating coupon: " + ex.Message);
        }
    }

    public async Task<ApiResult> DeleteAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var entity = await _db.Coupons.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (entity == null) return ApiResult.Fail("Coupon not found");

            _db.Coupons.Remove(entity);
            await _db.SaveChangesAsync(ct);
            return ApiResult.Ok("Coupon deleted!");
        }
        catch (Exception ex)
        {
            return ApiResult.Fail("Unable to delete coupon: " + ex.Message);
        }
    }

    private async Task<ApiResult<CouponRowDto>> ReloadAsync(int id, string message, CancellationToken ct)
    {
        var row = await _db.Coupons.AsNoTracking().Include(c => c.Role).FirstAsync(c => c.Id == id, ct);
        return ApiResult<CouponRowDto>.Ok(MapEntity(row), message);
    }

    private static void Apply(Coupon entity, UpsertCouponRequest request, string code)
    {
        var discountType = NormalizeDiscountType(request.PromoDiscountType)!;
        entity.RoleId = request.RoleId;
        entity.CouponName = request.CouponName.Trim();
        entity.CouponCode = code;
        entity.CouponType = NormalizeCouponType(request.CouponType)!;
        entity.ValidityDate = request.ValidityDate.Date;
        entity.PromoDiscountType = discountType;
        entity.PromoDiscountValue = Math.Round(request.PromoDiscountValue, 2);
        entity.UsageLimitPerCoupon = request.UsageLimitPerCoupon;
        entity.UsageLimitPerUser = request.UsageLimitPerUser;
    }

    private static IQueryable<Coupon> ApplyFilters(IQueryable<Coupon> q, CouponsQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            q = q.Where(c =>
                c.CouponName.Contains(s)
                || c.CouponCode.Contains(s)
                || (c.Role != null && c.Role.RoleName.Contains(s)));
        }

        if (query.RoleId is > 0)
            q = q.Where(c => c.RoleId == query.RoleId);

        var today = AppClock.Today;
        var status = (query.Status ?? "").Trim();
        if (status.Equals(CouponValues.StatusExpired, StringComparison.OrdinalIgnoreCase))
            q = q.Where(c => c.ValidityDate != null && c.ValidityDate < today);
        else if (status.Equals(CouponValues.StatusActive, StringComparison.OrdinalIgnoreCase))
            q = q.Where(c => c.Status == CouponValues.StatusActive && (c.ValidityDate == null || c.ValidityDate >= today));
        else if (status.Equals(CouponValues.StatusInActive, StringComparison.OrdinalIgnoreCase))
            q = q.Where(c => c.Status != CouponValues.StatusActive && (c.ValidityDate == null || c.ValidityDate >= today));

        return q;
    }

    private async Task<string?> ValidateAsync(UpsertCouponRequest request, CancellationToken ct)
    {
        if (request.RoleId <= 0) return "Plan type is required";
        if (string.IsNullOrWhiteSpace(request.CouponName)) return "Coupon name is required";
        if (request.CouponName.Trim().Length > 200) return "Coupon name must be 200 characters or fewer";
        if (string.IsNullOrWhiteSpace(request.CouponCode)) return "Coupon code is required";
        if (!CouponCodePattern().IsMatch(NormalizeCode(request.CouponCode)))
            return "Coupon code must be 4 to 20 letters or numbers";
        if (NormalizeCouponType(request.CouponType) == null) return "Invalid coupon type";
        if (request.ValidityDate == default) return "Validity date is required";

        var discountType = NormalizeDiscountType(request.PromoDiscountType);
        if (discountType == null) return "Invalid promo discount type";
        if (request.PromoDiscountValue < 0) return "Promo discount cannot be negative";
        if (discountType == CouponValues.DiscountPercentage && request.PromoDiscountValue > 100)
            return "Promo discount percentage cannot be more than 100";

        if (request.UsageLimitPerCoupon < 0) return "Usage limit per coupon cannot be negative";
        if (request.UsageLimitPerUser < 0) return "Usage limit per user cannot be negative";
        if (request.UsageLimitPerCoupon > 0 && request.UsageLimitPerUser > request.UsageLimitPerCoupon)
            return "Usage limit per user cannot be more than the usage limit per coupon";

        if (!string.IsNullOrWhiteSpace(request.Status) && NormalizeStatus(request.Status) == null)
            return "Status must be Active or InActive";

        if (!await _db.Roles.AnyAsync(r => r.RoleId == request.RoleId, ct))
            return "Invalid plan type";

        return null;
    }

    private static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();

    private static string? NormalizeCouponType(string? couponType)
    {
        var raw = (couponType ?? "").Trim();
        return CouponValues.Types.FirstOrDefault(t => t.Equals(raw, StringComparison.OrdinalIgnoreCase));
    }

    private static string? NormalizeDiscountType(string? discountType)
    {
        var raw = (discountType ?? "").Trim();
        if (raw.Equals(CouponValues.DiscountAmount, StringComparison.OrdinalIgnoreCase)) return CouponValues.DiscountAmount;
        if (raw.Equals(CouponValues.DiscountPercentage, StringComparison.OrdinalIgnoreCase)) return CouponValues.DiscountPercentage;
        return null;
    }

    private static string? NormalizeStatus(string? status)
    {
        var raw = (status ?? "").Trim();
        if (raw.Equals(CouponValues.StatusActive, StringComparison.OrdinalIgnoreCase)) return CouponValues.StatusActive;
        if (raw.Equals(CouponValues.StatusInActive, StringComparison.OrdinalIgnoreCase)) return CouponValues.StatusInActive;
        return null;
    }

    private static string FormatLimit(int limit) =>
        limit <= 0 ? "Unlimited" : limit.ToString(CultureInfo.InvariantCulture);

    private static CouponRowDto MapEntity(Coupon c)
    {
        var culture = CultureInfo.InvariantCulture;
        var status = CouponValues.DisplayStatus(c.Status, c.ValidityDate);
        var tone = status switch
        {
            CouponValues.StatusActive => "ok",
            CouponValues.StatusExpired => "danger",
            _ => "neutral",
        };

        var discountType = NormalizeDiscountType(c.PromoDiscountType) ?? CouponValues.DiscountAmount;
        var discountDisplay = c.PromoDiscountValue <= 0
            ? "-"
            : discountType == CouponValues.DiscountPercentage
                ? c.PromoDiscountValue.ToString("0.##", culture) + "%"
                : "₹" + c.PromoDiscountValue.ToString("#,##0.##", CultureInfo.CreateSpecificCulture("en-IN"));

        return new CouponRowDto(
            c.Id,
            c.RoleId,
            c.Role?.RoleName ?? "-",
            c.CouponName ?? "",
            c.CouponCode ?? "",
            c.CouponType ?? "",
            c.CreatedAt,
            c.CreatedAt?.ToString("dd-MM-yyyy", culture) ?? "-",
            c.ValidityDate,
            c.ValidityDate?.ToString("dd-MM-yyyy", culture) ?? "-",
            discountType,
            c.PromoDiscountValue,
            discountDisplay,
            c.UsageLimitPerCoupon,
            FormatLimit(c.UsageLimitPerCoupon),
            c.UsageLimitPerUser,
            FormatLimit(c.UsageLimitPerUser),
            c.CurrentUsage,
            NormalizeStatus(c.Status) ?? CouponValues.StatusInActive,
            status,
            tone);
    }

    [GeneratedRegex("^[A-Z0-9]{4,20}$")]
    private static partial Regex CouponCodePattern();
}
