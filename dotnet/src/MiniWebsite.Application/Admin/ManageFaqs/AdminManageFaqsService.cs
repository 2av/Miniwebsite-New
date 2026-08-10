using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MiniWebsite.Application.Admin.ManageFaqs.Dtos;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Domain.Entities;

namespace MiniWebsite.Application.Admin.ManageFaqs;

public class AdminManageFaqsService : IAdminManageFaqsService
{
    private static readonly FaqPageTypeOptionDto[] PageTypes =
    [
        new("home", "Home Page"),
        new("refer_earn", "Refer & Earn"),
        new("franchise", "Franchise"),
    ];

    private readonly IApplicationDbContext _db;

    public AdminManageFaqsService(IApplicationDbContext db)
    {
        _db = db;
    }

    public ManageFaqsMetaDto GetMeta() => new(PageTypes);

    public async Task<ApiResult<ManageFaqsPageDto>> ListAsync(ManageFaqsQuery query, CancellationToken ct = default)
    {
        query.Page = query.Page < 1 ? 1 : query.Page;
        query.PageSize = query.PageSize is < 1 or > 100 ? 10 : query.PageSize;

        try
        {
            var q = ApplyFilters(_db.Faqs.AsNoTracking(), query);
            var total = await q.CountAsync(ct);
            var rows = await q
                .OrderBy(f => f.PageType)
                .ThenBy(f => f.SortOrder)
                .ThenBy(f => f.Id)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(ct);

            return ApiResult<ManageFaqsPageDto>.Ok(new ManageFaqsPageDto(
                rows.Select(MapEntity).ToList(),
                total,
                query.Page,
                query.PageSize));
        }
        catch (Exception ex)
        {
            return ApiResult<ManageFaqsPageDto>.Fail("Unable to load FAQs: " + ex.Message);
        }
    }

    public async Task<ApiResult<ManageFaqRowDto>> GetAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var row = await _db.Faqs.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, ct);
            if (row == null) return ApiResult<ManageFaqRowDto>.Fail("FAQ not found");
            return ApiResult<ManageFaqRowDto>.Ok(MapEntity(row));
        }
        catch (Exception ex)
        {
            return ApiResult<ManageFaqRowDto>.Fail("Unable to load FAQ: " + ex.Message);
        }
    }

    public async Task<ApiResult<ManageFaqRowDto>> CreateAsync(UpsertFaqRequest request, CancellationToken ct = default)
    {
        var validation = Validate(request, requireStatus: false);
        if (validation != null) return ApiResult<ManageFaqRowDto>.Fail(validation);

        var pageType = request.PageType.Trim();
        var sortOrder = request.SortOrder < 1 ? 1 : request.SortOrder;
        var now = DateTime.UtcNow;

        try
        {
            var entity = new Faq
            {
                PageType = pageType,
                Question = request.Question.Trim(),
                Answer = request.Answer.Trim(),
                SortOrder = sortOrder,
                Status = "active",
                CreatedAt = now,
                UpdatedAt = now,
            };
            _db.Faqs.Add(entity);
            await _db.SaveChangesAsync(ct);

            return ApiResult<ManageFaqRowDto>.Ok(MapEntity(entity), "FAQ added successfully!");
        }
        catch (Exception ex)
        {
            return ApiResult<ManageFaqRowDto>.Fail("Error adding FAQ: " + ex.Message);
        }
    }

    public async Task<ApiResult<ManageFaqRowDto>> UpdateAsync(int id, UpsertFaqRequest request, CancellationToken ct = default)
    {
        var validation = Validate(request, requireStatus: true);
        if (validation != null) return ApiResult<ManageFaqRowDto>.Fail(validation);

        var pageType = request.PageType.Trim();
        var status = (request.Status ?? "active").Trim().ToLowerInvariant();
        var sortOrder = request.SortOrder < 1 ? 1 : request.SortOrder;

        try
        {
            var entity = await _db.Faqs.FirstOrDefaultAsync(f => f.Id == id, ct);
            if (entity == null) return ApiResult<ManageFaqRowDto>.Fail("FAQ not found");

            entity.PageType = pageType;
            entity.Question = request.Question.Trim();
            entity.Answer = request.Answer.Trim();
            entity.SortOrder = sortOrder;
            entity.Status = status;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return ApiResult<ManageFaqRowDto>.Ok(MapEntity(entity), "FAQ updated successfully!");
        }
        catch (Exception ex)
        {
            return ApiResult<ManageFaqRowDto>.Fail("Error updating FAQ: " + ex.Message);
        }
    }

    public async Task<ApiResult> DeleteAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var entity = await _db.Faqs.FirstOrDefaultAsync(f => f.Id == id, ct);
            if (entity == null) return ApiResult.Fail("FAQ not found");

            _db.Faqs.Remove(entity);
            await _db.SaveChangesAsync(ct);
            return ApiResult.Ok("FAQ deleted successfully!");
        }
        catch (Exception ex)
        {
            return ApiResult.Fail("Error deleting FAQ: " + ex.Message);
        }
    }

    private static IQueryable<Faq> ApplyFilters(IQueryable<Faq> q, ManageFaqsQuery query)
    {
        var pageType = NormalizeFilter(query.PageType);
        var status = NormalizeFilter(query.Status);
        if (pageType != null)
            q = q.Where(f => f.PageType == pageType);
        if (status != null)
            q = q.Where(f => f.Status == status);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLower();
            q = q.Where(f =>
                f.Question.ToLower().Contains(s)
                || f.Answer.ToLower().Contains(s));
        }

        return q;
    }

    private static string? Validate(UpsertFaqRequest request, bool requireStatus)
    {
        if (string.IsNullOrWhiteSpace(request.PageType)) return "Page type is required";
        if (string.IsNullOrWhiteSpace(request.Question)) return "Question is required";
        if (string.IsNullOrWhiteSpace(request.Answer)) return "Answer is required";

        var pageType = request.PageType.Trim();
        if (PageTypes.All(p => p.Value != pageType))
            return "Invalid page type";

        if (requireStatus)
        {
            var status = (request.Status ?? "").Trim().ToLowerInvariant();
            if (status is not ("active" or "inactive"))
                return "Invalid status";
        }

        return null;
    }

    private static string? NormalizeFilter(string? value) =>
        string.IsNullOrWhiteSpace(value) || value.Equals("all", StringComparison.OrdinalIgnoreCase)
            ? null
            : value.Trim();

    private static ManageFaqRowDto MapEntity(Faq f)
    {
        var pageType = f.PageType ?? "";
        var label = PageTypes.FirstOrDefault(p => p.Value == pageType)?.Label
                    ?? CultureInfo.InvariantCulture.TextInfo.ToTitleCase(pageType.Replace('_', ' '));
        var status = string.IsNullOrWhiteSpace(f.Status) ? "active" : f.Status.ToLowerInvariant();
        var tone = status == "active" ? "ok" : "danger";

        return new ManageFaqRowDto(
            f.Id,
            pageType,
            label,
            f.Question ?? "",
            f.Answer ?? "",
            StripHtmlPreview(f.Answer, 120),
            f.SortOrder,
            status,
            tone);
    }

    private static string StripHtmlPreview(string? html, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(html)) return "";
        var text = WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]+>", " "));
        text = Regex.Replace(text, @"\s+", " ").Trim();
        if (text.Length <= maxLen) return text;
        return text[..maxLen].TrimEnd() + "…";
    }
}
