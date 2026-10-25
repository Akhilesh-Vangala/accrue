using Accrue.Domain.Invoices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accrue.Infrastructure.Persistence.Configurations;

internal sealed class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> b)
    {
        b.Property(l => l.Description).HasMaxLength(300).IsRequired();
        b.Property(l => l.GlAccount).HasMaxLength(4).IsFixedLength().IsRequired();
    }
}
