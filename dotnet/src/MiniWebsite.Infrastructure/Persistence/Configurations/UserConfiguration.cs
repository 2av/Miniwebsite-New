using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWebsite.Domain.Entities;

namespace MiniWebsite.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Email).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(25);
        builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Password).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Ip).HasMaxLength(100);
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.UserToken).HasMaxLength(200);
        builder.Property(x => x.RefundStatus).HasMaxLength(20);
        builder.Property(x => x.SelectService).HasMaxLength(200);
        builder.Property(x => x.Department).HasMaxLength(100);
        builder.Property(x => x.ProfileImage).HasMaxLength(500);
        builder.Property(x => x.UserCode).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.UserCode).IsUnique();
        builder.HasIndex(x => x.Email).IsUnique();
        builder.HasIndex(x => x.RoleId);
        builder.HasIndex(x => new { x.RoleId, x.CreatedAt });
        builder.HasOne(x => x.AssignedRole)
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(x => x.AssignedRole).AutoInclude();
        builder.Ignore(x => x.LoginRole);
        builder.Ignore(x => x.FullName);

        builder.OwnsOne(x => x.Address, a =>
        {
            a.ToTable("user_addresses");
            a.WithOwner().HasForeignKey("UserId");
            a.Property<int>("UserId");
            a.HasKey("UserId");
            a.Property(x => x.AddressLine).HasMaxLength(500);
            a.Property(x => x.Address2).HasMaxLength(500);
            a.Property(x => x.City).HasMaxLength(100);
            a.Property(x => x.Country).HasMaxLength(100);
            a.Property(x => x.District).HasMaxLength(100);
            a.Property(x => x.State).HasMaxLength(100);
            a.Property(x => x.Pincode).HasMaxLength(20);
            a.Property(x => x.Gst).HasMaxLength(50);
        });
        builder.Navigation(x => x.Address).IsRequired();

        builder.OwnsOne(x => x.Payment, p =>
        {
            p.ToTable("user_payment_details");
            p.WithOwner().HasForeignKey("UserId");
            p.Property<int>("UserId");
            p.HasKey("UserId");
            p.Property(x => x.GooglePay).HasMaxLength(200);
            p.Property(x => x.Paytm).HasMaxLength(200);
            p.Property(x => x.PhonePay).HasMaxLength(200);
            p.Property(x => x.RzPay).HasMaxLength(300);
            p.Property(x => x.RzPay2).HasMaxLength(300);
        });
        builder.Navigation(x => x.Payment).IsRequired();

        builder.OwnsOne(x => x.Referral, r =>
        {
            r.ToTable("user_referral_settings");
            r.WithOwner().HasForeignKey("UserId");
            r.Property<int>("UserId");
            r.HasKey("UserId");
            r.Property(x => x.ReferralCode).HasMaxLength(50);
            r.Property(x => x.ReferredBy).HasMaxLength(255);
            r.Property(x => x.Influencer).IsRequired();
            r.Property(x => x.CollaborationEnabled).IsRequired();
            r.Property(x => x.SaleskitEnabled).IsRequired();
            r.HasIndex(x => x.ReferralCode);
            r.HasIndex(x => x.ReferredBy);
            r.HasIndex(x => x.ReferrerUserId);
            r.HasIndex(x => x.SenderUserId);
            r.HasOne<User>().WithMany().HasForeignKey(x => x.ReferrerUserId).OnDelete(DeleteBehavior.SetNull);
            r.HasOne<User>().WithMany().HasForeignKey(x => x.SenderUserId).OnDelete(DeleteBehavior.SetNull);
        });
        builder.Navigation(x => x.Referral).IsRequired();

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class CardConfiguration : IEntityTypeConfiguration<Card>
{
    public void Configure(EntityTypeBuilder<Card> builder)
    {
        builder.ToTable("cards", t => t.HasCheckConstraint(
            "CK_cards_CardStatus",
            "`CardStatus` IS NULL OR `CardStatus` IN ('Active', 'Suspended', 'Inactive', 'InActive')"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Slug).HasMaxLength(200);
        builder.Property(x => x.Email).HasMaxLength(255);
        builder.Property(x => x.CardStatus).HasMaxLength(20);
        builder.Property(x => x.UserEmail).HasMaxLength(200);
        builder.Property(x => x.FranchiseeEmail).HasMaxLength(200);
        builder.Property(x => x.CompanyName).HasMaxLength(200);
        builder.Property(x => x.DisplayName).HasMaxLength(199);
        builder.Property(x => x.ThemeCss).HasMaxLength(120);
        builder.Property(x => x.MobileCss).HasMaxLength(120);
        builder.Property(x => x.LogoPath).HasMaxLength(1000);
        builder.Property(x => x.HeroImagePath).HasMaxLength(255);
        builder.Property(x => x.CollaborationEnabled).HasMaxLength(3);
        builder.Property(x => x.ComplimentaryEnabled).HasMaxLength(10);
        builder.Property(x => x.AboutUs).HasMaxLength(2000);
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.UserEmail);
        builder.HasIndex(x => x.FranchiseeEmail);
        builder.HasIndex(x => x.FranchiseeUserId);
        builder.HasOne(x => x.User).WithMany(x => x.Cards).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.FranchiseeUser).WithMany().HasForeignKey(x => x.FranchiseeUserId).OnDelete(DeleteBehavior.SetNull);
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
        builder.HasIndex(x => x.ProductCategory);
        builder.HasOne(x => x.Card).WithMany(x => x.Products).HasForeignKey(x => x.CardId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ProductCategory>().WithMany().HasForeignKey(x => x.ProductCategory).OnDelete(DeleteBehavior.SetNull);
    }

    public void Configure(EntityTypeBuilder<CardService> builder)
    {
        builder.ToTable("card_services");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProductImage).HasMaxLength(500);
        builder.HasIndex(x => x.CardId);
        builder.HasIndex(x => x.ProductCategory);
        builder.HasOne(x => x.Card).WithMany(x => x.Services).HasForeignKey(x => x.CardId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ProductCategory>().WithMany().HasForeignKey(x => x.ProductCategory).OnDelete(DeleteBehavior.SetNull);
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
