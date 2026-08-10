namespace MiniWebsite.Application.Common.Options;

public class AppOptions
{
    public const string SectionName = "App";
    public string PublicHost { get; set; } = "miniwebsite.in";
    public string CustomerDashboardUrl { get; set; } = "https://miniwebsite.in/user/dashboard";
    public string FranchiseeLoginUrl { get; set; } = "https://miniwebsite.in/login/franchisee.php";
    public string FranchiseAgreementUrl { get; set; } = "https://miniwebsite.in/franchise_agreement.php";

    /// <summary>When true, Swagger UI is available even outside Development (useful for first server test).</summary>
    public bool EnableSwagger { get; set; }

    /// <summary>When true, 500 responses include exception type, message, and stack (server debugging only).</summary>
    public bool ExposeExceptionDetails { get; set; }

    /// <summary>
    /// PHP site origin that serves uploaded website assets (no trailing slash).
    /// Example: https://miniwebsite.in
    /// </summary>
    public string PhpSiteBaseUrl { get; set; } = "https://miniwebsite.in";

    /// <summary>Root folder under PHP site for website uploads.</summary>
    public string WebsiteUploadsPath { get; set; } = "/assets/upload/websites";

    /// <summary>
    /// Absolute filesystem path for documentation image uploads (PHP <c>uploads/documentation</c>).
    /// Example: C:\xampp\htdocs\miniwebsite\uploads\documentation
    /// </summary>
    public string DocumentationUploadsFsPath { get; set; } = "";

    /// <summary>Public URL prefix for docs help center (relative to PhpSiteBaseUrl if starts with /).</summary>
    public string DocumentationPublicPrefix { get; set; } = "/docs/";

    /// <summary>Public URL prefix for Grow with MW user hub.</summary>
    public string GrowWithMwPublicPrefix { get; set; } = "/user/grow-with-mw/";

    /// <summary>
    /// Absolute filesystem path for kit uploads (PHP <c>assets/upload/kits</c>).
    /// Example: C:\xampp\htdocs\miniwebsite\assets\upload\kits
    /// </summary>
    public string KitsUploadsFsPath { get; set; } = "";

    /// <summary>Public URL path for kit files (relative to PhpSiteBaseUrl unless absolute URL).</summary>
    public string KitsUploadsPublicPath { get; set; } = "/assets/upload/kits";

    /// <summary>
    /// Absolute filesystem path for user profile avatars (PHP <c>assets/upload/profile_image</c>).
    /// Example: C:\xampp\htdocs\miniwebsite\assets\upload\profile_image
    /// </summary>
    public string ProfileUploadsFsPath { get; set; } = "";

    /// <summary>Public relative path prefix for profile images, e.g. <c>assets/upload/profile_image</c>.</summary>
    public string ProfileUploadsRelativePath { get; set; } = "assets/upload/profile_image";

    /// <summary>
    /// Absolute filesystem path for franchisee KYC uploads (PHP <c>assets/upload/verification</c>).
    /// </summary>
    public string VerificationUploadsFsPath { get; set; } = "";

    /// <summary>Public relative path prefix, e.g. <c>assets/upload/verification</c>.</summary>
    public string VerificationUploadsRelativePath { get; set; } = "assets/upload/verification";
}
