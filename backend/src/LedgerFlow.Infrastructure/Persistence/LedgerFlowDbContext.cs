using LedgerFlow.Application.Abstractions;
using LedgerFlow.Application.Reporting;
using LedgerFlow.Domain.Invoices;
using LedgerFlow.Domain.Operations;
using LedgerFlow.Domain.Payments;
using LedgerFlow.Domain.Vendors;
using LedgerFlow.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LedgerFlow.Infrastructure.Persistence;

public sealed class LedgerFlowDbContext(DbContextOptions<LedgerFlowDbContext> options)
    : IdentityDbContext<AppUser>(options), ILedgerFlowDb
{
    public DbSet<Vendor> Vendors => Set<Vendor>();

    public DbSet<Invoice> Invoices => Set<Invoice>();

    public DbSet<InvoiceEvent> InvoiceEvents => Set<InvoiceEvent>();

    public DbSet<PaymentRun> PaymentRuns => Set<PaymentRun>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<AgingSnapshot> AgingSnapshots => Set<AgingSnapshot>();

    public DbSet<ReportArchive> ReportArchives => Set<ReportArchive>();

    public IQueryable<ApAgingRow> ApAging => Set<ApAgingRow>().AsNoTracking();

    public IQueryable<VendorSpendRow> VendorSpend => Set<VendorSpendRow>().AsNoTracking();

    public IQueryable<PaymentRemittanceRow> PaymentRemittance => Set<PaymentRemittanceRow>().AsNoTracking();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(LedgerFlowDbContext).Assembly);
    }
}
