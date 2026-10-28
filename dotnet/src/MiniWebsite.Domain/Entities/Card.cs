using System.ComponentModel.DataAnnotations.Schema;

namespace MiniWebsite.Domain.Entities;

/// <summary>Mini Website / public card (greenfield <c>cards</c> table).</summary>
public class Card
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public User? User { get; set; }
    public string? Ip { get; set; }
    public int? FranchiseeUserId { get; set; }
    public User? FranchiseeUser { get; set; }
    public string? FranchiseeEmail { get; set; }
    public string? UserEmail { get; set; }
    public string? Slug { get; set; }
    /// <summary>Public profile id shown as <c>DB 4E5T8Y9C</c>.</summary>
    public string? DbCode { get; set; }
    public string? Password { get; set; }
    public string? ThemeCss { get; set; }
    public string? MobileCss { get; set; }
    public string? CompanyName { get; set; }
    public string? DisplayName { get; set; }
    public string? Title { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Position { get; set; }
    public string? Contact { get; set; }
    public string? Contact2 { get; set; }
    public string? Whatsapp { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? Location { get; set; }
    public string? Facebook { get; set; }
    public string? Twitter { get; set; }
    public string? Instagram { get; set; }
    public string? LinkedIn { get; set; }
    public string? Youtube { get; set; }
    public string? Pinterest { get; set; }
    public string? Website2 { get; set; }
    public string? CompanyEstDate { get; set; }
    public string? NatureOfBusiness { get; set; }
    public string? Speciality { get; set; }
    public string? AboutUs { get; set; }
    public string? CardStatus { get; set; }
    public string? LogoPath { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string CollaborationEnabled { get; set; } = "NO";
    public string ComplimentaryEnabled { get; set; } = "No";
    public DateTime? ValidityDate { get; set; }
    public int NameChangeCount { get; set; }
    public int DisplayNameChangeCount { get; set; }
    public string? HeroImagePath { get; set; }
    public string PositionPrimary { get; set; } = "";
    public string PositionSecondary { get; set; } = "";
    public string? BusinessHours { get; set; }
    public string BusinessType { get; set; } = "";
    public string BusinessOperationArea { get; set; } = "";
    public string? BusinessOperationLocations { get; set; }
    public string BusinessProfileType { get; set; } = "";

    public ICollection<CardVideo> Videos { get; set; } = new List<CardVideo>();
    public ICollection<CardProduct> Products { get; set; } = new List<CardProduct>();
    public ICollection<CardService> Services { get; set; } = new List<CardService>();
    public ICollection<CardGalleryImage> GalleryImages { get; set; } = new List<CardGalleryImage>();
    public ICollection<CardOffer> Offers { get; set; } = new List<CardOffer>();
    public CardSlugHistory? SlugHistory { get; set; }

    // --- Legacy DigiCard property aliases (NotMapped) for Application service parity ---
    [NotMapped] public string? FUserEmail { get => FranchiseeEmail; set => FranchiseeEmail = value; }
    [NotMapped] public string? CardId { get => Slug; set => Slug = value; }
    [NotMapped] public string? DCss { get => ThemeCss; set => ThemeCss = value; }
    [NotMapped] public string? DMobileCss { get => MobileCss; set => MobileCss = value; }
    [NotMapped] public string? DCompName { get => CompanyName; set => CompanyName = value; }
    [NotMapped] public string? DDisplayName { get => DisplayName; set => DisplayName = value; }
    [NotMapped] public string? DTitle { get => Title; set => Title = value; }
    [NotMapped] public string? DFName { get => FirstName; set => FirstName = value; }
    [NotMapped] public string? DLName { get => LastName; set => LastName = value; }
    [NotMapped] public string? DPosition { get => Position; set => Position = value; }
    [NotMapped] public string? DContact { get => Contact; set => Contact = value; }
    [NotMapped] public string? DContact2 { get => Contact2; set => Contact2 = value; }
    [NotMapped] public string? DWhatsapp { get => Whatsapp; set => Whatsapp = value; }
    [NotMapped] public string? DEmail { get => Email; set => Email = value; }
    [NotMapped] public string? DWebsite { get => Website; set => Website = value; }
    [NotMapped] public string? DLocation { get => Location; set => Location = value; }
    [NotMapped] public string? DFb { get => Facebook; set => Facebook = value; }
    [NotMapped] public string? DTwitter { get => Twitter; set => Twitter = value; }
    [NotMapped] public string? DInstagram { get => Instagram; set => Instagram = value; }
    [NotMapped] public string? DLinkedin { get => LinkedIn; set => LinkedIn = value; }
    [NotMapped] public string? DYoutube { get => Youtube; set => Youtube = value; }
    [NotMapped] public string? DPinterest { get => Pinterest; set => Pinterest = value; }
    [NotMapped] public string? DWebsite2 { get => Website2; set => Website2 = value; }
    [NotMapped] public string? DCompEstDate { get => CompanyEstDate; set => CompanyEstDate = value; }
    [NotMapped] public string? DNatureOfBusiness { get => NatureOfBusiness; set => NatureOfBusiness = value; }
    [NotMapped] public string? DSpeciality { get => Speciality; set => Speciality = value; }
    [NotMapped] public string? DAboutUs { get => AboutUs; set => AboutUs = value; }
    [NotMapped] public string? DCardStatus { get => CardStatus; set => CardStatus = value; }
    [NotMapped] public string? DLogoLocation { get => LogoPath; set => LogoPath = value; }
    [NotMapped] public DateTime? UploadedDate { get => CreatedAt; set => CreatedAt = value; }
    [NotMapped] public int DNameChangeCount { get => DisplayNameChangeCount; set => DisplayNameChangeCount = value; }
    [NotMapped] public string? DHeroImageLocation { get => HeroImagePath; set => HeroImagePath = value; }
    [NotMapped] public string DPositionPrimary { get => PositionPrimary; set => PositionPrimary = value ?? ""; }
    [NotMapped] public string DPositionSecondary { get => PositionSecondary; set => PositionSecondary = value ?? ""; }
    [NotMapped] public string? DBusinessHours { get => BusinessHours; set => BusinessHours = value; }

    // Video slots backed by CardVideos collection (loaded/saved by WebsiteService helpers).
    [NotMapped] public string? DYoutube1 { get; set; }
    [NotMapped] public string? DYoutube2 { get; set; }
    [NotMapped] public string? DYoutube3 { get; set; }
    [NotMapped] public string? DYoutube4 { get; set; }
    [NotMapped] public string? DYoutube5 { get; set; }
    [NotMapped] public string? DYoutube6 { get; set; }
    [NotMapped] public string? DYoutube7 { get; set; }
    [NotMapped] public string? DYoutube8 { get; set; }
    [NotMapped] public string? DYoutube9 { get; set; }
    [NotMapped] public string? DYoutube10 { get; set; }
    [NotMapped] public string? DYoutube11 { get; set; }
    [NotMapped] public string? DYoutube12 { get; set; }
    [NotMapped] public string? DYoutube13 { get; set; }
    [NotMapped] public string? DYoutube14 { get; set; }
    [NotMapped] public string? DYoutube15 { get; set; }
    [NotMapped] public string? DYoutube16 { get; set; }
    [NotMapped] public string? DYoutube17 { get; set; }
    [NotMapped] public string? DYoutube18 { get; set; }
    [NotMapped] public string? DYoutube19 { get; set; }
    [NotMapped] public string? DYoutube20 { get; set; }
}
