using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWebsite.Domain.Common;
using MiniWebsite.Domain.Entities;

namespace MiniWebsite.Infrastructure.Persistence.Configurations;

public class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.ToTable("coupons", t => t.HasCheckConstraint(
            "CK_coupons_Status",
            "`Status` IN ('Active', 'InActive', 'Expired')"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CouponName).HasMaxLength(200);
        builder.Property(x => x.CouponCode).HasMaxLength(100);
        builder.Property(x => x.CouponType).HasMaxLength(50);
        builder.Property(x => x.PromoDiscountType).HasMaxLength(20);
        builder.Property(x => x.PromoDiscountValue).HasPrecision(12, 2);
        builder.Property(x => x.Status).HasMaxLength(20);
        builder.Property(x => x.CreatedBy).HasMaxLength(200);
        builder.HasIndex(x => x.CouponCode);
        builder.HasIndex(x => x.RoleId);
        builder.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class CommerceConfigurations :
    IEntityTypeConfiguration<PricingPlan>,
    IEntityTypeConfiguration<PlanCommission>,
    IEntityTypeConfiguration<DealCustomerMapping>,
    IEntityTypeConfiguration<JoiningDeal>,
    IEntityTypeConfiguration<UserJoiningDeal>,
    IEntityTypeConfiguration<Order>,
    IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<PricingPlan> builder)
    {
        builder.ToTable("pricing_plans");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PlanName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.PlanCategory).HasMaxLength(50).IsRequired()
            .HasDefaultValue(PlanCommissionValues.CategoryDbProfile);
        builder.Property(x => x.OriginalPrice).HasPrecision(12, 2);
        builder.Property(x => x.OriginalPriceGst).HasPrecision(12, 2);
        builder.Property(x => x.OfferedPrice).HasPrecision(12, 2);
        builder.Property(x => x.OfferedPriceGst).HasPrecision(12, 2);
        builder.Property(x => x.Status).HasMaxLength(20);
        builder.HasIndex(x => x.PlanName).IsUnique();
    }

    public void Configure(EntityTypeBuilder<PlanCommission> builder)
    {
        builder.ToTable("plan_commissions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DiscountType).HasMaxLength(30).IsRequired();
        builder.Property(x => x.DiscountAmount).HasPrecision(12, 2);
        builder.Property(x => x.EarningType).HasMaxLength(30).IsRequired();
        builder.Property(x => x.EarningAmount).HasPrecision(12, 2);
        builder.Property(x => x.RenewalPrice).HasPrecision(12, 2);
        builder.Property(x => x.RenewalPriceGst).HasPrecision(12, 2);
        builder.Property(x => x.RenewalEarningPercent).HasPrecision(5, 2);
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.HasIndex(x => new { x.RoleId, x.PricingPlanId }).IsUnique();
        builder.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PricingPlan).WithMany().HasForeignKey(x => x.PricingPlanId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<DealCustomerMapping> builder)
    {
        builder.ToTable("deal_customer_mappings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CustomerEmail).HasMaxLength(255).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(255);
        builder.HasIndex(x => x.CouponId);
        builder.HasIndex(x => x.CustomerEmail);
        builder.HasOne(x => x.Coupon).WithMany(x => x.CustomerMappings).HasForeignKey(x => x.CouponId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
    }

    public void Configure(EntityTypeBuilder<JoiningDeal> builder)
    {
        builder.ToTable("joining_deals");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DealCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.DealName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.DealType).HasMaxLength(100);
        builder.Property(x => x.Fees).HasPrecision(12, 2);
        builder.Property(x => x.GstAmount).HasPrecision(12, 2);
        builder.Property(x => x.TotalFees).HasPrecision(12, 2);
        builder.Property(x => x.CommissionAmount).HasPrecision(12, 2);
        builder.Property(x => x.DiscountAmount).HasPrecision(12, 2);
        builder.HasIndex(x => x.DealCode).IsUnique();
        builder.HasIndex(x => x.MwDealId);
        builder.HasIndex(x => x.FranchiseDealId);
        builder.HasOne(x => x.MwDeal).WithMany().HasForeignKey(x => x.MwDealId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.FranchiseDeal).WithMany().HasForeignKey(x => x.FranchiseDealId).OnDelete(DeleteBehavior.SetNull);
    }

    public void Configure(EntityTypeBuilder<UserJoiningDeal> builder)
    {
        builder.ToTable("user_joining_deals");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DealCode).HasMaxLength(50);
        builder.Property(x => x.MappingStatus).HasMaxLength(20);
        builder.HasIndex(x => x.UserId);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.JoiningDeal).WithMany().HasForeignKey(x => x.JoiningDealId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Invoice>().WithMany().HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.SetNull);
    }

    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ExternalId).HasMaxLength(200);
        builder.Property(x => x.UserEmail).HasMaxLength(200);
        builder.Property(x => x.CustomerEmail).HasMaxLength(255);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.InvoiceId);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.Card).WithMany().HasForeignKey(x => x.CardId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.Invoice).WithMany().HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.SetNull);
    }

    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices", t => t.HasCheckConstraint(
            "CK_invoices_PaymentStatus",
            "`PaymentStatus` IN ('Created', 'Pending', 'Success', 'Paid', 'Failed', 'Rejected', 'Refunded')"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.InvoiceNumber).HasMaxLength(20).IsRequired();
        builder.Property(x => x.OrderCode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.UserEmail).HasMaxLength(255);
        builder.Property(x => x.BillingEmail).HasMaxLength(255);
        builder.Property(x => x.BillingCity).HasMaxLength(100);
        builder.Property(x => x.PaymentStatus).HasMaxLength(20);
        builder.Property(x => x.AdminComment).HasMaxLength(1000);
        builder.Property(x => x.OriginalAmount).HasPrecision(12, 2);
        builder.Property(x => x.DiscountAmount).HasPrecision(12, 2);
        builder.Property(x => x.FinalAmount).HasPrecision(12, 2);
        builder.Property(x => x.PromoDiscount).HasPrecision(12, 2);
        builder.Property(x => x.UnitPrice).HasPrecision(12, 2);
        builder.Property(x => x.TotalPrice).HasPrecision(12, 2);
        builder.Property(x => x.SubTotal).HasPrecision(12, 2);
        builder.Property(x => x.IgstPercentage).HasPrecision(5, 2);
        builder.Property(x => x.IgstAmount).HasPrecision(12, 2);
        builder.Property(x => x.CgstAmount).HasPrecision(12, 2);
        builder.Property(x => x.SgstAmount).HasPrecision(12, 2);
        builder.Property(x => x.TotalAmount).HasPrecision(12, 2);
        builder.HasIndex(x => x.InvoiceNumber).IsUnique();
        builder.HasIndex(x => x.OrderCode).IsUnique();
        builder.HasIndex(x => x.UserEmail);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.Card).WithMany().HasForeignKey(x => x.CardId).OnDelete(DeleteBehavior.SetNull);
    }
}
