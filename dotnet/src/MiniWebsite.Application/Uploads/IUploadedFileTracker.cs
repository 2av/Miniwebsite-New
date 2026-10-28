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
    /// Marks the previous current file for the same user, folder, and document as Replaced,
    /// then adds the new row. The caller saves the database.
    /// </summary>
    Task TrackAsync(int userId, NewUploadedFile file, CancellationToken cancellationToken = default);
}
