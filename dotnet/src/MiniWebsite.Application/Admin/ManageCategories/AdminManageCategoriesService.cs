using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MiniWebsite.Application.Admin.ManageCategories.Dtos;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Domain.Entities;

namespace MiniWebsite.Application.Admin.ManageCategories;

public class AdminManageCategoriesService : IAdminManageCategoriesService
{
    private static readonly string[] CsvHeaders =
    [
        "Business Profile Type",
        "Business Heading",
        "Business Category",
        "Business Category Slug",
        "Product Category",
        "Product Category Slug",
        "Directory Priority",
        "Is Active",
        "Keywords",
        "Tags",
    ];

    private static readonly string[] RequiredHeaderKeys =
    [
        "business profile type",
        "business heading",
        "business category",
        "business category slug",
        "product category",
        "product category slug",
        "directory priority",
        "is active",
        "keywords",
        "tags",
    ];

    private readonly IApplicationDbContext _db;

    public AdminManageCategoriesService(IApplicationDbContext db)
    {
        _db = db;
    }

    public ManageCategoriesMetaDto GetMeta() => new(CsvHeaders);

    public async Task<ApiResult<ManageCategoriesPageDto>> ListAsync(ManageCategoriesQuery query, CancellationToken ct = default)
    {
        query.Page = query.Page < 1 ? 1 : query.Page;
        query.PageSize = query.PageSize is < 1 or > 100 ? 10 : query.PageSize;

        try
        {
            var q = ApplyFilters(_db.ProductCategories.AsNoTracking(), query);
            var total = await q.CountAsync(ct);
            var rows = await q
                .OrderBy(c => c.DirectoryPriority)
                .ThenBy(c => c.BusinessHeading)
                .ThenBy(c => c.BusinessCategory)
                .ThenBy(c => c.ProductCategoryName)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(ct);

            return ApiResult<ManageCategoriesPageDto>.Ok(new ManageCategoriesPageDto(
                rows.Select(MapEntity).ToList(),
                total,
                query.Page,
                query.PageSize));
        }
        catch (Exception ex)
        {
            return ApiResult<ManageCategoriesPageDto>.Fail("Unable to load categories: " + ex.Message);
        }
    }

