using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MiniWebsite.Domain.Entities;
using MiniWebsite.Domain.Enums;

namespace MiniWebsite.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Role).HasMaxLength(20).HasConversion(new UserRoleConverter()).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(25);
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Password).HasMaxLength(255).IsRequired();
        builder.Property(x => x.PasswordHash).HasMaxLength(255);
        builder.Property(x => x.Ip).HasMaxLength(100);
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.UserToken).HasMaxLength(200);
        builder.Property(x => x.RefundStatus).HasMaxLength(20);
        builder.Property(x => x.CollaborationEnabled).IsRequired();
        builder.Property(x => x.SaleskitEnabled).IsRequired();
        builder.Property(x => x.Influencer).IsRequired();
        builder.Property(x => x.SelectService).HasMaxLength(200);
        builder.Property(x => x.GooglePay).HasMaxLength(200);
        builder.Property(x => x.Paytm).HasMaxLength(200);
        builder.Property(x => x.RzPay).HasMaxLength(300);
        builder.Property(x => x.RzPay2).HasMaxLength(300);
        builder.Property(x => x.Department).HasMaxLength(100);
        builder.Property(x => x.ProfileImage).HasMaxLength(500);
        builder.Property(x => x.ReferralCode).HasMaxLength(50);
        builder.Property(x => x.District).HasMaxLength(100);
        builder.Property(x => x.State).HasMaxLength(100);
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.Property(x => x.City).HasMaxLength(100);
        builder.Property(x => x.Pincode).HasMaxLength(20);
        builder.Property(x => x.Gst).HasMaxLength(50);
        builder.HasIndex(x => x.Email).IsUnique();
        builder.HasIndex(x => x.ReferralCode);
        builder.HasOne(x => x.AssignedRole)
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

internal sealed class UserRoleConverter : ValueConverter<UserRole, string>
{
    public UserRoleConverter() : base(v => ToDb(v), v => FromDb(v)) { }

    private static string ToDb(UserRole role) => role switch
    {
        UserRole.Customer => "CUSTOMER",
        UserRole.Franchisee => "FRANCHISEE",
        UserRole.Team => "TEAM",
        UserRole.Admin => "ADMIN",
        _ => "CUSTOMER"
    };

    private static UserRole FromDb(string value) =>
        (value ?? "").Trim().ToUpperInvariant() switch
        {
            "CUSTOMER" => UserRole.Customer,
            "FRANCHISEE" => UserRole.Franchisee,
            "TEAM" => UserRole.Team,
            "ADMIN" => UserRole.Admin,
            _ => UserRole.Customer
        };
}

public class CardConfiguration : IEntityTypeConfiguration<Card>
{
    public void Configure(EntityTypeBuilder<Card> builder)
    {
        builder.ToTable("cards");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Slug).HasMaxLength(200);
        builder.Property(x => x.UserEmail).HasMaxLength(200);
        builder.Property(x => x.FranchiseeEmail).HasMaxLength(200);
        builder.Property(x => x.CompanyName).HasMaxLength(200);
        builder.Property(x => x.DisplayName).HasMaxLength(199);
        builder.Property(x => x.ThemeCss).HasMaxLength(120);
        builder.Property(x => x.MobileCss).HasMaxLength(120);
        builder.Property(x => x.LogoPath).HasMaxLength(1000);
        builder.Property(x => x.HeroImagePath).HasMaxLength(255);
        builder.Property(x => x.PaymentAmount).HasMaxLength(50);
        builder.Property(x => x.CollaborationEnabled).HasMaxLength(3);
        builder.Property(x => x.ComplimentaryEnabled).HasMaxLength(10);
        builder.Property(x => x.AboutUs).HasMaxLength(2000);
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.UserEmail);
        builder.HasOne(x => x.User).WithMany(x => x.Cards).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.SlugHistory).WithOne(x => x.Card).HasForeignKey<CardSlugHistory>(x => x.CardId);
    }
}

public class CardSlugHistoryConfiguration : IEntityTypeConfiguration<CardSlugHistory>
{
    public void Configure(EntityTypeBuilder<CardSlugHistory> builder)
    {
        builder.ToTable("card_slug_history");
        builder.HasKey(x => x.CardId);
        builder.Property(x => x.PreviousSlug).HasMaxLength(255).IsRequired();
        builder.HasIndex(x => x.PreviousSlug).IsUnique();
    }
}

public class CardChildrenConfigurations :
    IEntityTypeConfiguration<CardProduct>,
    IEntityTypeConfiguration<CardService>,
    IEntityTypeConfiguration<CardGalleryImage>,
    IEntityTypeConfiguration<CardOffer>,
    IEntityTypeConfiguration<CardVideo>
{
    public void Configure(EntityTypeBuilder<CardProduct> builder)
    {
        builder.ToTable("card_products");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Mrp).HasPrecision(12, 2);
        builder.Property(x => x.SellingPrice).HasPrecision(12, 2);
        builder.Property(x => x.TaxRate).HasPrecision(12, 2);
        builder.Property(x => x.ProductImage).HasMaxLength(500);
        builder.HasIndex(x => x.CardId);
        builder.HasOne(x => x.Card).WithMany(x => x.Products).HasForeignKey(x => x.CardId).OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<CardService> builder)
    {
        builder.ToTable("card_services");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProductImage).HasMaxLength(500);
        builder.HasIndex(x => x.CardId);
        builder.HasOne(x => x.Card).WithMany(x => x.Services).HasForeignKey(x => x.CardId).OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<CardGalleryImage> builder)
    {
        builder.ToTable("card_gallery_images");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.GalleryImage).HasMaxLength(500);
        builder.HasIndex(x => x.CardId);
        builder.HasOne(x => x.Card).WithMany(x => x.GalleryImages).HasForeignKey(x => x.CardId).OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<CardOffer> builder)
    {
        builder.ToTable("card_offers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OfferTitle).HasMaxLength(255).IsRequired();
        builder.Property(x => x.OfferImage).HasMaxLength(255);
        builder.HasIndex(x => x.CardId);
        builder.HasOne(x => x.Card).WithMany(x => x.Offers).HasForeignKey(x => x.CardId).OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<CardVideo> builder)
    {
        builder.ToTable("card_videos");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Url).HasMaxLength(500);
        builder.Property(x => x.VideoType).HasMaxLength(20);
        builder.Property(x => x.ThumbnailPath).HasMaxLength(500);
        builder.HasIndex(x => x.CardId);
        builder.HasOne(x => x.Card).WithMany(x => x.Videos).HasForeignKey(x => x.CardId).OnDelete(DeleteBehavior.Cascade);
    }
}
