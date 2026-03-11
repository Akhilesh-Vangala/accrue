using LedgerFlow.Application.Reporting;
using LedgerFlow.Domain.Invoices;
using LedgerFlow.Domain.Operations;
using LedgerFlow.Domain.Payments;
using LedgerFlow.Domain.Vendors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace LedgerFlow.Application.Abstractions;

/// <summary>Persistence boundary for the application layer; implemented by the EF Core DbContext.</summary>
public interface ILedgerFlowDb
{
    DbSet<Vendor> Vendors { get; }

    DbSet<Invoice> Invoices { get; }

    DbSet<InvoiceEvent> InvoiceEvents { get; }

    DbSet<PaymentRun> PaymentRuns { get; }

    DbSet<Payment> Payments { get; }

    DbSet<Notification> Notifications { get; }

    DbSet<AgingSnapshot> AgingSnapshots { get; }

    DbSet<ReportArchive> ReportArchives { get; }

    /// <summary>Keyless projection of the <c>vw_ApAging</c> SQL view.</summary>
    IQueryable<ApAgingRow> ApAging { get; }

    /// <summary>Keyless projection of the <c>vw_VendorSpend</c> SQL view.</summary>
    IQueryable<VendorSpendRow> VendorSpend { get; }

    /// <summary>Keyless projection of the <c>vw_PaymentRemittance</c> SQL view.</summary>
    IQueryable<PaymentRemittanceRow> PaymentRemittance { get; }

    DatabaseFacade Database { get; }

    EntityEntry<TEntity> Entry<TEntity>(TEntity entity)
        where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
