using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MiniWebsite.Application.Admin.EmailTemplates.Dtos;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Domain.Entities;
using MiniWebsite.Domain.Common;

namespace MiniWebsite.Application.Admin.EmailTemplates;

public class AdminEmailTemplatesService : IAdminEmailTemplatesService
{
    private static readonly EmailTemplateCategoryOption[] Categories =
    [
        new("auth", "Auth & Registration"),
        new("welcome", "Welcome"),
        new("franchise", "Franchise"),
        new("team", "Team"),
        new("admin", "Admin / System"),
        new("general", "General"),
    ];

    private readonly IApplicationDbContext _db;

    public AdminEmailTemplatesService(IApplicationDbContext db)
    {
        _db = db;
    }

    public Task<ApiResult<EmailTemplateMetaDto>> GetMetaAsync(CancellationToken ct = default) =>
        Task.FromResult(ApiResult<EmailTemplateMetaDto>.Ok(new EmailTemplateMetaDto(Categories)));

    public async Task<ApiResult<EmailTemplatePageDto>> ListAsync(EmailTemplateQuery query, CancellationToken ct = default)
    {
        query.Page = query.Page < 1 ? 1 : query.Page;
        query.PageSize = query.PageSize is < 1 or > 100 ? 10 : query.PageSize;

        try
        {
            var q = _db.EmailTemplates.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var s = query.Search.Trim();
                q = q.Where(t =>
                    t.Name.Contains(s) ||
                    t.TemplateKey.Contains(s) ||
                    t.Subject.Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(query.Category) &&
                !query.Category.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                var cat = query.Category.Trim();
                q = q.Where(t => t.Category == cat);
            }

            if (!string.IsNullOrWhiteSpace(query.Status) &&
                !query.Status.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                var active = query.Status.Equals("active", StringComparison.OrdinalIgnoreCase);
                q = q.Where(t => t.IsActive == active);
            }

            var total = await q.CountAsync(ct);
            var rows = await q
                .OrderBy(t => t.Category)
                .ThenBy(t => t.Name)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(ct);

            return ApiResult<EmailTemplatePageDto>.Ok(new EmailTemplatePageDto(
                rows.Select(MapRow).ToList(),
                total,
                query.Page,
                query.PageSize));
        }
        catch (Exception ex)
        {
            return ApiResult<EmailTemplatePageDto>.Fail("Unable to load email templates: " + ex.Message);
        }
    }

