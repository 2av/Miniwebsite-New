using System.Net;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Options;
using MiniWebsite.Domain.Common;

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
        ProductsPrefix = "products";
        WebsitePrefix = "website";

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
    public string ProductsPrefix { get; }
    public string WebsitePrefix { get; }

    public string BuildKey(string prefix, string fileName) =>
        NormalizePrefix(prefix, "uploads") + "/" + FileNameOnly(fileName);

    public string BuildUserKey(string userCode, string folder, string fileName)
    {
        var owner = UserCodeGenerator.FormatDisplay(userCode);
        if (string.IsNullOrWhiteSpace(owner))
            throw new InvalidOperationException("A user code is required for this upload.");
        owner = owner.Replace('\\', '/').Trim().Trim('/');
        if (owner.Contains('/') || owner.Contains("..", StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid user code.");

        return owner + "/" + NormalizePrefix(folder, "uploads") + "/" + FileNameOnly(fileName);
    }

    public bool IsUnder(string? stored, string prefix)
    {
        if (string.IsNullOrWhiteSpace(stored)) return false;
        var path = stored.Trim().Replace('\\', '/').Trim('/');
        var folder = NormalizePrefix(prefix, "");
        if (folder.Length == 0) return false;
        if (path.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase))
            return true;

        var slash = path.IndexOf('/');
        if (slash <= 0) return false;
        return path.AsSpan(slash).StartsWith(("/" + folder + "/").AsSpan(), StringComparison.OrdinalIgnoreCase);
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

    public async Task<StoredObjectContent?> DownloadAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        if (_client == null || string.IsNullOrWhiteSpace(objectKey)) return null;
        try
        {
            using var response = await _client.GetObjectAsync(_options.Bucket.Trim(), objectKey.Trim(), cancellationToken);
            await using var body = response.ResponseStream;
            using var buffer = new MemoryStream();
            await body.CopyToAsync(buffer, cancellationToken);
            var type = response.Headers.ContentType;
            if (string.IsNullOrWhiteSpace(type)) type = "application/octet-stream";
            return new StoredObjectContent(buffer.ToArray(), type.Split(';')[0].Trim());
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public string? PublicUrl(string objectKey)
    {
        var publicBase = (_options.PublicBaseUrl ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(publicBase) || string.IsNullOrWhiteSpace(objectKey))
            return null;
        var encoded = string.Join("/",
            objectKey.Trim().Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.EscapeDataString));
        return publicBase + "/" + encoded;
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

    private static string FileNameOnly(string fileName)
    {
        var name = Path.GetFileName((fileName ?? "").Replace('\\', '/').Trim());
        if (string.IsNullOrWhiteSpace(name) || name is "." or "..")
            throw new InvalidOperationException("Invalid file name.");
        return name;
    }

    private static string NormalizePrefix(string? prefix, string fallback)
    {
        var value = string.IsNullOrWhiteSpace(prefix) ? fallback : prefix.Trim();
        return value.Trim('/').Replace('\\', '/');
    }
}
