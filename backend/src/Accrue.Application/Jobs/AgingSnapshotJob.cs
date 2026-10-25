using Accrue.Application.Abstractions;
using Accrue.Application.Dashboard;
using Accrue.Domain.Operations;
using Microsoft.EntityFrameworkCore;

namespace Accrue.Application.Jobs;

public sealed class AgingSnapshotJob(IAccrueDb db, TimeProvider time)
{
    /// <summary>Replaces today's snapshot so re-runs and retries never double count.</summary>
    public async Task<int> RunAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        var rows = await db.ApAging
            .GroupBy(a => new { a.Bucket, a.BucketOrder })
            .Select(g => new { g.Key.Bucket, g.Key.BucketOrder, Count = g.Count(), Amount = g.Sum(a => a.Outstanding) })
            .ToListAsync(ct);

        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            await db.AgingSnapshots.Where(s => s.SnapshotDate == today).ExecuteDeleteAsync(ct);
            foreach (var (bucket, order) in DashboardService.Buckets)
            {
                var row = rows.FirstOrDefault(r => r.Bucket == bucket);
                db.AgingSnapshots.Add(new AgingSnapshot(today, bucket, order, row?.Count ?? 0, row?.Amount ?? 0));
            }

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        });

        return DashboardService.Buckets.Count;
    }
}
