namespace MiniWebsite.Application.Common.Options;

public class AppOptions
{
    public const string SectionName = "App";
    public string PublicHost { get; set; } = "miniwebsite.in";

    /// <summary>
    /// Public user-web site origin (no trailing slash). Serves the portal pages and the uploaded files under
    /// <c>/assets/upload/…</c>.
    /// </summary>
    public string SiteBaseUrl { get; set; } = "https://sharebizz.com";

    public string CustomerDashboardUrl => SitePage("dashboard");
    public string FranchiseeLoginUrl => SitePage("login");
    public string FranchiseAgreementUrl => SitePage("franchise-agreement");

    /// <summary>When true, Swagger UI is available even outside Development (useful for first server test).</summary>
    public bool EnableSwagger { get; set; }

    /// <summary>When true, 500 responses include exception type, message, and stack (server debugging only).</summary>
    public bool ExposeExceptionDetails { get; set; }

    /// <summary>Folder under <see cref="SiteBaseUrl"/> for website uploads.</summary>
    public string WebsiteUploadsPath { get; set; } = "/assets/upload/websites";

    /// <summary>
    /// Absolute filesystem path for documentation image uploads (<c>uploads/documentation</c> on the site).
    /// </summary>
    public string DocumentationUploadsFsPath { get; set; } = "";

    /// <summary>Public URL prefix for docs help center (relative to <see cref="SiteBaseUrl"/> if starts with /).</summary>
    public string DocumentationPublicPrefix { get; set; } = "/docs/";

    /// <summary>Public URL prefix for Grow with MW user hub.</summary>
    public string GrowWithMwPublicPrefix { get; set; } = "/user/grow-with-mw/";

    /// <summary>
    /// Absolute filesystem path for kit uploads (user-web <c>public/assets/upload/kits</c>).
    /// </summary>
    public string KitsUploadsFsPath { get; set; } = "";

    /// <summary>Public URL path for kit files (relative to <see cref="SiteBaseUrl"/> unless absolute URL).</summary>
    public string KitsUploadsPublicPath { get; set; } = "/assets/upload/kits";

    /// <summary>
    /// Absolute filesystem path for user profile avatars (user-web <c>public/assets/upload/profile_image</c>).
    /// </summary>
    public string ProfileUploadsFsPath { get; set; } = "";

    /// <summary>Public relative path prefix for profile images, e.g. <c>assets/upload/profile_image</c>.</summary>
    public string ProfileUploadsRelativePath { get; set; } = "assets/upload/profile_image";

    /// <summary>
    /// Absolute filesystem path for franchisee KYC uploads (user-web <c>public/assets/upload/verification</c>).
    /// </summary>
    public string VerificationUploadsFsPath { get; set; } = "";

    /// <summary>Public relative path prefix, e.g. <c>assets/upload/verification</c>.</summary>
    public string VerificationUploadsRelativePath { get; set; } = "assets/upload/verification";

    private string SitePage(string path) => $"{SiteBaseUrl.TrimEnd('/')}/{path}";
}
