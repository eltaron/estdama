using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TMS.Core.Entities.Execution;

namespace TMS.Infrastructure.Data.Configurations;

public class ExamSubmissionConfiguration : IEntityTypeConfiguration<ExamSubmission>
{
    public void Configure(EntityTypeBuilder<ExamSubmission> builder)
    {
        builder.Property(e => e.TotalScore).HasColumnType("decimal(18,2)");
    }
}
