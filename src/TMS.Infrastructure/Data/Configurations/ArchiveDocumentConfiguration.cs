using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TMS.Core.Entities.Archive;

namespace TMS.Infrastructure.Data.Configurations;

public class ArchiveDocumentConfiguration : IEntityTypeConfiguration<ArchiveDocument>
{
    public void Configure(EntityTypeBuilder<ArchiveDocument> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Code).HasMaxLength(20).IsRequired();
        builder.Property(d => d.TitleAr).HasMaxLength(300).IsRequired();
        builder.Property(d => d.Subject).HasMaxLength(1000);
        builder.Property(d => d.SenderName).HasMaxLength(200);
        builder.Property(d => d.RecipientName).HasMaxLength(200);
        builder.Property(d => d.Notes).HasMaxLength(2000);
        builder.Property(d => d.FilePath).HasMaxLength(500);
        builder.Property(d => d.OriginalFileName).HasMaxLength(300);
        builder.Property(d => d.ContentType).HasMaxLength(100);
        builder.HasOne(d => d.Department).WithMany().HasForeignKey(d => d.DepartmentId);
        builder.HasOne(d => d.CreatedBy).WithMany().HasForeignKey(d => d.CreatedById);
        builder.HasIndex(d => d.Code).IsUnique();
        builder.HasIndex(d => new { d.Year, d.Sequence }).IsUnique();
    }
}