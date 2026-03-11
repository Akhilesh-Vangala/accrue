using LedgerFlow.Application.Reporting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerFlow.Infrastructure.Persistence.Configurations;

internal sealed class VendorSpendRowConfiguration : IEntityTypeConfiguration<VendorSpendRow>
{
    public void Configure(EntityTypeBuilder<VendorSpendRow> b) => b.HasNoKey().ToView("vw_VendorSpend");
}
