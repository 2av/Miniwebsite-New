namespace MiniWebsite.Domain.Entities;

public class KitFolder
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Category { get; set; } = "sales";
    public int? ParentId { get; set; }
    public KitFolder? Parent { get; set; }
    public int DisplayOrder { get; set; }
    public string Status { get; set; } = "active";
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public ICollection<KitItem> Items { get; set; } = new List<KitItem>();
}

public class KitItem
{
    public int Id { get; set; }
    public string Type { get; set; } = "file"; // image|video|file
    public string? Title { get; set; }
    public string? FilePath { get; set; }
    public string? VideoUrl { get; set; }
    public int DisplayOrder { get; set; }
    public string Status { get; set; } = "active";
    public string Category { get; set; } = "franchisee";
    public int? FolderId { get; set; }
    public KitFolder? Folder { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ProductCategory
{
    public int Id { get; set; }
    public string? BusinessProfileType { get; set; }
    public string? BusinessHeading { get; set; }
    public string? BusinessCategory { get; set; }
    public string? BusinessCategorySlug { get; set; }
    public string? ProductCategoryName { get; set; }
    public string? ProductCategorySlug { get; set; }
    public int DirectoryPriority { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Keywords { get; set; }
    public string? Tags { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class UserCustomCategory
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string CategoryName { get; set; } = "";
    public string CategoryType { get; set; } = "business-category";
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class UserBankDetails
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string? BankName { get; set; }
    public string? AccountHolderName { get; set; }
    public string? AccountNumber { get; set; }
    public string? AccountType { get; set; }
    public string? IfscCode { get; set; }
    public string? UpiId { get; set; }
    public string? UpiName { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class FranchiseVerification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Status { get; set; } = "pending";
    public string? AdminRemarks { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedBy { get; set; }
    public string? DocumentPath { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CardVideo
{
    public int Id { get; set; }
    public int CardId { get; set; }
    public Card Card { get; set; } = null!;
    public string? Url { get; set; }
    public string? VideoType { get; set; }
    public string? ThumbnailPath { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