    public async Task<ApiResult<ManageCategoryRowDto>> GetAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var row = await _db.ProductCategories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
            if (row == null) return ApiResult<ManageCategoryRowDto>.Fail("Category not found");
            return ApiResult<ManageCategoryRowDto>.Ok(MapEntity(row));
        }
        catch (Exception ex)
        {
            return ApiResult<ManageCategoryRowDto>.Fail("Unable to load category: " + ex.Message);
        }
    }

    public async Task<ApiResult<ManageCategoryRowDto>> CreateAsync(UpsertCategoryRequest request, CancellationToken ct = default)
    {
        var validation = Validate(request);
        if (validation != null) return ApiResult<ManageCategoryRowDto>.Fail(validation);

        var data = Normalize(request);
        var now = DateTime.UtcNow;

        try
        {
            if (await _db.ProductCategories.AnyAsync(
                    c => c.BusinessCategorySlug == data.BusinessCategorySlug
                         && c.ProductCategorySlug == data.ProductCategorySlug, ct))
                return ApiResult<ManageCategoryRowDto>.Fail("Category with this business/product slug already exists");

            var entity = new ProductCategory
            {
                BusinessProfileType = data.BusinessProfileType,
                BusinessHeading = data.BusinessHeading,
                BusinessCategory = data.BusinessCategory,
                BusinessCategorySlug = data.BusinessCategorySlug,
                ProductCategoryName = data.ProductCategory,
                ProductCategorySlug = data.ProductCategorySlug,
                DirectoryPriority = data.DirectoryPriority,
                IsActive = data.IsActive,
                Keywords = data.Keywords,
                Tags = data.Tags,
                CreatedBy = data.CreatedBy ?? "admin",
                CreatedAt = now,
                UpdatedAt = now,
            };
            _db.ProductCategories.Add(entity);
            await _db.SaveChangesAsync(ct);

            return ApiResult<ManageCategoryRowDto>.Ok(MapEntity(entity), "Category added successfully!");
        }
        catch (Exception ex)
        {
            return ApiResult<ManageCategoryRowDto>.Fail("Error adding category: " + ex.Message);
        }
    }

    public async Task<ApiResult<ManageCategoryRowDto>> UpdateAsync(int id, UpsertCategoryRequest request, CancellationToken ct = default)
    {
        var validation = Validate(request);
        if (validation != null) return ApiResult<ManageCategoryRowDto>.Fail(validation);

        var data = Normalize(request);

        try
        {
            var entity = await _db.ProductCategories.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (entity == null) return ApiResult<ManageCategoryRowDto>.Fail("Category not found");

            if (await _db.ProductCategories.AnyAsync(
                    c => c.BusinessCategorySlug == data.BusinessCategorySlug
                         && c.ProductCategorySlug == data.ProductCategorySlug
                         && c.Id != id, ct))
                return ApiResult<ManageCategoryRowDto>.Fail("Category with this business/product slug already exists");

            entity.BusinessProfileType = data.BusinessProfileType;
            entity.BusinessHeading = data.BusinessHeading;
            entity.BusinessCategory = data.BusinessCategory;
            entity.BusinessCategorySlug = data.BusinessCategorySlug;
            entity.ProductCategoryName = data.ProductCategory;
            entity.ProductCategorySlug = data.ProductCategorySlug;
            entity.DirectoryPriority = data.DirectoryPriority;
            entity.IsActive = data.IsActive;
            entity.Keywords = data.Keywords;
            entity.Tags = data.Tags;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return ApiResult<ManageCategoryRowDto>.Ok(MapEntity(entity), "Category updated successfully!");
        }
        catch (Exception ex)
        {
            return ApiResult<ManageCategoryRowDto>.Fail("Error updating category: " + ex.Message);
        }
    }

    public async Task<ApiResult<ManageCategoryRowDto>> ToggleActiveAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var entity = await _db.ProductCategories.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (entity == null) return ApiResult<ManageCategoryRowDto>.Fail("Category not found");

            entity.IsActive = !entity.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return ApiResult<ManageCategoryRowDto>.Ok(MapEntity(entity), "Status updated successfully!");
        }
        catch (Exception ex)
        {
            return ApiResult<ManageCategoryRowDto>.Fail("Error updating status: " + ex.Message);
        }
    }

    public async Task<ApiResult> DeleteAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var entity = await _db.ProductCategories.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (entity == null) return ApiResult.Fail("Category not found");

            _db.ProductCategories.Remove(entity);
            await _db.SaveChangesAsync(ct);
            return ApiResult.Ok("Row deleted successfully!");
        }
        catch (Exception ex)
        {
            return ApiResult.Fail("Error deleting row: " + ex.Message);
        }
    }

    public async Task<(byte[] Content, string FileName)> ExportCsvAsync(CancellationToken ct = default)
    {
        var rows = await _db.ProductCategories.AsNoTracking()
            .OrderBy(c => c.DirectoryPriority)
            .ThenBy(c => c.BusinessHeading)
            .ThenBy(c => c.BusinessCategory)
            .ThenBy(c => c.ProductCategoryName)
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.Append('\uFEFF');
        sb.AppendLine(string.Join(',', CsvHeaders.Select(EscapeCsv)));

        foreach (var r in rows)
        {
            sb.AppendLine(string.Join(',', new[]
            {
                EscapeCsv(r.BusinessProfileType),
                EscapeCsv(r.BusinessHeading),
                EscapeCsv(r.BusinessCategory),
                EscapeCsv(r.BusinessCategorySlug),
                EscapeCsv(r.ProductCategoryName),
                EscapeCsv(r.ProductCategorySlug),
                EscapeCsv(r.DirectoryPriority.ToString(CultureInfo.InvariantCulture)),
                EscapeCsv(r.IsActive ? "1" : "0"),
                EscapeCsv(r.Keywords),
                EscapeCsv(r.Tags),
            }));
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"categories_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv";
        return (bytes, fileName);
    }

    public async Task<ApiResult<CategoryImportResultDto>> ImportCsvAsync(
        Stream csvStream,
        bool replaceAll,
        bool skipDuplicates,
        string? createdBy,
        CancellationToken ct = default)
    {
        try
        {
            using var reader = new StreamReader(csvStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var headerLine = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(headerLine))
                return ApiResult<CategoryImportResultDto>.Fail("CSV file is empty");

            var headers = ParseCsvLine(headerLine).Select(h => h.Trim().ToLowerInvariant()).ToList();
            var missing = RequiredHeaderKeys.Where(k => !headers.Contains(k)).ToList();
            if (missing.Count > 0)
                return ApiResult<CategoryImportResultDto>.Fail(
                    "Invalid CSV format. Required columns: " + string.Join(", ", CsvHeaders));

            if (replaceAll)
            {
                var all = await _db.ProductCategories.ToListAsync(ct);
                _db.ProductCategories.RemoveRange(all);
                await _db.SaveChangesAsync(ct);
            }

            var byEmail = string.IsNullOrWhiteSpace(createdBy) ? "admin" : createdBy.Trim();
            var errors = new List<string>();
            var imported = 0;
            var skipped = 0;
            var lineNo = 1;
            var now = DateTime.UtcNow;

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync(ct);
                lineNo++;
                if (string.IsNullOrWhiteSpace(line)) continue;

                var cols = ParseCsvLine(line);
                if (cols.Count < headers.Count)
                {
                    errors.Add($"Line {lineNo}: incomplete row");
                    skipped++;
                    continue;
                }

                string Get(string key)
                {
                    var idx = headers.IndexOf(key);
                    return idx >= 0 && idx < cols.Count ? cols[idx].Trim() : "";
                }

                var businessCategory = Get("business category");
                var productCategory = Get("product category");
                if (string.IsNullOrWhiteSpace(businessCategory) || string.IsNullOrWhiteSpace(productCategory))
                {
                    errors.Add($"Line {lineNo}: business/product category required");
                    skipped++;
                    continue;
                }

                var businessSlug = Get("business category slug");
                if (string.IsNullOrWhiteSpace(businessSlug)) businessSlug = Slugify(businessCategory);
                var productSlug = Get("product category slug");
                if (string.IsNullOrWhiteSpace(productSlug)) productSlug = Slugify(productCategory);

                int.TryParse(Get("directory priority"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var priority);
                var isActive = ParseIsActive(Get("is active"));

                try
                {
                    var existing = await _db.ProductCategories
                        .FirstOrDefaultAsync(
                            c => c.BusinessCategorySlug == businessSlug && c.ProductCategorySlug == productSlug,
                            ct);

                    if (existing != null)
                    {
                        if (skipDuplicates)
                        {
                            skipped++;
                            continue;
                        }

                        existing.BusinessProfileType = Get("business profile type");
                        existing.BusinessHeading = Get("business heading");
                        existing.BusinessCategory = businessCategory;
                        existing.ProductCategoryName = productCategory;
                        existing.DirectoryPriority = priority;
                        existing.IsActive = isActive != 0;
                        existing.Keywords = Get("keywords");
                        existing.Tags = Get("tags");
                        existing.UpdatedAt = now;
                    }
                    else
                    {
                        _db.ProductCategories.Add(new ProductCategory
                        {
                            BusinessProfileType = Get("business profile type"),
                            BusinessHeading = Get("business heading"),
                            BusinessCategory = businessCategory,
                            BusinessCategorySlug = businessSlug,
                            ProductCategoryName = productCategory,
                            ProductCategorySlug = productSlug,
                            DirectoryPriority = priority,
                            IsActive = isActive != 0,
                            Keywords = Get("keywords"),
                            Tags = Get("tags"),
                            CreatedBy = byEmail,
                            CreatedAt = now,
                            UpdatedAt = now,
                        });
                    }

                    await _db.SaveChangesAsync(ct);
                    imported++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Line {lineNo}: {ex.Message}");
                    skipped++;
                }
            }

            return ApiResult<CategoryImportResultDto>.Ok(
                new CategoryImportResultDto(imported, skipped, errors.Take(20).ToList()),
                $"Import finished. Imported: {imported}, Skipped: {skipped}");
        }
        catch (Exception ex)
        {
            return ApiResult<CategoryImportResultDto>.Fail("Import failed: " + ex.Message);
        }
    }

    private static IQueryable<ProductCategory> ApplyFilters(IQueryable<ProductCategory> q, ManageCategoriesQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLower();
            q = q.Where(c =>
                (c.BusinessProfileType != null && c.BusinessProfileType.ToLower().Contains(s))
                || (c.BusinessHeading != null && c.BusinessHeading.ToLower().Contains(s))
                || (c.BusinessCategory != null && c.BusinessCategory.ToLower().Contains(s))
                || (c.BusinessCategorySlug != null && c.BusinessCategorySlug.ToLower().Contains(s))
                || (c.ProductCategoryName != null && c.ProductCategoryName.ToLower().Contains(s))
                || (c.ProductCategorySlug != null && c.ProductCategorySlug.ToLower().Contains(s))
                || (c.Keywords != null && c.Keywords.ToLower().Contains(s))
                || (c.Tags != null && c.Tags.ToLower().Contains(s)));
        }

        var active = NormalizeActive(query.Active);
        if (active.HasValue)
            q = q.Where(c => c.IsActive == (active.Value != 0));

        return q;
    }

    private static string? Validate(UpsertCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BusinessCategory)) return "Business category is required";
        if (string.IsNullOrWhiteSpace(request.ProductCategory)) return "Product category is required";
        return null;
    }

    private static NormalizedCategory Normalize(UpsertCategoryRequest request)
    {
        var businessCategory = request.BusinessCategory.Trim();
        var productCategory = request.ProductCategory.Trim();
        var businessSlug = string.IsNullOrWhiteSpace(request.BusinessCategorySlug)
            ? Slugify(businessCategory)
            : Slugify(request.BusinessCategorySlug);
        var productSlug = string.IsNullOrWhiteSpace(request.ProductCategorySlug)
            ? Slugify(productCategory)
            : Slugify(request.ProductCategorySlug);

        return new NormalizedCategory(
            (request.BusinessProfileType ?? "").Trim(),
            (request.BusinessHeading ?? "").Trim(),
            businessCategory,
            businessSlug,
            productCategory,
            productSlug,
            request.DirectoryPriority,
            request.IsActive,
            (request.Keywords ?? "").Trim(),
            (request.Tags ?? "").Trim(),
            string.IsNullOrWhiteSpace(request.CreatedBy) ? "admin" : request.CreatedBy.Trim());
    }

    private static int? NormalizeActive(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("all", StringComparison.OrdinalIgnoreCase))
            return null;
        var v = value.Trim().ToLowerInvariant();
        if (v is "1" or "yes" or "true" or "active") return 1;
        if (v is "0" or "no" or "false" or "inactive") return 0;
        return null;
    }

    private static int ParseIsActive(string value)
    {
        var v = value.Trim().ToLowerInvariant();
        if (v is "0" or "no" or "n" or "false" or "inactive") return 0;
        return 1;
    }

    private static string Slugify(string text)
    {
        var s = text.Trim().ToLowerInvariant();
        s = Regex.Replace(s, @"[^a-z0-9]+", "-");
        s = s.Trim('-');
        return string.IsNullOrEmpty(s) ? "category" : s;
    }

    private static string EscapeCsv(string? value)
    {
        var v = value ?? "";
        if (v.Contains('"') || v.Contains(',') || v.Contains('\n') || v.Contains('\r'))
            return "\"" + v.Replace("\"", "\"\"") + "\"";
        return v;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    sb.Append(ch);
                }
            }
            else if (ch == '"')
            {
                inQuotes = true;
            }
            else if (ch == ',')
            {
                result.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(ch);
            }
        }

        result.Add(sb.ToString());
        return result;
    }

    private static ManageCategoryRowDto MapEntity(ProductCategory r)
    {
        var active = r.IsActive;
        return new ManageCategoryRowDto(
            r.Id,
            r.BusinessProfileType ?? "",
            r.BusinessHeading ?? "",
            r.BusinessCategory ?? "",
            r.BusinessCategorySlug ?? "",
            r.ProductCategoryName ?? "",
            r.ProductCategorySlug ?? "",
            r.DirectoryPriority,
            active,
            active ? "Yes" : "No",
            active ? "ok" : "danger",
            r.Keywords ?? "",
            r.Tags ?? "");
    }

    private sealed record NormalizedCategory(
        string BusinessProfileType,
        string BusinessHeading,
        string BusinessCategory,
        string BusinessCategorySlug,
        string ProductCategory,
        string ProductCategorySlug,
        int DirectoryPriority,
        bool IsActive,
        string Keywords,
        string Tags,
        string CreatedBy);
}
