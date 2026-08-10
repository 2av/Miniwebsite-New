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
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.State).HasMaxLength(100);
        builder.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
        builder.Property(x => x.PlainPassword).HasMaxLength(255).IsRequired();
        builder.Property(x => x.ReferrerEmail).HasMaxLength(255);
        builder.Property(x => x.Otp).HasMaxLength(10).IsRequired();
        builder.HasIndex(x => new { x.Email, x.Role });
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
