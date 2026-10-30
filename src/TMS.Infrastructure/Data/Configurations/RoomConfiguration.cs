using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TMS.Core.Entities.Resources;

namespace TMS.Infrastructure.Data.Configurations;

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.NameAr).HasMaxLength(100).IsRequired();
        builder.Property(r => r.NameEn).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(r => r.Code).IsUnique();
    }
}
