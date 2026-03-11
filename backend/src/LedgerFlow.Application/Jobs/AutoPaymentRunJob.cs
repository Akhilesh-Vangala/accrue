using LedgerFlow.Application.Abstractions;
using LedgerFlow.Application.Payments;
using LedgerFlow.Domain.Common;
using LedgerFlow.Domain.Invoices;
using LedgerFlow.Domain.Operations;
using LedgerFlow.Domain.Payments;
using Microsoft.EntityFrameworkCore;

namespace LedgerFlow.Application.Jobs;

public sealed class AutoPaymentRunJob(ILedgerFlowDb db, PaymentRunService runs, TimeProvider time)
{
    public const int HorizonDays = 10;

    /// <returns>The drafted run id, or null when there was nothing to draft or a draft already awaits release.</returns>
    public async Task<int?> RunAsync(CancellationToken ct)
    {
        if (await db.PaymentRuns.AnyAsync(r => r.Status == PaymentRunStatus.Draft, ct))
        {
            return null;
        }

        var now = time.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var ids = await db.Invoices
            .Where(i => i.Status == InvoiceStatus.Approved && i.PaymentRunId == null && i.DueDate <= today.AddDays(HorizonDays))
            .OrderBy(i => i.DueDate)
            .Select(i => i.Id)
            .ToListAsync(ct);
        if (ids.Count == 0)
        {
            return null;
        }

        var paymentDate = NextBusinessDay(today.AddDays(2));
        var runId = await runs.DraftAsync($"Auto run {today:yyyy-MM-dd}", paymentDate, ids, SystemActors.PaymentRunJob, ct);
        db.Notifications.Add(new Notification(
            Roles.Controller,
            "Payment run ready for release",
            $"{ids.Count} approved invoice(s) were drafted into a run paying on {paymentDate:MMM d}. Review and release it.",
            $"/payment-runs/{runId}",
            now));
        await db.SaveChangesAsync(ct);
        return runId;
    }

    private static DateOnly NextBusinessDay(DateOnly date) => date.DayOfWeek switch
    {
        DayOfWeek.Saturday => date.AddDays(2),
        DayOfWeek.Sunday => date.AddDays(1),
        _ => date,
    };
}
