using LedgerFlow.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerFlow.Infrastructure.Persistence.Configurations;

internal sealed class PaymentRunConfiguration : IEntityTypeConfiguration<PaymentRun>
{
    public void Configure(EntityTypeBuilder<PaymentRun> b)
    {
        b.Property(r => r.Name).HasMaxLength(100).IsRequired();
        b.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(r => r.CreatedBy).HasMaxLength(256);
        b.Property(r => r.UpdatedBy).HasMaxLength(256);
        b.Property(r => r.ReleasedBy).HasMaxLength(256);
        b.HasMany(r => r.Payments).WithOne().HasForeignKey(p => p.PaymentRunId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(r => r.Payments).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
