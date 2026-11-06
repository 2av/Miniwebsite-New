using System.Globalization;
using Microsoft.EntityFrameworkCore;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Application.Uploads;
using MiniWebsite.Domain.Common;
using MiniWebsite.Domain.Entities;

namespace MiniWebsite.Application.Admin.Onboarding;

public record FieldSalesSummaryDto(decimal MonthlyTarget, int DealCount, IReadOnlyList<FieldSalesDealDto> Deals);

public record FieldSalesDealDto(int Id, string Details, decimal Amount, string DealDate);

public record AddFieldSalesDealRequest(string Details, decimal Amount, DateTime? DealDate);

public record FieldSalesDocumentsRequest(
    string? PanBase64,
    string? AadhaarFrontBase64,
    string? AadhaarBackBase64,
    string? GstBase64,
    string? AppointmentLetterBase64);

public interface IFieldSalesAdminService
{
    Task<ApiResult<FieldSalesSummaryDto>> SummaryAsync(int userId, CancellationToken ct = default);
    Task<ApiResult<FieldSalesSummaryDto>> SummaryForEmailAsync(string email, CancellationToken ct = default);
    Task<ApiResult> AddDealAsync(int userId, AddFieldSalesDealRequest request, CancellationToken ct = default);
    Task<ApiResult> SaveDocumentsAsync(int userId, FieldSalesDocumentsRequest request, CancellationToken ct = default);
}

public class FieldSalesAdminService : IFieldSalesAdminService
{
    private readonly IApplicationDbContext _db;
    private readonly IObjectStorage _storage;
    private readonly IUploadedFileTracker _files;

    public FieldSalesAdminService(IApplicationDbContext db, IObjectStorage storage, IUploadedFileTracker files)
    {
        _db = db;
        _storage = storage;
        _files = files;
    }

    public async Task<ApiResult<FieldSalesSummaryDto>> SummaryAsync(int userId, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null) return ApiResult<FieldSalesSummaryDto>.Fail("User not found.");
        if (!string.Equals(user.AssignedRole?.Prefix, RolePrefixes.FieldSalesTeam, StringComparison.OrdinalIgnoreCase))
            return ApiResult<FieldSalesSummaryDto>.Fail("Monthly targets are for Field Sales Team.");
        return ApiResult<FieldSalesSummaryDto>.Ok(await BuildAsync(user.Id, ct));
    }

    public async Task<ApiResult<FieldSalesSummaryDto>> SummaryForEmailAsync(string email, CancellationToken ct = default)
    {
        var key = (email ?? "").Trim().ToLowerInvariant();
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email.ToLower() == key, ct);
        if (user == null) return ApiResult<FieldSalesSummaryDto>.Fail("User not found.");
        return await SummaryAsync(user.Id, ct);
    }

    public async Task<ApiResult> AddDealAsync(int userId, AddFieldSalesDealRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null) return ApiResult.Fail("User not found.");
        if (!string.Equals(user.AssignedRole?.Prefix, RolePrefixes.FieldSalesTeam, StringComparison.OrdinalIgnoreCase))
            return ApiResult.Fail("Deals can be added for Field Sales Team only.");

        var details = (request.Details ?? "").Trim();
        if (details.Length == 0) return ApiResult.Fail("Deal details are required.");
        if (request.Amount < 0) return ApiResult.Fail("Deal amount cannot be negative.");

        _db.FieldSalesDeals.Add(new FieldSalesDeal
        {
            UserId = user.Id,
            Details = details.Length > 2000 ? details[..2000] : details,
            Amount = request.Amount,
            DealDate = request.DealDate ?? AppClock.Now,
            CreatedAt = AppClock.Now,
        });
        await _db.SaveChangesAsync(ct);
        return ApiResult.Ok("Deal details saved.");
    }

    public async Task<ApiResult> SaveDocumentsAsync(int userId, FieldSalesDocumentsRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null) return ApiResult.Fail("User not found.");
        if (!string.Equals(user.AssignedRole?.Prefix, RolePrefixes.FieldSalesTeam, StringComparison.OrdinalIgnoreCase))
            return ApiResult.Fail("These documents are stored for Field Sales Team.");
        if (!_storage.IsEnabled) return ApiResult.Fail("Cloudflare storage is not configured.");

        var slots = new (string? Raw, string Kind, string Document)[]
        {
            (request.PanBase64, "pan", UploadedFileDocument.Pan),
            (request.AadhaarFrontBase64, "aadhaar_front", UploadedFileDocument.AadhaarFront),
            (request.AadhaarBackBase64, "aadhaar_back", UploadedFileDocument.AadhaarBack),
            (request.GstBase64, "gst", UploadedFileDocument.GstCertificate),
            (request.AppointmentLetterBase64, "appointment", UploadedFileDocument.AppointmentLetter),
        };
        var saved = 0;
        foreach (var slot in slots)
        {
            if (string.IsNullOrWhiteSpace(slot.Raw)) continue;
            var tracked = await StoreAsync(user.UserCode, slot.Raw, slot.Kind, slot.Document, ct);
            if (tracked == null) continue;
            await _files.TrackAsync(user.Id, tracked, ct);
            saved++;
        }

        if (saved == 0) return ApiResult.Fail("Choose at least one document to upload.");
        await _db.SaveChangesAsync(ct);
        return ApiResult.Ok("Documents saved.");
    }

    private async Task<FieldSalesSummaryDto> BuildAsync(int userId, CancellationToken ct)
    {
        var now = AppClock.Now;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var nextMonth = monthStart.AddMonths(1);
        var deals = await _db.FieldSalesDeals.AsNoTracking()
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.DealDate)
            .ToListAsync(ct);
        var monthly = deals.Where(d => d.DealDate >= monthStart && d.DealDate < nextMonth).Sum(d => d.Amount);
        var rows = deals.Select(d => new FieldSalesDealDto(
            d.Id,
            d.Details,
            d.Amount,
            d.DealDate.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture))).ToList();
        return new FieldSalesSummaryDto(monthly, deals.Count, rows);
    }

    private async Task<NewUploadedFile?> StoreAsync(
        string userCode, string raw, string kind, string document, CancellationToken ct)
    {
        var data = raw.Trim();
        var comma = data.IndexOf(',');
        if (data.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma > 0)
            data = data[(comma + 1)..];
        byte[] bytes;
        try { bytes = Convert.FromBase64String(data); }
        catch { return null; }
        if (bytes.Length == 0 || bytes.Length > 2_500_000) return null;

        var stamp = AppClock.Now.ToString("yyMMddHHmmss", CultureInfo.InvariantCulture);
        var fileName = $"{stamp}_{kind}.jpg";
        var key = _storage.BuildUserKey(userCode, _storage.VerificationPrefix, fileName);
        await _storage.UploadAsync(key, bytes, "image/jpeg", ct);
        return new NewUploadedFile(
            UploadedFileFolder.Verification, document, fileName, fileName, key, "image/jpeg", bytes.LongLength);
    }
}
