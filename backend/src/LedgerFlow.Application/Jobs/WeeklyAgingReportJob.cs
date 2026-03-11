using LedgerFlow.Application.Abstractions;
using LedgerFlow.Application.Reporting;
using LedgerFlow.Domain.Common;
using LedgerFlow.Domain.Operations;

namespace LedgerFlow.Application.Jobs;

public sealed class WeeklyAgingReportJob(ILedgerFlowDb db, ReportService reports, TimeProvider time)
{
    public async Task<int> RunAsync(CancellationToken ct)
    {
        var archiveId = await reports.RenderAndArchiveAsync(ReportNames.ApAging, new Dictionary<string, object?>(), SystemActors.ReportJob, ct);
        db.Notifications.Add(new Notification(
            Roles.Controller,
            "Weekly AP aging report filed",
            "This week's AP aging report is in the report archive.",
            "/reports",
            time.GetUtcNow()));
        await db.SaveChangesAsync(ct);
        return archiveId;
    }
}
