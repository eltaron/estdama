using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TMS.Core.Entities.Finance;

namespace TMS.Infrastructure.Data.Configurations;

public class FinanceTransactionConfiguration : IEntityTypeConfiguration<FinanceTransaction>
{
    public void Configure(EntityTypeBuilder<FinanceTransaction> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Amount).HasColumnType("decimal(18,2)");
        builder.Property(t => t.DescriptionAr).HasMaxLength(500).IsRequired();
        builder.Property(t => t.ReferenceNo).HasMaxLength(100);
        builder.HasOne(t => t.Course)
            .WithMany()
            .HasForeignKey(t => t.CourseId);
        builder.HasOne(t => t.CreatedBy)
            .WithMany()
            .HasForeignKey(t => t.CreatedById)
            .OnDelete(DeleteBehavior.SetNull);
    }
}