using Accrue.Application.Abstractions;
using Accrue.Application.Reporting;
using Accrue.Domain.Common;
using Accrue.Domain.Operations;

namespace Accrue.Application.Jobs;

public sealed class WeeklyAgingReportJob(IAccrueDb db, ReportService reports, TimeProvider time)
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
