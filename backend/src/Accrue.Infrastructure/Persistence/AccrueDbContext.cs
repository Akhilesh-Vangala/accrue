using Accrue.Application.Abstractions;
using Accrue.Application.Reporting;
using Accrue.Domain.Invoices;
using Accrue.Domain.Operations;
using Accrue.Domain.Payments;
using Accrue.Domain.Vendors;
using Accrue.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Accrue.Infrastructure.Persistence;

public sealed class AccrueDbContext(DbContextOptions<AccrueDbContext> options)
    : IdentityDbContext<AppUser>(options), IAccrueDb
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
        builder.ApplyConfigurationsFromAssembly(typeof(AccrueDbContext).Assembly);
    }
}
