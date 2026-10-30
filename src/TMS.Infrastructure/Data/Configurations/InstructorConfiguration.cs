using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TMS.Core.Entities.Instructors;

namespace TMS.Infrastructure.Data.Configurations;

public class InstructorConfiguration : IEntityTypeConfiguration<Instructor>
{
    public void Configure(EntityTypeBuilder<Instructor> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.FirstNameAr).HasMaxLength(100).IsRequired();
        builder.Property(i => i.LastNameAr).HasMaxLength(100).IsRequired();
        builder.Property(i => i.FirstNameEn).HasMaxLength(100).IsRequired();
        builder.Property(i => i.LastNameEn).HasMaxLength(100).IsRequired();
        builder.Property(i => i.HourlyRate).HasColumnType("decimal(18,2)");        builder.HasOne(i => i.User)
            .WithMany()
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
