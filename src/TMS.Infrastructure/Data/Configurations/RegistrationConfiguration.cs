using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TMS.Core.Entities.Trainees;

namespace TMS.Infrastructure.Data.Configurations;

public class RegistrationConfiguration : IEntityTypeConfiguration<Registration>
{
    public void Configure(EntityTypeBuilder<Registration> builder)
    {
        builder.Property(r => r.FeeAmount).HasColumnType("decimal(18,2)");
        builder.Property(r => r.AmountPaid).HasColumnType("decimal(18,2)");
        builder.HasOne(r => r.Trainee)
            .WithMany(t => t.Registrations)
            .HasForeignKey(r => r.TraineeId);
        builder.HasOne(r => r.CourseSchedule)
            .WithMany()
            .HasForeignKey(r => r.CourseScheduleId);
        builder.HasOne(r => r.RegisteredBy)
            .WithMany()
            .HasForeignKey(r => r.RegisteredById)
            .OnDelete(DeleteBehavior.SetNull);
    }
}