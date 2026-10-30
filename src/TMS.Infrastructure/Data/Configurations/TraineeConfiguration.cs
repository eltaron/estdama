using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TMS.Core.Entities.Trainees;

namespace TMS.Infrastructure.Data.Configurations;

public class TraineeConfiguration : IEntityTypeConfiguration<Trainee>
{
    public void Configure(EntityTypeBuilder<Trainee> builder)
    {
        builder.HasKey(t => t.Id);
        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(t => t.CurrentDepartment)
            .WithMany()
            .HasForeignKey(t => t.CurrentDepartmentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
