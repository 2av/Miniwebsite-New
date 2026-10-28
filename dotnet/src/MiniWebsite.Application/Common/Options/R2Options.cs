namespace MiniWebsite.Application.Common.Options;

/// <summary>
/// Cloudflare R2 (S3-compatible) settings for the sharebizz-uploads bucket.
/// Access keys belong in appsettings.*.local.json, which is not published.
/// </summary>
public class R2Options
{
    public const string SectionName = "R2";

    /// <summary>S3 API endpoint, without the bucket name.</summary>
    public string ServiceUrl { get; set; } = "";

    public string Bucket { get; set; } = "";

    public string AccessKeyId { get; set; } = "";

    public string SecretAccessKey { get; set; } = "";

    /// <summary>
    /// Browser URL for public files (r2.dev subdomain or a custom domain).
    /// This is not the S3 API URL.
    /// </summary>
    public string PublicBaseUrl { get; set; } = "";

    /// <summary>Object prefix for profile photos. Must match the bucket folder name.</summary>
    public string ProfilePrefix { get; set; } = "profile";

    /// <summary>Object prefix for KYC documents. Must match the bucket folder name.</summary>
    public string VerificationPrefix { get; set; } = "verification";
}
