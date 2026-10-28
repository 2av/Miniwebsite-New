namespace MiniWebsite.Application.Uploads;

public sealed record NewUploadedFile(
    string Folder,
    string Document,
    string OriginalFileName,
    string StoredFileName,
    string ObjectKey,
    string ContentType,
    long SizeBytes);

public interface IUploadedFileTracker
{
    /// <summary>
    /// Adds the new row. A previous profile photo is removed, because that object is deleted from storage.
    /// A previous verification file is marked Replaced and kept. The caller saves the database.
    /// </summary>
    Task TrackAsync(int userId, NewUploadedFile file, CancellationToken cancellationToken = default);
}
