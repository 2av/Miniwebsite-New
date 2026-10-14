using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWebsite.Domain.Entities;

namespace MiniWebsite.Infrastructure.Persistence.Configurations;

public class CrmContentConfigurations :
    IEntityTypeConfiguration<Customer>,
    IEntityTypeConfiguration<CustomerFollowup>,
    IEntityTypeConfiguration<ReferralEarning>,
    IEntityTypeConfiguration<ReferralPayment>,
    IEntityTypeConfiguration<ContentPage>,
    IEntityTypeConfiguration<Faq>,
    IEntityTypeConfiguration<DocSection>,
    IEntityTypeConfiguration<DocPage>,
    IEntityTypeConfiguration<DocMedia>,
    IEntityTypeConfiguration<ThemeCatalog>,
    IEntityTypeConfiguration<CardView>,
    IEntityTypeConfiguration<EmailLog>,
    IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CustomerName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.PhoneNumber).HasMaxLength(25).IsRequired();
        builder.Property(x => x.OwnerRole).HasMaxLength(20);
        builder.HasIndex(x => x.OwnerUserId);
        builder.HasOne(x => x.OwnerUser).WithMany().HasForeignKey(x => x.OwnerUserId).OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<CustomerFollowup> builder)
    {
        builder.ToTable("customer_followups");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.CustomerId);
        builder.HasOne(x => x.Customer).WithMany(x => x.Followups).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<ReferralEarning> builder)
    {
        builder.ToTable("referral_earnings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ReferrerEmail).HasMaxLength(255).IsRequired();
        builder.Property(x => x.ReferredEmail).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(12, 2);
        builder.Property(x => x.Status).HasMaxLength(20);
        builder.Property(x => x.IsCollaboration).HasMaxLength(10);
        builder.Property(x => x.CommissionCode).HasMaxLength(30);
        builder.Property(x => x.AdminComment).HasMaxLength(1000);
        builder.HasIndex(x => x.CommissionCode).IsUnique();
        builder.HasIndex(x => x.ReferrerEmail);
        builder.HasIndex(x => x.ReferredEmail);
        builder.HasOne(x => x.ReferrerUser).WithMany().HasForeignKey(x => x.ReferrerUserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.ReferredUser).WithMany().HasForeignKey(x => x.ReferredUserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.Coupon).WithMany().HasForeignKey(x => x.DealId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.Invoice).WithMany().HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.SetNull);
    }

    public void Configure(EntityTypeBuilder<ReferralPayment> builder)
    {
        builder.ToTable("referral_payments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasPrecision(12, 2);
        builder.HasOne(x => x.ReferralEarning).WithMany(x => x.Payments).HasForeignKey(x => x.ReferralEarningId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<ContentPage> builder)
    {
        builder.ToTable("content_pages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ContentType).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(255).IsRequired();
        builder.HasIndex(x => x.ContentType).IsUnique();
    }

    public void Configure(EntityTypeBuilder<Faq> builder)
    {
        builder.ToTable("faqs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PageType).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20);
        builder.HasIndex(x => new { x.PageType, x.SortOrder });
    }

    public void Configure(EntityTypeBuilder<DocSection> builder)
    {
        builder.ToTable("doc_sections");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(191).IsRequired();
        builder.HasIndex(x => x.Slug).IsUnique();
    }

    public void Configure(EntityTypeBuilder<DocPage> builder)
    {
        builder.ToTable("doc_pages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(191).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20);
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasOne(x => x.Section).WithMany(x => x.Pages).HasForeignKey(x => x.SectionId).OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<DocMedia> builder)
    {
        builder.ToTable("doc_media");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Filename).HasMaxLength(255).IsRequired();
        builder.Property(x => x.RelPath).HasMaxLength(512).IsRequired();
        builder.Property(x => x.Mime).HasMaxLength(128);
    }

    public void Configure(EntityTypeBuilder<ThemeCatalog> builder)
    {
        builder.ToTable("theme_catalog");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ThemeName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CssValue).HasMaxLength(120).IsRequired();
        builder.HasIndex(x => x.ThemeNumber).IsUnique();
    }

    public void Configure(EntityTypeBuilder<CardView> builder)
    {
        builder.ToTable("card_views");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Ip).HasMaxLength(100);
        builder.Property(x => x.CardSlug).HasMaxLength(200);
        builder.HasOne(x => x.Card).WithMany().HasForeignKey(x => x.CardId).OnDelete(DeleteBehavior.SetNull);
    }

    public void Configure(EntityTypeBuilder<EmailLog> builder)
    {
        builder.ToTable("email_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserEmail).HasMaxLength(255).IsRequired();
        builder.Property(x => x.EmailType).HasMaxLength(100);
        builder.Property(x => x.Subject).HasMaxLength(500);
        builder.Property(x => x.Status).HasMaxLength(20);
    }

    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Audience).HasMaxLength(20);
        builder.Property(x => x.Type).HasMaxLength(50);
        builder.Property(x => x.Title).HasMaxLength(255);
        builder.Property(x => x.Priority).HasMaxLength(20);
        builder.HasIndex(x => new { x.Audience, x.IsRead });
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class KitAccessConfigurations :
    IEntityTypeConfiguration<KitFolder>,
    IEntityTypeConfiguration<KitItem>,
    IEntityTypeConfiguration<ProductCategory>,
    IEntityTypeConfiguration<UserCustomCategory>,
    IEntityTypeConfiguration<UserBankDetails>,
    IEntityTypeConfiguration<FranchiseVerification>
{
    public void Configure(EntityTypeBuilder<KitFolder> builder)
    {
        builder.ToTable("kit_folders");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(50);
        builder.Property(x => x.Status).HasMaxLength(20);
        builder.HasOne(x => x.Parent).WithMany().HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.SetNull);
    }

    public void Configure(EntityTypeBuilder<KitItem> builder)
    {
        builder.ToTable("kit_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type).HasMaxLength(20);
        builder.Property(x => x.Title).HasMaxLength(255);
        builder.Property(x => x.FilePath).HasMaxLength(500);
        builder.Property(x => x.Status).HasMaxLength(20);
        builder.Property(x => x.Category).HasMaxLength(50);
        builder.HasOne(x => x.Folder).WithMany(x => x.Items).HasForeignKey(x => x.FolderId).OnDelete(DeleteBehavior.SetNull);
    }

    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.ToTable("product_categories");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BusinessProfileType).HasMaxLength(100);
        builder.Property(x => x.BusinessCategory).HasMaxLength(255);
        builder.Property(x => x.ProductCategoryName).HasMaxLength(255);
    }

    public void Configure(EntityTypeBuilder<UserCustomCategory> builder)
    {
        builder.ToTable("user_custom_categories");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CategoryName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.CategoryType).HasMaxLength(50);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<UserBankDetails> builder)
    {
        builder.ToTable("user_bank_details");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserEmail).HasMaxLength(255);
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.HasIndex(x => x.UserEmail);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<FranchiseVerification> builder)
    {
        builder.ToTable("franchise_verifications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserEmail).HasMaxLength(255);
        builder.Property(x => x.Status).HasMaxLength(20);
        builder.Property(x => x.DocumentPath).HasMaxLength(500);
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.HasIndex(x => x.UserEmail);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
