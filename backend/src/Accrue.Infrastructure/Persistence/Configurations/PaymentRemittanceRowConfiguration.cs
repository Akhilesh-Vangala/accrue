using Accrue.Application.Reporting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accrue.Infrastructure.Persistence.Configurations;

internal sealed class PaymentRemittanceRowConfiguration : IEntityTypeConfiguration<PaymentRemittanceRow>
{
    public void Configure(EntityTypeBuilder<PaymentRemittanceRow> b) => b.HasNoKey().ToView("vw_PaymentRemittance");
}
