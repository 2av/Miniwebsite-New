using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Options;

namespace MiniWebsite.Infrastructure.Storage;

public sealed class R2ObjectStorage : IObjectStorage, IDisposable
{
    private readonly R2Options _options;
    private readonly IAmazonS3? _client;

    public R2ObjectStorage(IOptions<R2Options> options)
    {
        _options = options.Value;
        ProfilePrefix = NormalizePrefix(_options.ProfilePrefix, "profile");
        VerificationPrefix = NormalizePrefix(_options.VerificationPrefix, "verification");

        if (!IsEnabled) return;

        var config = new AmazonS3Config
        {
            ServiceURL = _options.ServiceUrl.Trim().TrimEnd('/'),
            ForcePathStyle = true,
            AuthenticationRegion = "auto",
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
        };
        _client = new AmazonS3Client(_options.AccessKeyId.Trim(), _options.SecretAccessKey.Trim(), config);
    }

    public bool IsEnabled =>
        !string.IsNullOrWhiteSpace(_options.ServiceUrl)
        && !string.IsNullOrWhiteSpace(_options.Bucket)
        && !string.IsNullOrWhiteSpace(_options.AccessKeyId)
        && !string.IsNullOrWhiteSpace(_options.SecretAccessKey);

    public string ProfilePrefix { get; }
    public string VerificationPrefix { get; }

    public string BuildKey(string prefix, string fileName)
    {
        var name = Path.GetFileName((fileName ?? "").Replace('\\', '/').Trim());
        if (string.IsNullOrWhiteSpace(name) || name is "." or "..")
            throw new InvalidOperationException("Invalid file name.");
        return NormalizePrefix(prefix, "uploads") + "/" + name;
    }

    public bool IsUnder(string? stored, string prefix)
    {
        if (string.IsNullOrWhiteSpace(stored)) return false;
        var path = stored.Trim().Replace('\\', '/');
        var folder = NormalizePrefix(prefix, "");
        return folder.Length > 0
            && path.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase);
    }

    public async Task UploadAsync(string objectKey, byte[] content, string contentType, CancellationToken cancellationToken = default)
    {
        if (_client == null)
            throw new InvalidOperationException("R2 storage is not configured.");

        using var stream = new MemoryStream(content, writable: false);
        var request = new PutObjectRequest
        {
            BucketName = _options.Bucket.Trim(),
            Key = objectKey,
            InputStream = stream,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            DisablePayloadSigning = true,
            DisableDefaultChecksumValidation = true,
            AutoCloseStream = false,
        };
        await _client.PutObjectAsync(request, cancellationToken);
    }

    public async Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        if (_client == null || string.IsNullOrWhiteSpace(objectKey)) return;
        await _client.DeleteObjectAsync(_options.Bucket.Trim(), objectKey, cancellationToken);
    }

    public string? PublicUrl(string objectKey)
    {
        var publicBase = (_options.PublicBaseUrl ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(publicBase) || string.IsNullOrWhiteSpace(objectKey))
            return null;
        return publicBase + "/" + objectKey.TrimStart('/');
    }

    public string ProfileReadUrl(string objectKey) =>
        PublicUrl(objectKey) ?? PresignedReadUrl(objectKey, TimeSpan.FromDays(7));

    public string VerificationReadUrl(string objectKey) =>
        PresignedReadUrl(objectKey, TimeSpan.FromHours(12));

    public void Dispose() => _client?.Dispose();

    private string PresignedReadUrl(string objectKey, TimeSpan lifetime)
    {
        if (_client == null)
            throw new InvalidOperationException("R2 storage is not configured.");

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.Bucket.Trim(),
            Key = objectKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(lifetime),
            Protocol = Protocol.HTTPS,
        };
        return _client.GetPreSignedURL(request);
    }

    private static string NormalizePrefix(string? prefix, string fallback)
    {
        var value = string.IsNullOrWhiteSpace(prefix) ? fallback : prefix.Trim();
        return value.Trim('/').Replace('\\', '/');
    }
}
