using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MiniWebsite.Application.Admin.GrowWithMw.Dtos;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Application.Common.Options;
using MiniWebsite.Domain.Entities;
using MiniWebsite.Domain.Common;

namespace MiniWebsite.Application.Admin.GrowWithMw;

public class AdminGrowWithMwService : IAdminGrowWithMwService
{
    private readonly IApplicationDbContext _db;
    private readonly AppOptions _app;
    private bool _seedChecked;

    public AdminGrowWithMwService(IApplicationDbContext db, IOptions<AppOptions> app)
    {
        _db = db;
        _app = app.Value;
    }

    public async Task<GrowWithMwMetaDto> GetMetaAsync(CancellationToken ct = default)
    {
        await EnsureSeedAsync(ct);
        var sections = await ListSectionOptionsAsync(ct);
        return new GrowWithMwMetaDto(sections, PublicDocsPrefix(), GrowWithMwPrefix());
    }

    public async Task<ApiResult<DocPagesPageDto>> ListPagesAsync(DocPagesQuery query, CancellationToken ct = default)
    {
        query.Page = query.Page < 1 ? 1 : query.Page;
        query.PageSize = query.PageSize is < 1 or > 100 ? 10 : query.PageSize;

        try
        {
            await EnsureSeedAsync(ct);

            var q =
                from p in _db.DocPages.AsNoTracking()
                join s in _db.DocSections.AsNoTracking() on p.SectionId equals s.Id
                select new { Page = p, Section = s };

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var s = query.Search.Trim().ToLower();
                q = q.Where(x =>
                    x.Page.Title.ToLower().Contains(s)
                    || x.Page.Slug.ToLower().Contains(s)
                    || (x.Page.MetaTitle != null && x.Page.MetaTitle.ToLower().Contains(s)));
            }

            if (query.SectionId is > 0)
            {
                var sectionId = query.SectionId.Value;
                q = q.Where(x => x.Page.SectionId == sectionId);
            }

            var status = NormalizePageStatus(query.Status);
            if (status != null)
                q = q.Where(x => x.Page.Status == status);

            var total = await q.CountAsync(ct);
            var rows = await q
                .OrderBy(x => x.Section.SortOrder)
                .ThenBy(x => x.Page.SortOrder)
                .ThenBy(x => x.Page.Id)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(ct);

            var items = rows.Select(r => new DocPageListItemDto(
                r.Page.Id,
                r.Page.Title ?? "",
                r.Page.Slug ?? "",
                r.Page.Status ?? "draft",
                (r.Page.Status ?? "") == "published" ? "ok" : "neutral",
                r.Page.SortOrder,
                FormatDateTime(r.Page.UpdatedAt),
                r.Page.SectionId,
                r.Section.Title ?? "")).ToList();

            return ApiResult<DocPagesPageDto>.Ok(new DocPagesPageDto(items, total, query.Page, query.PageSize));
        }
        catch (Exception ex)
        {
            return ApiResult<DocPagesPageDto>.Fail("Unable to load pages: " + ex.Message);
        }
    }

    public async Task<ApiResult<DocPageDetailDto>> GetPageAsync(int id, CancellationToken ct = default)
    {
        await EnsureSeedAsync(ct);
        var row = await LoadPageAsync(id, ct);
        if (row == null) return ApiResult<DocPageDetailDto>.Fail("Page not found.");
        return ApiResult<DocPageDetailDto>.Ok(row);
    }

    public async Task<ApiResult<DocPageDetailDto>> UpsertPageAsync(int? id, UpsertDocPageRequest request, CancellationToken ct = default)
    {
        await EnsureSeedAsync(ct);

        var title = (request.Title ?? "").Trim();
        var sectionId = request.SectionId;
        var action = (request.Action ?? "draft").Trim().ToLowerInvariant();
        var publish = action == "publish";
        var status = publish ? "published" : "draft";
        var content = request.ContentHtml ?? "";
        var metaTitle = (request.MetaTitle ?? "").Trim();
        var metaDesc = (request.MetaDescription ?? "").Trim();
        var metaKw = (request.MetaKeywords ?? "").Trim();

        if (title.Length == 0 || sectionId <= 0)
            return ApiResult<DocPageDetailDto>.Fail("Title and section are required.");

        if (!await _db.DocSections.AnyAsync(s => s.Id == sectionId, ct))
            return ApiResult<DocPageDetailDto>.Fail("Invalid section.");

        try
        {
            var slugBase = string.IsNullOrWhiteSpace(request.Slug) ? title : request.Slug!;
            var slug = await UniquePageSlugAsync(Slugify(slugBase), id, ct);
            var now = AppClock.Now;

            if (id is null or <= 0)
            {
                var nextOrd = (await _db.DocPages
                    .Where(p => p.SectionId == sectionId)
                    .Select(p => (int?)p.SortOrder)
                    .MaxAsync(ct) ?? 0) + 1;

                var entity = new DocPage
                {
                    SectionId = sectionId,
                    Title = title,
                    Slug = slug,
                    Status = status,
                    ContentHtml = content,
                    MetaTitle = metaTitle,
                    MetaDescription = metaDesc,
                    MetaKeywords = metaKw,
                    SortOrder = nextOrd,
                    PublishedAt = publish ? now : null,
                    CreatedAt = now,
                    UpdatedAt = now,
                };
                _db.DocPages.Add(entity);
                await _db.SaveChangesAsync(ct);

                return await GetPageAsync(entity.Id, ct) switch
                {
                    { Success: true, Data: { } data } => ApiResult<DocPageDetailDto>.Ok(data, publish ? "Page published." : "Draft saved."),
                    var r => r
                };
            }

            var existing = await _db.DocPages.FirstOrDefaultAsync(p => p.Id == id.Value, ct);
            if (existing == null) return ApiResult<DocPageDetailDto>.Fail("Page not found.");

            existing.SectionId = sectionId;
            existing.Title = title;
            existing.Slug = slug;
            existing.Status = status;
            existing.ContentHtml = content;
            existing.MetaTitle = metaTitle;
            existing.MetaDescription = metaDesc;
            existing.MetaKeywords = metaKw;
            existing.UpdatedAt = now;
            if (publish)
                existing.PublishedAt ??= now;

            await _db.SaveChangesAsync(ct);

            return await GetPageAsync(id.Value, ct) switch
            {
                { Success: true, Data: { } data } => ApiResult<DocPageDetailDto>.Ok(data, publish ? "Page published." : "Draft saved."),
                var r => r
            };
        }
        catch (Exception ex)
        {
            return ApiResult<DocPageDetailDto>.Fail("Could not save page: " + ex.Message);
        }
    }

    public async Task<ApiResult> DeletePageAsync(int id, CancellationToken ct = default)
    {
        await EnsureSeedAsync(ct);
        try
        {
            var entity = await _db.DocPages.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (entity == null) return ApiResult.Fail("Page not found.");
            _db.DocPages.Remove(entity);
            await _db.SaveChangesAsync(ct);
            return ApiResult.Ok("Page deleted.");
        }
        catch (Exception ex)
        {
            return ApiResult.Fail("Could not delete page: " + ex.Message);
        }
    }

    public async Task<ApiResult> ReorderPagesAsync(ReorderPagesRequest request, CancellationToken ct = default)
    {
        await EnsureSeedAsync(ct);
        if (request.SectionId <= 0 || request.Order == null || request.Order.Count == 0)
            return ApiResult.Fail("Invalid payload");

        try
        {
            var ids = request.Order.Where(x => x > 0).ToList();
            var pages = await _db.DocPages
                .Where(p => p.SectionId == request.SectionId && ids.Contains(p.Id))
                .ToListAsync(ct);

            var pos = 0;
            foreach (var pageId in ids)
            {
                var page = pages.FirstOrDefault(p => p.Id == pageId);
                if (page != null)
                {
                    page.SortOrder = pos;
                    page.UpdatedAt = AppClock.Now;
                }
                pos++;
            }

            await _db.SaveChangesAsync(ct);
            return ApiResult.Ok("Order saved.");
        }
        catch (Exception ex)
        {
            return ApiResult.Fail("Reorder failed: " + ex.Message);
        }
    }

    public async Task<ApiResult<IReadOnlyList<DocSectionDto>>> ListSectionsAsync(CancellationToken ct = default)
    {
        await EnsureSeedAsync(ct);
        try
        {
            var pageCounts = await _db.DocPages.AsNoTracking()
                .GroupBy(p => p.SectionId)
                .Select(g => new { SectionId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.SectionId, x => x.Count, ct);

            var rows = await _db.DocSections.AsNoTracking()
                .OrderBy(s => s.SortOrder)
                .ThenBy(s => s.Id)
                .ToListAsync(ct);

            var list = rows.Select(s => new DocSectionDto(
                s.Id,
                s.Title ?? "",
                s.Slug ?? "",
                s.Description ?? "",
                s.SortOrder,
                s.CollapsedDefault,
                pageCounts.GetValueOrDefault(s.Id))).ToList();

            return ApiResult<IReadOnlyList<DocSectionDto>>.Ok(list);
        }
        catch (Exception ex)
        {
            return ApiResult<IReadOnlyList<DocSectionDto>>.Fail("Unable to load sections: " + ex.Message);
        }
    }

    public async Task<ApiResult<DocSectionDto>> CreateSectionAsync(UpsertDocSectionRequest request, CancellationToken ct = default)
    {
        await EnsureSeedAsync(ct);
        var title = (request.Title ?? "").Trim();
        if (title.Length == 0) return ApiResult<DocSectionDto>.Fail("Title is required.");

        try
        {
            var slug = await UniqueSectionSlugAsync(
                Slugify(string.IsNullOrWhiteSpace(request.Slug) ? title : request.Slug!),
                null,
                ct);
            var nextOrd = (await _db.DocSections.Select(s => (int?)s.SortOrder).MaxAsync(ct) ?? 0) + 1;
            var now = AppClock.Now;

            var entity = new DocSection
            {
                Title = title,
                Slug = slug,
                Description = (request.Description ?? "").Trim(),
                SortOrder = nextOrd,
                CollapsedDefault = request.CollapsedDefault,
                CreatedAt = now,
                UpdatedAt = now,
            };
            _db.DocSections.Add(entity);
            await _db.SaveChangesAsync(ct);

            return ApiResult<DocSectionDto>.Ok(
                new DocSectionDto(entity.Id, entity.Title, entity.Slug, entity.Description ?? "", entity.SortOrder, entity.CollapsedDefault, 0),
                "Section created.");
        }
        catch (Exception ex)
        {
            return ApiResult<DocSectionDto>.Fail("Could not create section: " + ex.Message);
        }
    }

    public async Task<ApiResult<DocSectionDto>> UpdateSectionAsync(int id, UpsertDocSectionRequest request, CancellationToken ct = default)
    {
        await EnsureSeedAsync(ct);
        var title = (request.Title ?? "").Trim();
        if (id <= 0 || title.Length == 0) return ApiResult<DocSectionDto>.Fail("Invalid section.");

        try
        {
            var entity = await _db.DocSections.FirstOrDefaultAsync(s => s.Id == id, ct);
            if (entity == null) return ApiResult<DocSectionDto>.Fail("Section not found.");

            entity.Title = title;
            entity.Slug = await UniqueSectionSlugAsync(
                Slugify(string.IsNullOrWhiteSpace(request.Slug) ? title : request.Slug!),
                id,
                ct);
            entity.Description = (request.Description ?? "").Trim();
            entity.CollapsedDefault = request.CollapsedDefault;
            entity.UpdatedAt = AppClock.Now;
            await _db.SaveChangesAsync(ct);

            var pageCount = await _db.DocPages.CountAsync(p => p.SectionId == id, ct);
            return ApiResult<DocSectionDto>.Ok(
                new DocSectionDto(entity.Id, entity.Title, entity.Slug, entity.Description ?? "", entity.SortOrder, entity.CollapsedDefault, pageCount),
                "Section updated.");
        }
        catch (Exception ex)
        {
            return ApiResult<DocSectionDto>.Fail("Could not update section: " + ex.Message);
        }
    }

    public async Task<ApiResult> DeleteSectionAsync(int id, CancellationToken ct = default)
    {
        await EnsureSeedAsync(ct);
        try
        {
            if (await _db.DocPages.AnyAsync(p => p.SectionId == id, ct))
                return ApiResult.Fail("Move or delete pages in this section first.");

            var entity = await _db.DocSections.FirstOrDefaultAsync(s => s.Id == id, ct);
            if (entity == null) return ApiResult.Fail("Section not found.");

            _db.DocSections.Remove(entity);
            await _db.SaveChangesAsync(ct);
            return ApiResult.Ok("Section deleted.");
        }
        catch (Exception ex)
        {
            return ApiResult.Fail("Could not delete section: " + ex.Message);
        }
    }

    public async Task<ApiResult> ReorderSectionsAsync(ReorderRequest request, CancellationToken ct = default)
    {
        await EnsureSeedAsync(ct);
        if (request.Order == null || request.Order.Count == 0)
            return ApiResult.Fail("Invalid payload");

        try
        {
            var ids = request.Order.Where(x => x > 0).ToList();
            var sections = await _db.DocSections.Where(s => ids.Contains(s.Id)).ToListAsync(ct);
            var pos = 0;
            foreach (var sectionId in ids)
            {
                var section = sections.FirstOrDefault(s => s.Id == sectionId);
                if (section != null)
                {
                    section.SortOrder = pos;
                    section.UpdatedAt = AppClock.Now;
                }
                pos++;
            }

            await _db.SaveChangesAsync(ct);
            return ApiResult.Ok("Order saved.");
        }
        catch (Exception ex)
        {
            return ApiResult.Fail("Reorder failed: " + ex.Message);
        }
    }

    public async Task<ApiResult<DocMediaListDto>> ListMediaAsync(CancellationToken ct = default)
    {
        await EnsureSeedAsync(ct);
        try
        {
            var rows = await _db.DocMedia.AsNoTracking()
                .OrderByDescending(m => m.Id)
                .Take(100)
                .ToListAsync(ct);

            var items = rows.Select(MapMedia).ToList();
            return ApiResult<DocMediaListDto>.Ok(new DocMediaListDto(items));
        }
        catch (Exception ex)
        {
            return ApiResult<DocMediaListDto>.Fail("Unable to load media: " + ex.Message);
        }
    }

    public async Task<ApiResult<DocUploadResultDto>> UploadMediaAsync(
        Stream file,
        string fileName,
        string contentType,
        string? uploadedBy,
        CancellationToken ct = default)
    {
        await EnsureSeedAsync(ct);

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = "jpg",
            ["image/png"] = "png",
            ["image/gif"] = "gif",
            ["image/webp"] = "webp",
        };

        if (!map.TryGetValue(contentType ?? "", out var ext))
            return ApiResult<DocUploadResultDto>.Fail("Only JPG, PNG, GIF, WebP allowed");

        await using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        if (ms.Length > 5 * 1024 * 1024)
            return ApiResult<DocUploadResultDto>.Fail("Max 5MB");
        if (ms.Length == 0)
            return ApiResult<DocUploadResultDto>.Fail("Empty file");

        var dir = ResolveUploadDir();
        if (string.IsNullOrWhiteSpace(dir))
            return ApiResult<DocUploadResultDto>.Fail("Documentation upload path is not configured (App:DocumentationUploadsFsPath).");

        try
        {
            Directory.CreateDirectory(dir);
            var baseName = $"doc-{AppClock.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8]}.{ext}";
            var dest = Path.Combine(dir, baseName);
            await File.WriteAllBytesAsync(dest, ms.ToArray(), ct);

            var rel = "uploads/documentation/" + baseName;
            var by = string.IsNullOrWhiteSpace(uploadedBy) ? "admin" : uploadedBy.Trim();
            var now = AppClock.Now;

            var entity = new DocMedia
            {
                Filename = baseName,
                RelPath = rel,
                Mime = contentType,
                SizeBytes = (int)ms.Length,
                UploadedBy = by,
                CreatedAt = now,
            };
            _db.DocMedia.Add(entity);
            await _db.SaveChangesAsync(ct);

            var media = MapMedia(entity);
            return ApiResult<DocUploadResultDto>.Ok(new DocUploadResultDto(media.Url, media), "Uploaded");
        }
        catch (Exception ex)
        {
            return ApiResult<DocUploadResultDto>.Fail("Upload failed: " + ex.Message);
        }
    }

    private async Task EnsureSeedAsync(CancellationToken ct)
    {
        if (_seedChecked) return;

        if (!await _db.DocSections.AnyAsync(ct))
        {
            var now = AppClock.Now;
            var section = new DocSection
            {
                Title = "Getting Started",
                Slug = "getting-started",
                Description = "Introduction and setup",
                SortOrder = 0,
                CollapsedDefault = false,
                CreatedAt = now,
                UpdatedAt = now,
            };
            _db.DocSections.Add(section);
            await _db.SaveChangesAsync(ct);

            _db.DocPages.Add(new DocPage
            {
                SectionId = section.Id,
                Title = "Welcome",
                Slug = "welcome",
                Status = "published",
                ContentHtml = "<h1>Welcome to the documentation</h1><p>This is your first published page.</p>",
                SortOrder = 0,
                PublishedAt = now,
                CreatedAt = now,
                UpdatedAt = now,
            });
            await _db.SaveChangesAsync(ct);
        }

        _seedChecked = true;
    }

    private async Task<DocPageDetailDto?> LoadPageAsync(int id, CancellationToken ct)
    {
        var row = await (
            from p in _db.DocPages.AsNoTracking()
            join s in _db.DocSections.AsNoTracking() on p.SectionId equals s.Id
            where p.Id == id
            select new { Page = p, SectionTitle = s.Title }).FirstOrDefaultAsync(ct);

        if (row == null) return null;

        var sectionPages = await _db.DocPages.AsNoTracking()
            .Where(p => p.SectionId == row.Page.SectionId)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Id)
            .Select(p => new DocPageOrderItemDto(p.Id, p.Title, p.Slug, p.Status, p.SortOrder))
            .ToListAsync(ct);

        return new DocPageDetailDto(
            row.Page.Id,
            row.Page.SectionId,
            row.SectionTitle ?? "",
            row.Page.Title ?? "",
            row.Page.Slug ?? "",
            row.Page.Status ?? "draft",
            row.Page.ContentHtml ?? "",
            row.Page.MetaTitle ?? "",
            row.Page.MetaDescription ?? "",
            row.Page.MetaKeywords ?? "",
            row.Page.SortOrder,
            row.Page.PublishedAt is { Year: > 1 } ? FormatDateTime(row.Page.PublishedAt) : null,
            FormatDateTime(row.Page.UpdatedAt),
            GrowWithMwPrefix().TrimEnd('/') + "/" + Uri.EscapeDataString(row.Page.Slug ?? ""),
            sectionPages);
    }

    private async Task<IReadOnlyList<DocSectionOptionDto>> ListSectionOptionsAsync(CancellationToken ct)
    {
        return await _db.DocSections.AsNoTracking()
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Id)
            .Select(s => new DocSectionOptionDto(s.Id, s.Title))
            .ToListAsync(ct);
    }

    private async Task<string> UniquePageSlugAsync(string baseSlug, int? excludeId, CancellationToken ct)
    {
        var candidate = string.IsNullOrWhiteSpace(baseSlug) ? "page" : baseSlug;
        var n = 2;
        while (true)
        {
            var exists = excludeId is > 0
                ? await _db.DocPages.AnyAsync(p => p.Slug == candidate && p.Id != excludeId.Value, ct)
                : await _db.DocPages.AnyAsync(p => p.Slug == candidate, ct);
            if (!exists) return candidate;
            candidate = baseSlug + "-" + n;
            n++;
        }
    }

    private async Task<string> UniqueSectionSlugAsync(string baseSlug, int? excludeId, CancellationToken ct)
    {
        var candidate = string.IsNullOrWhiteSpace(baseSlug) ? "section" : baseSlug;
        var n = 2;
        while (true)
        {
            var exists = excludeId is > 0
                ? await _db.DocSections.AnyAsync(s => s.Slug == candidate && s.Id != excludeId.Value, ct)
                : await _db.DocSections.AnyAsync(s => s.Slug == candidate, ct);
            if (!exists) return candidate;
            candidate = baseSlug + "-" + n;
            n++;
        }
    }

    private string ResolveUploadDir()
    {
        if (!string.IsNullOrWhiteSpace(_app.DocumentationUploadsFsPath))
            return _app.DocumentationUploadsFsPath.Trim();

        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "uploads", "documentation")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "uploads", "documentation")),
            @"C:\xampp\htdocs\miniwebsite\uploads\documentation",
        };
        foreach (var c in candidates)
        {
            if (Directory.Exists(c) || Directory.Exists(Path.GetDirectoryName(c)!))
                return c;
        }
        return candidates[0];
    }

    private string PublicDocsPrefix()
    {
        var prefix = _app.DocumentationPublicPrefix;
        if (prefix.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return prefix;
        return CombineUrl(_app.PhpSiteBaseUrl, prefix);
    }

    private string GrowWithMwPrefix()
    {
        var prefix = _app.GrowWithMwPublicPrefix;
        if (prefix.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return prefix;
        return CombineUrl(_app.PhpSiteBaseUrl, prefix);
    }

    private string BuildPublicUrl(string relPath) =>
        CombineUrl(_app.PhpSiteBaseUrl, relPath);

    private static string CombineUrl(string baseUrl, string path)
    {
        return baseUrl.TrimEnd('/') + "/" + path.TrimStart('/');
    }

    private DocMediaItemDto MapMedia(DocMedia r)
    {
        var mime = r.Mime ?? "";
        return new DocMediaItemDto(
            r.Id,
            r.Filename ?? "",
            r.RelPath ?? "",
            BuildPublicUrl(r.RelPath ?? ""),
            mime,
            r.SizeBytes,
            r.UploadedBy,
            FormatDateTime(r.CreatedAt),
            mime.StartsWith("image/", StringComparison.OrdinalIgnoreCase));
    }

    private static string? NormalizePageStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("all", StringComparison.OrdinalIgnoreCase))
            return null;
        var v = value.Trim().ToLowerInvariant();
        return v is "draft" or "published" ? v : null;
    }

    private static string Slugify(string text)
    {
        var s = text.Trim().ToLowerInvariant();
        s = Regex.Replace(s, @"[^a-z0-9]+", "-");
        s = s.Trim('-');
        return string.IsNullOrEmpty(s) ? "section" : s;
    }

    private static string FormatDateTime(DateTime? value)
    {
        if (value is not { } d || d.Year < 2) return "—";
        return d.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }
}
