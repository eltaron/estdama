using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TMS.Core.Entities.Identity;

namespace TMS.Infrastructure.Data.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FullNameAr).HasMaxLength(200).IsRequired();
        builder.Property(u => u.FullNameEn).HasMaxLength(200).IsRequired();
        builder.Property(u => u.EmployeeNumber).HasMaxLength(50);
        builder.HasIndex(u => u.EmployeeNumber).IsUnique();

        builder.HasOne(u => u.Department)
            .WithMany()
            .HasForeignKey(u => u.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
