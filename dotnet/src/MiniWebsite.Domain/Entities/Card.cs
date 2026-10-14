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
    public string? Address { get; set; }
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
    public string? Paytm { get; set; }
    public string? GooglePay { get; set; }
    public string? PhonePay { get; set; }
    public string? AccountNo { get; set; }
    public string? Ifsc { get; set; }
    public string? AccountName { get; set; }
    public string? BankName { get; set; }
    public string? AccountType { get; set; }
    public string? PaymentStatus { get; set; }
    public string? CardStatus { get; set; }
    public string? PaymentAmount { get; set; }
    public string? OrderId { get; set; }
    public string? LogoPath { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? Gst { get; set; }
    public string? GstName { get; set; }
    public string? GstAddress { get; set; }
    public string? GstState { get; set; }
    public string? GstCity { get; set; }
    public string? GstPincode { get; set; }
    public string CollaborationEnabled { get; set; } = "NO";
    public string ComplimentaryEnabled { get; set; } = "No";
    public string? GstEmail { get; set; }
    public string? GstContact { get; set; }
    public DateTime? ValidityDate { get; set; }
    public int NameChangeCount { get; set; }
    public int DisplayNameChangeCount { get; set; }
    public string? HeroImagePath { get; set; }
    public string PositionPrimary { get; set; } = "";
    public string PositionSecondary { get; set; } = "";
    public string GstNumber { get; set; } = "";
    public string Address2 { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string Pincode { get; set; } = "";
    public string Country { get; set; } = "";
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
    [NotMapped] public string? DAddress { get => Address; set => Address = value; }
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
    [NotMapped] public string? DPaytm { get => Paytm; set => Paytm = value; }
    [NotMapped] public string? DGooglePay { get => GooglePay; set => GooglePay = value; }
    [NotMapped] public string? DPhonePay { get => PhonePay; set => PhonePay = value; }
    [NotMapped] public string? DAccountNo { get => AccountNo; set => AccountNo = value; }
    [NotMapped] public string? DIfsc { get => Ifsc; set => Ifsc = value; }
    [NotMapped] public string? DAcName { get => AccountName; set => AccountName = value; }
    [NotMapped] public string? DBankName { get => BankName; set => BankName = value; }
    [NotMapped] public string? DAcType { get => AccountType; set => AccountType = value; }
    [NotMapped] public string? DPaymentStatus { get => PaymentStatus; set => PaymentStatus = value; }
    [NotMapped] public string? DCardStatus { get => CardStatus; set => CardStatus = value; }
    [NotMapped] public string? DPaymentAmount { get => PaymentAmount; set => PaymentAmount = value; }
    [NotMapped] public string? DOrderId { get => OrderId; set => OrderId = value; }
    [NotMapped] public string? DLogoLocation { get => LogoPath; set => LogoPath = value; }
    [NotMapped] public DateTime? UploadedDate { get => CreatedAt; set => CreatedAt = value; }
    [NotMapped] public DateTime? DPaymentDate { get => PaymentDate; set => PaymentDate = value; }
    [NotMapped] public string? DGst { get => Gst; set => Gst = value; }
    [NotMapped] public string? DGstName { get => GstName; set => GstName = value; }
    [NotMapped] public string? DGstAddress { get => GstAddress; set => GstAddress = value; }
    [NotMapped] public string? DGstState { get => GstState; set => GstState = value; }
    [NotMapped] public string? DGstCity { get => GstCity; set => GstCity = value; }
    [NotMapped] public string? DGstPincode { get => GstPincode; set => GstPincode = value; }
    [NotMapped] public string? DGstEmail { get => GstEmail; set => GstEmail = value; }
    [NotMapped] public string? DGstContact { get => GstContact; set => GstContact = value; }
    [NotMapped] public int DNameChangeCount { get => DisplayNameChangeCount; set => DisplayNameChangeCount = value; }
    [NotMapped] public string? DHeroImageLocation { get => HeroImagePath; set => HeroImagePath = value; }
    [NotMapped] public string DPositionPrimary { get => PositionPrimary; set => PositionPrimary = value ?? ""; }
    [NotMapped] public string DPositionSecondary { get => PositionSecondary; set => PositionSecondary = value ?? ""; }
    [NotMapped] public string DGstNumber { get => GstNumber; set => GstNumber = value ?? ""; }
    [NotMapped] public string DAddress2 { get => Address2; set => Address2 = value ?? ""; }
    [NotMapped] public string DCity { get => City; set => City = value ?? ""; }
    [NotMapped] public string DState { get => State; set => State = value ?? ""; }
    [NotMapped] public string DPincode { get => Pincode; set => Pincode = value ?? ""; }
    [NotMapped] public string DCountry { get => Country; set => Country = value ?? ""; }
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
