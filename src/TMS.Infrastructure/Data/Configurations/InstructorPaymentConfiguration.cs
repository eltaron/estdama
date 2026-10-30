using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TMS.Core.Entities.Instructors;

namespace TMS.Infrastructure.Data.Configurations;

public class InstructorPaymentConfiguration : IEntityTypeConfiguration<InstructorPayment>
{
    public void Configure(EntityTypeBuilder<InstructorPayment> builder)
    {
        builder.Property(p => p.HoursWorked).HasColumnType("decimal(18,2)");
        builder.Property(p => p.HourlyRate).HasColumnType("decimal(18,2)");
        builder.Property(p => p.TotalAmount).HasColumnType("decimal(18,2)");
    }
}
