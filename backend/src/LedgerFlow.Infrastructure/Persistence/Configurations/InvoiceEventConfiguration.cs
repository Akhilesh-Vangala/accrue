using LedgerFlow.Domain.Invoices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerFlow.Infrastructure.Persistence.Configurations;

internal sealed class InvoiceEventConfiguration : IEntityTypeConfiguration<InvoiceEvent>
{
    public void Configure(EntityTypeBuilder<InvoiceEvent> b)
    {
        b.Property(e => e.Type).HasConversion<string>().HasMaxLength(20);
        b.Property(e => e.Actor).HasMaxLength(256).IsRequired();
        b.Property(e => e.Comment).HasMaxLength(1000);
        b.HasIndex(e => new { e.InvoiceId, e.OccurredAt });
    }
}
