namespace MiniWebsite.Application.Common.Interfaces;

public sealed record StoredObjectContent(byte[] Content, string ContentType);

public interface IObjectStorage
{
    bool IsEnabled { get; }
    string ProfilePrefix { get; }
    string VerificationPrefix { get; }
    string ProductsPrefix { get; }
    string WebsitePrefix { get; }

    string BuildKey(string prefix, string fileName);

    /// <summary>Key shaped as <c>{userCode}/{folder}/{fileName}</c>, for example <c>PSP 0FPWZS6Q/profile/photo.jpg</c>.</summary>
    string BuildUserKey(string userCode, string folder, string fileName);

    bool IsUnder(string? stored, string prefix);

    Task UploadAsync(string objectKey, byte[] content, string contentType, CancellationToken cancellationToken = default);
    Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default);

    /// <summary>Reads an object. Null when storage is off or the object is missing.</summary>
    Task<StoredObjectContent?> DownloadAsync(string objectKey, CancellationToken cancellationToken = default);

    /// <summary>Public Cloudflare URL for an object key. Null when no public address is configured.</summary>
    string? PublicUrl(string objectKey);

    /// <summary>Stable URL for a profile photo. Uses the public R2 address when it is set.</summary>
    string ProfileReadUrl(string objectKey);

    /// <summary>Time-limited URL for a verification document.</summary>
    string VerificationReadUrl(string objectKey);
}
