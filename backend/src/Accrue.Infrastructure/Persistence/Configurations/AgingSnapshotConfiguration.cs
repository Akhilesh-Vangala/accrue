using Accrue.Domain.Operations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accrue.Infrastructure.Persistence.Configurations;

internal sealed class AgingSnapshotConfiguration : IEntityTypeConfiguration<AgingSnapshot>
{
    public void Configure(EntityTypeBuilder<AgingSnapshot> b)
    {
        b.Property(s => s.Bucket).HasMaxLength(20).IsRequired();
        b.HasIndex(s => new { s.SnapshotDate, s.Bucket }).IsUnique();
    }
}
