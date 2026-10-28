namespace MiniWebsite.Application.Common.Interfaces;

public interface IObjectStorage
{
    bool IsEnabled { get; }
    string ProfilePrefix { get; }
    string VerificationPrefix { get; }

    string BuildKey(string prefix, string fileName);
    bool IsUnder(string? stored, string prefix);

    Task UploadAsync(string objectKey, byte[] content, string contentType, CancellationToken cancellationToken = default);
    Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default);

    /// <summary>Public Cloudflare URL for an object key. Null when no public address is configured.</summary>
    string? PublicUrl(string objectKey);

    /// <summary>Stable URL for a profile photo. Uses the public R2 address when it is set.</summary>
    string ProfileReadUrl(string objectKey);

    /// <summary>Time-limited URL for a verification document.</summary>
    string VerificationReadUrl(string objectKey);
}
