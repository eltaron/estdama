using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TMS.Core.Entities.Instructors;

namespace TMS.Infrastructure.Data.Configurations;

public class InstructorEvaluationConfiguration : IEntityTypeConfiguration<InstructorEvaluation>
{
    public void Configure(EntityTypeBuilder<InstructorEvaluation> builder)
    {
        builder.Property(e => e.AverageRating).HasColumnType("decimal(18,2)");
    }
}
