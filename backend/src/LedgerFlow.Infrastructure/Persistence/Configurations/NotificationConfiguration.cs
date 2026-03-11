using LedgerFlow.Domain.Operations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerFlow.Infrastructure.Persistence.Configurations;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.Property(n => n.Audience).HasMaxLength(256).IsRequired();
        b.Property(n => n.Title).HasMaxLength(200).IsRequired();
        b.Property(n => n.Body).HasMaxLength(1000).IsRequired();
        b.Property(n => n.Link).HasMaxLength(300);
        b.HasIndex(n => new { n.Audience, n.CreatedAt });
    }
}
