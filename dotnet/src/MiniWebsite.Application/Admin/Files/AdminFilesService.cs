using Microsoft.EntityFrameworkCore;
using MiniWebsite.Application.Admin.Files.Dtos;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Domain.Entities;

namespace MiniWebsite.Application.Admin.Files;

public class AdminFilesService : IAdminFilesService
{
    private readonly IApplicationDbContext _db;
    private readonly IObjectStorage _storage;

    public AdminFilesService(IApplicationDbContext db, IObjectStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<ApiResult<AdminFilesPageDto>> ListAsync(AdminFilesQuery query, CancellationToken cancellationToken = default)
    {
        query.Page = query.Page < 1 ? 1 : query.Page;
        query.PageSize = query.PageSize is < 1 or > 100 ? 10 : query.PageSize;

        try
        {
            var q = _db.UploadedFiles.AsNoTracking().Include(f => f.User);
            var filtered = ApplyFilters(q, query);
            var total = await filtered.CountAsync(cancellationToken);
            var totalSize = await filtered.SumAsync(f => (long?)f.SizeBytes, cancellationToken) ?? 0;
            var rows = await filtered
                .OrderByDescending(f => f.UploadedAt)
                .ThenByDescending(f => f.Id)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            return ApiResult<AdminFilesPageDto>.Ok(new AdminFilesPageDto(
                rows.Select(Map).ToList(),
                total,
                query.Page,
                query.PageSize,
                totalSize));
        }
        catch (Exception ex)
        {
            return ApiResult<AdminFilesPageDto>.Fail("Unable to load files: " + ex.Message);
        }
    }

    private static IQueryable<UploadedFile> ApplyFilters(IQueryable<UploadedFile> query, AdminFilesQuery filter)
    {
        var search = (filter.Search ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(f =>
                f.OriginalFileName.Contains(search)
                || f.StoredFileName.Contains(search)
                || f.Document.Contains(search)
                || f.User.Email.Contains(search)
                || f.User.FirstName.Contains(search)
                || f.User.LastName.Contains(search)
                || (f.User.FirstName + " " + f.User.LastName).Contains(search));
        }

        var folder = (filter.Folder ?? "").Trim();
        if (folder is UploadedFileFolder.Profile or UploadedFileFolder.Verification)
            query = query.Where(f => f.Folder == folder);

        var status = (filter.Status ?? "").Trim();
        if (status is UploadedFileStatus.Current or UploadedFileStatus.Replaced)
            query = query.Where(f => f.Status == status);

        return query;
    }

    private AdminFileRowDto Map(UploadedFile file) => new(
        file.Id,
        file.UserId,
        file.User.FullName,
        file.User.Email,
        file.Folder,
        file.Document,
        file.OriginalFileName,
        file.StoredFileName,
        file.ContentType,
        file.SizeBytes,
        file.Status,
        file.UploadedAt,
        file.ReplacedAt,
        _storage.PublicUrl(file.ObjectKey));
}
