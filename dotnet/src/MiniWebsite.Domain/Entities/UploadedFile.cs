namespace MiniWebsite.Domain.Entities;

/// <summary>Metadata for a file stored in Cloudflare R2. The file bytes are not stored here.</summary>
public class UploadedFile
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary><c>profile</c> or <c>verification</c>.</summary>
    public string Folder { get; set; } = "";

    /// <summary>Profile photo, PAN, Aadhaar front, Aadhaar back, GST certificate, or Agreement.</summary>
    public string Document { get; set; } = "";

    public string OriginalFileName { get; set; } = "";
    public string StoredFileName { get; set; } = "";

    /// <summary>R2 object key, for example <c>profile/abc.jpg</c>. Not a public URL.</summary>
    public string ObjectKey { get; set; } = "";

    public string ContentType { get; set; } = "";
    public long SizeBytes { get; set; }

    /// <summary><c>Current</c> or <c>Replaced</c>.</summary>
    public string Status { get; set; } = UploadedFileStatus.Current;

    public DateTime UploadedAt { get; set; }
    public DateTime? ReplacedAt { get; set; }
}

public static class UploadedFileStatus
{
    public const string Current = "Current";
    public const string Replaced = "Replaced";
}

public static class UploadedFileFolder
{
    public const string Profile = "profile";
    public const string Verification = "verification";
}

public static class UploadedFileDocument
{
    public const string ProfilePhoto = "Profile photo";
    public const string Pan = "PAN";
    public const string AadhaarFront = "Aadhaar front";
    public const string AadhaarBack = "Aadhaar back";
    public const string GstCertificate = "GST certificate";
    public const string Agreement = "Agreement";
}
