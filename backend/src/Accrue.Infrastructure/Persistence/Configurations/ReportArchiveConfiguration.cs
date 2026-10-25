using Accrue.Domain.Operations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accrue.Infrastructure.Persistence.Configurations;

internal sealed class ReportArchiveConfiguration : IEntityTypeConfiguration<ReportArchive>
{
    public void Configure(EntityTypeBuilder<ReportArchive> b)
    {
        b.Property(r => r.ReportName).HasMaxLength(100).IsRequired();
        b.Property(r => r.FileName).HasMaxLength(200).IsRequired();
        b.Property(r => r.ContentType).HasMaxLength(100).IsRequired();
        b.Property(r => r.GeneratedBy).HasMaxLength(256).IsRequired();
        b.HasIndex(r => r.GeneratedAt);
    }
}
