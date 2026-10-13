using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWebsite.Domain.Entities;

namespace MiniWebsite.Infrastructure.Persistence.Configurations;

public class DealConfiguration : IEntityTypeConfiguration<Deal>
{
    public void Configure(EntityTypeBuilder<Deal> builder)
    {
        builder.ToTable("deals");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PlanName).HasMaxLength(200);
        builder.Property(x => x.PlanType).HasMaxLength(100);
        builder.Property(x => x.DealState).HasMaxLength(100);
        builder.Property(x => x.DealName).HasMaxLength(200);
        builder.Property(x => x.CouponCode).HasMaxLength(100);
        builder.Property(x => x.CouponType).HasMaxLength(50);
        builder.Property(x => x.BonusAmount).HasPrecision(12, 2);
        builder.Property(x => x.DiscountAmount).HasPrecision(12, 2);
        builder.Property(x => x.DiscountPercentage).HasPrecision(12, 2);
        builder.Property(x => x.RenewalBonusAmount).HasPrecision(12, 2);
        builder.Property(x => x.DealStatus).HasMaxLength(20);
        builder.Property(x => x.CreatedBy).HasMaxLength(200);
        builder.HasIndex(x => x.CouponCode);
    }
}

public class CommerceConfigurations :
    IEntityTypeConfiguration<PricingPlan>,
    IEntityTypeConfiguration<DealCustomerMapping>,
    IEntityTypeConfiguration<JoiningDeal>,
    IEntityTypeConfiguration<UserJoiningDeal>,
    IEntityTypeConfiguration<Wallet>,
    IEntityTypeConfiguration<WalletTransaction>,
    IEntityTypeConfiguration<Order>,
    IEntityTypeConfiguration<Invoice>,
    IEntityTypeConfiguration<FranchisePayment>
{
    public void Configure(EntityTypeBuilder<PricingPlan> builder)
    {
        builder.ToTable("pricing_plans");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PlanType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PlanName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.OriginalPrice).HasPrecision(12, 2);
        builder.Property(x => x.OriginalPriceGst).HasPrecision(12, 2);
        builder.Property(x => x.OfferedPrice).HasPrecision(12, 2);
        builder.Property(x => x.OfferedPriceGst).HasPrecision(12, 2);
        builder.Property(x => x.Status).HasMaxLength(20);
        builder.HasIndex(x => new { x.PlanType, x.PlanName });
        builder.HasOne(x => x.Deal).WithMany().HasForeignKey(x => x.DealId).OnDelete(DeleteBehavior.SetNull);
    }

    public void Configure(EntityTypeBuilder<DealCustomerMapping> builder)
    {
        builder.ToTable("deal_customer_mappings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CustomerEmail).HasMaxLength(255).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(255);
        builder.HasIndex(x => x.DealId);
        builder.HasIndex(x => x.CustomerEmail);
        builder.HasOne(x => x.Deal).WithMany(x => x.CustomerMappings).HasForeignKey(x => x.DealId).OnDelete(DeleteBehavior.Cascade);
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
    }

    public void Configure(EntityTypeBuilder<UserJoiningDeal> builder)
    {
        builder.ToTable("user_joining_deals");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserEmail).HasMaxLength(255);
        builder.Property(x => x.DealCode).HasMaxLength(50);
        builder.Property(x => x.MappingStatus).HasMaxLength(20);
        builder.Property(x => x.PaymentStatus).HasMaxLength(20);
        builder.Property(x => x.AmountPaid).HasPrecision(12, 2);
        builder.HasIndex(x => x.UserId);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.JoiningDeal).WithMany().HasForeignKey(x => x.JoiningDealId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Invoice>().WithMany().HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.SetNull);
    }

    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable("wallets");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserEmail).HasMaxLength(200);
        builder.Property(x => x.Balance).HasPrecision(12, 2);
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.HasOne(x => x.User).WithOne(x => x.Wallet).HasForeignKey<Wallet>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<WalletTransaction> builder)
    {
        builder.ToTable("wallet_transactions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type).HasMaxLength(30);
        builder.Property(x => x.Amount).HasPrecision(12, 2);
        builder.Property(x => x.BalanceAfter).HasPrecision(12, 2);
        builder.Property(x => x.OrderId).HasMaxLength(200);
        builder.HasIndex(x => x.WalletId);
        builder.HasOne(x => x.Wallet).WithMany().HasForeignKey(x => x.WalletId).OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ExternalId).HasMaxLength(200);
        builder.Property(x => x.UserEmail).HasMaxLength(200);
        builder.Property(x => x.PaymentAmount).HasPrecision(12, 2);
        builder.Property(x => x.OrderStatus).HasMaxLength(50);
        builder.HasIndex(x => x.UserId);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.Card).WithMany().HasForeignKey(x => x.CardId).OnDelete(DeleteBehavior.SetNull);
    }

    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.InvoiceNumber).HasMaxLength(20).IsRequired();
        builder.Property(x => x.OrderCode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.UserEmail).HasMaxLength(255);
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

    public void Configure(EntityTypeBuilder<FranchisePayment> builder)
    {
        builder.ToTable("franchise_payments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FranchiseEmail).HasMaxLength(255).IsRequired();
        builder.Property(x => x.ReferenceNumber).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(12, 2);
        builder.Property(x => x.RefundAmount).HasPrecision(12, 2);
        builder.Property(x => x.Currency).HasMaxLength(10);
        builder.Property(x => x.PaymentStatus).HasMaxLength(20);
        builder.HasOne(x => x.FranchiseUser).WithMany().HasForeignKey(x => x.FranchiseUserId).OnDelete(DeleteBehavior.SetNull);
    }
}
