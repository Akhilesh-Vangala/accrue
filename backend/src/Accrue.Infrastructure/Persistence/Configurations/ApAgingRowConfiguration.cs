using Accrue.Application.Reporting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accrue.Infrastructure.Persistence.Configurations;

internal sealed class ApAgingRowConfiguration : IEntityTypeConfiguration<ApAgingRow>
{
    public void Configure(EntityTypeBuilder<ApAgingRow> b) => b.HasNoKey().ToView("vw_ApAging");
}
