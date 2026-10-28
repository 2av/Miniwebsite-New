using Microsoft.EntityFrameworkCore;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Domain.Common;
using MiniWebsite.Domain.Entities;

namespace MiniWebsite.Application.Uploads;

public class UploadedFileTracker : IUploadedFileTracker
{
    private readonly IApplicationDbContext _db;

    public UploadedFileTracker(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task TrackAsync(int userId, NewUploadedFile file, CancellationToken cancellationToken = default)
    {
        var now = AppClock.Now;
        var previous = await _db.UploadedFiles
            .Where(f => f.UserId == userId
                && f.Folder == file.Folder
                && f.Document == file.Document
                && f.Status == UploadedFileStatus.Current)
            .ToListAsync(cancellationToken);

        foreach (var old in previous)
        {
            // A new profile photo replaces the object in storage, so the old row is not a stored file.
            if (file.Folder == UploadedFileFolder.Profile)
            {
                _db.UploadedFiles.Remove(old);
                continue;
            }

            old.Status = UploadedFileStatus.Replaced;
            old.ReplacedAt = now;
        }

        _db.UploadedFiles.Add(new UploadedFile
        {
            UserId = userId,
            Folder = file.Folder,
            Document = file.Document,
            OriginalFileName = file.OriginalFileName,
            StoredFileName = file.StoredFileName,
            ObjectKey = file.ObjectKey,
            ContentType = file.ContentType,
            SizeBytes = file.SizeBytes,
            Status = UploadedFileStatus.Current,
            UploadedAt = now,
        });
    }
}
