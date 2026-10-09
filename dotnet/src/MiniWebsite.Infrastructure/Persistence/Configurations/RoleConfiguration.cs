using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWebsite.Domain.Entities;

namespace MiniWebsite.Infrastructure.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.HasKey(x => x.RoleId);
        builder.Property(x => x.RoleName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.RoleDescription).HasMaxLength(500);
        builder.Property(x => x.Prefix).HasMaxLength(40);
        builder.Property(x => x.SalesType).HasMaxLength(80);
        builder.HasIndex(x => x.RoleName).IsUnique();
        builder.HasIndex(x => x.SalesType);
    }
}