    public async Task<ApiResult<EmailTemplateRowDto>> GetAsync(int id, CancellationToken ct = default)
    {
        var row = await _db.EmailTemplates.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);
        return row == null
            ? ApiResult<EmailTemplateRowDto>.Fail("Email template not found.")
            : ApiResult<EmailTemplateRowDto>.Ok(MapRow(row));
    }

    public async Task<ApiResult<EmailTemplateRowDto>> GetByKeyAsync(string templateKey, CancellationToken ct = default)
    {
        var key = (templateKey ?? "").Trim();
        if (string.IsNullOrEmpty(key))
            return ApiResult<EmailTemplateRowDto>.Fail("Template key is required.");

        var row = await _db.EmailTemplates.AsNoTracking()
            .FirstOrDefaultAsync(t => t.TemplateKey == key, ct);
        return row == null
            ? ApiResult<EmailTemplateRowDto>.Fail("Email template not found.")
            : ApiResult<EmailTemplateRowDto>.Ok(MapRow(row));
    }

    public async Task<ApiResult<EmailTemplateRowDto>> CreateAsync(UpsertEmailTemplateRequest request, CancellationToken ct = default)
    {
        var validation = Validate(request, requireKey: true);
        if (validation != null) return ApiResult<EmailTemplateRowDto>.Fail(validation);

        var key = NormalizeKey(request.TemplateKey);
        if (await _db.EmailTemplates.AnyAsync(t => t.TemplateKey == key, ct))
            return ApiResult<EmailTemplateRowDto>.Fail("Template key already exists.");

        var entity = new EmailTemplate
        {
            TemplateKey = key,
            Name = request.Name.Trim(),
            Category = NormalizeCategory(request.Category),
            Subject = request.Subject.Trim(),
            BodyHtml = request.BodyHtml.Trim(),
            AvailableVariables = NormalizeVariables(request.AvailableVariables),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            IsActive = !string.Equals(request.Status, "inactive", StringComparison.OrdinalIgnoreCase),
            IsSystem = false,
            CreatedAt = AppClock.Now,
            UpdatedAt = AppClock.Now,
            UpdatedBy = string.IsNullOrWhiteSpace(request.UpdatedBy) ? "admin" : request.UpdatedBy.Trim(),
        };

        _db.EmailTemplates.Add(entity);
        await _db.SaveChangesAsync(ct);
        return ApiResult<EmailTemplateRowDto>.Ok(MapRow(entity), "Email template created.");
    }

    public async Task<ApiResult<EmailTemplateRowDto>> UpdateAsync(int id, UpsertEmailTemplateRequest request, CancellationToken ct = default)
    {
        var validation = Validate(request, requireKey: false);
        if (validation != null) return ApiResult<EmailTemplateRowDto>.Fail(validation);

        var entity = await _db.EmailTemplates.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (entity == null) return ApiResult<EmailTemplateRowDto>.Fail("Email template not found.");

        // System templates keep stable keys.
        if (!entity.IsSystem && !string.IsNullOrWhiteSpace(request.TemplateKey))
        {
            var key = NormalizeKey(request.TemplateKey);
            if (key != entity.TemplateKey &&
                await _db.EmailTemplates.AnyAsync(t => t.TemplateKey == key && t.Id != id, ct))
                return ApiResult<EmailTemplateRowDto>.Fail("Template key already exists.");
            entity.TemplateKey = key;
        }

        entity.Name = request.Name.Trim();
        entity.Category = NormalizeCategory(request.Category);
        entity.Subject = request.Subject.Trim();
        entity.BodyHtml = request.BodyHtml.Trim();
        entity.AvailableVariables = NormalizeVariables(request.AvailableVariables);
        entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        if (!string.IsNullOrWhiteSpace(request.Status))
            entity.IsActive = request.Status.Equals("active", StringComparison.OrdinalIgnoreCase);
        entity.UpdatedAt = AppClock.Now;
        entity.UpdatedBy = string.IsNullOrWhiteSpace(request.UpdatedBy) ? "admin" : request.UpdatedBy.Trim();

        await _db.SaveChangesAsync(ct);
        return ApiResult<EmailTemplateRowDto>.Ok(MapRow(entity), "Email template updated.");
    }

    public async Task<ApiResult> ToggleStatusAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.EmailTemplates.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (entity == null) return ApiResult.Fail("Email template not found.");
        entity.IsActive = !entity.IsActive;
        entity.UpdatedAt = AppClock.Now;
        await _db.SaveChangesAsync(ct);
        return ApiResult.Ok(entity.IsActive ? "Template activated." : "Template deactivated.");
    }

    public async Task<ApiResult> DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.EmailTemplates.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (entity == null) return ApiResult.Fail("Email template not found.");
        if (entity.IsSystem)
            return ApiResult.Fail("System templates cannot be deleted. Deactivate them instead.");

        _db.EmailTemplates.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return ApiResult.Ok("Email template deleted.");
    }

    public Task<ApiResult<PreviewEmailTemplateDto>> PreviewAsync(PreviewEmailTemplateRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.BodyHtml))
            return Task.FromResult(ApiResult<PreviewEmailTemplateDto>.Fail("Subject and body are required."));

        var vars = request.SampleVariables ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var subject = EmailTemplateRenderer.Apply(request.Subject, vars);
        var body = EmailTemplateRenderer.Apply(request.BodyHtml, vars);
        return Task.FromResult(ApiResult<PreviewEmailTemplateDto>.Ok(new PreviewEmailTemplateDto(subject, body)));
    }

    private static string? Validate(UpsertEmailTemplateRequest request, bool requireKey)
    {
        if (requireKey && string.IsNullOrWhiteSpace(request.TemplateKey))
            return "Template key is required.";
        if (string.IsNullOrWhiteSpace(request.Name)) return "Name is required.";
        if (string.IsNullOrWhiteSpace(request.Category)) return "Category is required.";
        if (string.IsNullOrWhiteSpace(request.Subject)) return "Subject is required.";
        if (string.IsNullOrWhiteSpace(request.BodyHtml)) return "Body HTML is required.";
        return null;
    }

    private static string NormalizeKey(string key) =>
        Regex.Replace(key.Trim().ToLowerInvariant(), @"[^a-z0-9_]+", "_").Trim('_');

    private static string NormalizeCategory(string category)
    {
        var c = category.Trim().ToLowerInvariant();
        return Categories.Any(x => x.Value == c) ? c : "general";
    }

    private static string? NormalizeVariables(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var parts = raw.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => p.Trim().Trim('{', '}'))
            .Where(p => p.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase);
        var joined = string.Join(",", parts);
        return string.IsNullOrEmpty(joined) ? null : joined;
    }

    private static EmailTemplateRowDto MapRow(EmailTemplate t)
    {
        var catLabel = Categories.FirstOrDefault(c => c.Value == t.Category)?.Label ?? t.Category;
        var plain = Regex.Replace(WebUtility.HtmlDecode(t.BodyHtml), "<[^>]+>", " ");
        plain = Regex.Replace(plain, @"\s+", " ").Trim();
        if (plain.Length > 140) plain = plain[..140] + "…";

        return new EmailTemplateRowDto(
            t.Id,
            t.TemplateKey,
            t.Name,
            t.Category,
            catLabel,
            t.Subject,
            t.BodyHtml,
            plain,
            t.AvailableVariables,
            t.Description,
            t.IsActive ? "active" : "inactive",
            t.IsActive ? "success" : "muted",
            t.IsSystem,
            t.UpdatedAt,
            t.UpdatedBy);
    }
}
