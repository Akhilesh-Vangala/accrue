using LedgerFlow.Domain.Invoices;
using LedgerFlow.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerFlow.Infrastructure.Persistence.Configurations;

internal sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> b)
    {
        b.Property(i => i.InvoiceNumber).HasMaxLength(50).IsRequired();
        b.Property(i => i.Currency).HasMaxLength(3).IsFixedLength().IsRequired();
        b.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(i => i.Notes).HasMaxLength(1000);
        b.Property(i => i.CreatedBy).HasMaxLength(256);
        b.Property(i => i.UpdatedBy).HasMaxLength(256);
        b.Property(i => i.RowVersion).IsRowVersion();

        // Same vendor + same invoice number is the classic duplicate-payment risk; enforce it in the database too.
        b.HasIndex(i => new { i.VendorId, i.InvoiceNumber }).IsUnique();
        b.HasIndex(i => new { i.Status, i.DueDate });
        b.HasIndex(i => i.IsOverdue).HasFilter("[IsOverdue] = 1");

        b.HasOne<PaymentRun>().WithMany().HasForeignKey(i => i.PaymentRunId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(i => i.Lines).WithOne().HasForeignKey(l => l.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(i => i.Events).WithOne().HasForeignKey(e => e.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(i => i.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(i => i.Events).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
