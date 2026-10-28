using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWebsite.Domain.Entities;

namespace MiniWebsite.Infrastructure.Persistence.Configurations;

public class UploadedFileConfiguration : IEntityTypeConfiguration<UploadedFile>
{
    public void Configure(EntityTypeBuilder<UploadedFile> builder)
    {
        builder.ToTable("uploaded_files", t =>
        {
            t.HasCheckConstraint(
                "CK_uploaded_files_Status",
                "`Status` IN ('Current', 'Replaced')");
            t.HasCheckConstraint(
                "CK_uploaded_files_Folder",
                "`Folder` IN ('profile', 'verification')");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Folder).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Document).HasMaxLength(40).IsRequired();
        builder.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.StoredFileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.ObjectKey).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.Folder, x.Document, x.Status });
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
