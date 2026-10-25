using Accrue.Application.Abstractions;
using Accrue.Domain.Invoices;
using Accrue.Domain.Payments;
using Microsoft.EntityFrameworkCore;

namespace Accrue.Application.Dashboard;

public sealed class DashboardService(IAccrueDb db, TimeProvider time)
{
    public static readonly IReadOnlyList<(string Bucket, int Order)> Buckets =
        [("Current", 0), ("1-30", 1), ("31-60", 2), ("61-90", 3), ("90+", 4)];

    public async Task<DashboardDto> GetAsync(CancellationToken ct)
    {
        var now = time.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var invoices = db.Invoices.AsNoTracking();
        var open = invoices.Where(i => i.Status == InvoiceStatus.Submitted || i.Status == InvoiceStatus.Approved || i.Status == InvoiceStatus.Scheduled);
        var last30 = now.AddDays(-30);

        var kpis = new KpiDto(
            await open.SumAsync(i => i.Total - i.AmountPaid, ct),
            await open.CountAsync(ct),
            await open.Where(i => i.IsOverdue).SumAsync(i => i.Total - i.AmountPaid, ct),
            await open.CountAsync(i => i.IsOverdue, ct),
            await invoices.CountAsync(i => i.Status == InvoiceStatus.Submitted, ct),
            await invoices.Where(i => i.Status == InvoiceStatus.Approved && i.PaymentRunId == null).SumAsync(i => i.Total, ct),
            await db.PaymentRuns.Where(r => r.Status == PaymentRunStatus.Released && r.ReleasedAt >= last30).SumAsync(r => r.Total, ct),
            await AverageApprovalHoursAsync(now.AddDays(-90), ct));

        var agingRows = await db.ApAging
            .GroupBy(a => new { a.Bucket, a.BucketOrder })
            .Select(g => new AgingBucketDto(g.Key.Bucket, g.Key.BucketOrder, g.Count(), g.Sum(a => a.Outstanding)))
            .ToListAsync(ct);
        var aging = Buckets
            .Select(b => agingRows.FirstOrDefault(r => r.Bucket == b.Bucket) ?? new AgingBucketDto(b.Bucket, b.Order, 0, 0))
            .ToList();

        var topVendors = (await open
            .GroupBy(i => new { i.VendorId, i.Vendor!.Name })
            .Select(g => new { g.Key.VendorId, g.Key.Name, Balance = g.Sum(i => i.Total - i.AmountPaid), Count = g.Count() })
            .OrderByDescending(v => v.Balance)
            .Take(6)
            .ToListAsync(ct))
            .Select(v => new VendorBalanceDto(v.VendorId, v.Name, v.Balance, v.Count))
            .ToList();

        var since = new DateOnly(today.Year, today.Month, 1).AddMonths(-11);
        var spendRows = await db.VendorSpend
            .Where(s => s.PeriodStart >= since)
            .GroupBy(s => s.PeriodStart)
            .Select(g => new MonthlySpendDto(g.Key, g.Sum(s => s.TotalAmount), g.Sum(s => s.InvoiceCount)))
            .ToListAsync(ct);
        var monthly = Enumerable.Range(0, 12)
            .Select(m => since.AddMonths(m))
            .Select(m => spendRows.FirstOrDefault(r => r.Month == m) ?? new MonthlySpendDto(m, 0, 0))
            .ToList();

        var trendSince = today.AddDays(-30);
        var trend = await db.AgingSnapshots.AsNoTracking()
            .Where(s => s.SnapshotDate >= trendSince)
            .GroupBy(s => s.SnapshotDate)
            .Select(g => new
            {
                Date = g.Key,
                Current = g.Sum(s => s.BucketOrder == 0 ? s.Amount : 0m),
                Overdue = g.Sum(s => s.BucketOrder > 0 ? s.Amount : 0m),
            })
            .OrderBy(p => p.Date)
            .ToListAsync(ct);

        var statusCounts = await invoices
            .GroupBy(i => i.Status)
            .Select(g => new StatusCountDto(g.Key, g.Count()))
            .ToListAsync(ct);

        return new DashboardDto(
            kpis,
            aging,
            topVendors,
            monthly,
            trend.Select(p => new AgingTrendPointDto(p.Date, p.Current, p.Overdue)).ToList(),
            statusCounts.OrderBy(s => s.Status).ToList());
    }

    private async Task<double?> AverageApprovalHoursAsync(DateTimeOffset since, CancellationToken ct)
    {
        var pairs = await db.InvoiceEvents.AsNoTracking()
            .Where(e => e.OccurredAt >= since && (e.Type == InvoiceEventType.Submitted || e.Type == InvoiceEventType.Approved))
            .Select(e => new { e.InvoiceId, e.Type, e.OccurredAt })
            .ToListAsync(ct);

        var durations = pairs
            .GroupBy(e => e.InvoiceId)
            .Select(g => new
            {
                Submitted = g.Where(e => e.Type == InvoiceEventType.Submitted).Select(e => (DateTimeOffset?)e.OccurredAt).Max(),
                Approved = g.Where(e => e.Type == InvoiceEventType.Approved).Select(e => (DateTimeOffset?)e.OccurredAt).Max(),
            })
            .Where(x => x.Submitted is not null && x.Approved is not null && x.Approved >= x.Submitted)
            .Select(x => (x.Approved!.Value - x.Submitted!.Value).TotalHours)
            .ToList();

        return durations.Count == 0 ? null : Math.Round(durations.Average(), 1);
    }
}
