using MiniWebsite.Domain.Common;

namespace MiniWebsite.Domain.Entities;

public class Customer
{
    public int Id { get; set; }
    public int OwnerUserId { get; set; }
    public User OwnerUser { get; set; } = null!;
    public string OwnerRole { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string? LabelTag { get; set; }
    public string? BusinessType { get; set; }
    public string? ApproachedFor { get; set; }
    public string? FollowupMethod { get; set; }
    public string PhoneNumber { get; set; } = "";
    public string? EmailId { get; set; }
    public string? CompanyName { get; set; }
    public string? Website { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AreaCity { get; set; }
    public string? Comments { get; set; }
    public string Source { get; set; } = "Direct";
    public string Status { get; set; } = "Followup required";
    public DateTime CreatedAt { get; set; } = AppClock.Now;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastSharedAt { get; set; }
    public ICollection<CustomerFollowup> Followups { get; set; } = new List<CustomerFollowup>();
}

public class CustomerFollowup
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public int OwnerUserId { get; set; }
    public User OwnerUser { get; set; } = null!;
    public string OwnerRole { get; set; } = "";
    public DateTime FollowupDatetime { get; set; }
    public string FollowupMethod { get; set; } = "";
    public string FollowupStatus { get; set; } = "";
    public string? Comments { get; set; }
    public DateTime CreatedAt { get; set; } = AppClock.Now;
}

public class ReferralPayment
{
    public int Id { get; set; }
    public int ReferralEarningId { get; set; }
    public ReferralEarning ReferralEarning { get; set; } = null!;
    public decimal Amount { get; set; }
    public string? TransactionNumber { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentNotes { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? ProcessedBy { get; set; }
}

public class ContentPage
{
    public int Id { get; set; }
    public string ContentType { get; set; } = "";
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }
    public DateTime? LastUpdated { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Faq
{
    public int Id { get; set; }
    public string PageType { get; set; } = "";
    public string Question { get; set; } = "";
    public string Answer { get; set; } = "";
    public int SortOrder { get; set; }
    public string Status { get; set; } = "active";
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class DocSection
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool CollapsedDefault { get; set; }
    public DateTime CreatedAt { get; set; } = AppClock.Now;
    public DateTime? UpdatedAt { get; set; }
    public ICollection<DocPage> Pages { get; set; } = new List<DocPage>();
}

public class DocPage
{
    public int Id { get; set; }
    public int SectionId { get; set; }
    public DocSection Section { get; set; } = null!;
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Status { get; set; } = "draft";
    public string? ContentHtml { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }
    public int SortOrder { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; } = AppClock.Now;
    public DateTime? UpdatedAt { get; set; }
}

public class DocMedia
{
    public int Id { get; set; }
    public string Filename { get; set; } = "";
    public string RelPath { get; set; } = "";
    public string? Mime { get; set; }
    public int SizeBytes { get; set; }
    public string? UploadedBy { get; set; }
    public DateTime CreatedAt { get; set; } = AppClock.Now;
}

public class ThemeCatalog
{
    public int Id { get; set; }
    public int ThemeNumber { get; set; }
    public string ThemeName { get; set; } = "";
    public string CssValue { get; set; } = "";
    public string? PreviewImage { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CardView
{
    public int Id { get; set; }
    public string? Ip { get; set; }
    public int? CardId { get; set; }
    public Card? Card { get; set; }
    public DateTime CreatedAt { get; set; } = AppClock.Now;
}

public class EmailLog
{
    public int Id { get; set; }
    public string UserEmail { get; set; } = "";
    public string EmailType { get; set; } = "";
    public string Subject { get; set; } = "";
    public DateTime SentDate { get; set; } = AppClock.Now;
    public string Status { get; set; } = "SENT";
}

public class Notification
{
    public int Id { get; set; }
    public string Audience { get; set; } = "admin"; // admin|customer|franchisee
    public string Type { get; set; } = "";
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string? UserEmail { get; set; }
    public int? UserId { get; set; }
    public User? User { get; set; }
    public string? UserType { get; set; }
    public bool IsRead { get; set; }
    public string Priority { get; set; } = "medium";
    public DateTime CreatedAt { get; set; } = AppClock.Now;
    public DateTime? UpdatedAt { get; set; }
}
