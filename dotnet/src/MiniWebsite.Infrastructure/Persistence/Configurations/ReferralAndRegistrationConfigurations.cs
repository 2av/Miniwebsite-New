using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWebsite.Domain.Entities;

namespace MiniWebsite.Infrastructure.Persistence.Configurations;

public class RegistrationPendingConfiguration : IEntityTypeConfiguration<RegistrationPending>
{
    public void Configure(EntityTypeBuilder<RegistrationPending> builder)
    {
        builder.ToTable("registration_pending");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Role).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(25).IsRequired();
        builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.State).HasMaxLength(100);
        builder.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
        builder.Property(x => x.IssuedPassword).HasMaxLength(255);
        builder.Property(x => x.ReferrerEmail).HasMaxLength(255);
        builder.Property(x => x.Otp).HasMaxLength(10).IsRequired();
        builder.HasIndex(x => new { x.Email, x.Role });
        builder.HasIndex(x => x.ReferrerUserId);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.ReferrerUserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class AccountInviteConfiguration : IEntityTypeConfiguration<AccountInvite>
{
    public void Configure(EntityTypeBuilder<AccountInvite> builder)
    {
        builder.ToTable("account_invites");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Email).HasMaxLength(255).IsRequired();
        builder.Property(x => x.RolePrefix).HasMaxLength(20).IsRequired();
        builder.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => x.Email);
        builder.HasIndex(x => x.InvitedByUserId);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.InvitedByUserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class FieldSalesDealConfiguration : IEntityTypeConfiguration<FieldSalesDeal>
{
    public void Configure(EntityTypeBuilder<FieldSalesDeal> builder)
    {
        builder.ToTable("field_sales_deals");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Details).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(12, 2);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.UserId, x.DealDate });
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
