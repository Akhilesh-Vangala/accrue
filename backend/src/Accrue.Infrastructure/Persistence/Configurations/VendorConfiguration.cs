using Accrue.Domain.Vendors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accrue.Infrastructure.Persistence.Configurations;

internal sealed class VendorConfiguration : IEntityTypeConfiguration<Vendor>
{
    public void Configure(EntityTypeBuilder<Vendor> b)
    {
        b.Property(v => v.Name).HasMaxLength(200).IsRequired();
        b.Property(v => v.TaxId).HasMaxLength(20).IsRequired();
        b.Property(v => v.Email).HasMaxLength(256).IsRequired();
        b.Property(v => v.Category).HasMaxLength(50).IsRequired();
        b.Property(v => v.CreatedBy).HasMaxLength(256);
        b.Property(v => v.UpdatedBy).HasMaxLength(256);
        b.Property(v => v.RowVersion).IsRowVersion();
        b.HasIndex(v => v.TaxId).IsUnique();
        b.HasIndex(v => v.Name);
        b.HasMany(v => v.Invoices).WithOne(i => i.Vendor).HasForeignKey(i => i.VendorId).OnDelete(DeleteBehavior.Restrict);
        b.Navigation(v => v.Invoices).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
