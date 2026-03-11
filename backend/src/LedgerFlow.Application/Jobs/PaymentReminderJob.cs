using System.Globalization;
using LedgerFlow.Application.Abstractions;
using LedgerFlow.Domain.Common;
using LedgerFlow.Domain.Invoices;
using LedgerFlow.Domain.Operations;
using Microsoft.EntityFrameworkCore;

namespace LedgerFlow.Application.Jobs;

public sealed class PaymentReminderJob(ILedgerFlowDb db, TimeProvider time)
{
    public const int WindowDays = 7;

    /// <returns>Number of invoices in the reminder, or 0 when nothing was sent.</returns>
    public async Task<int> RunAsync(CancellationToken ct)
    {
        var now = time.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var horizon = today.AddDays(WindowDays);
        var due = await db.Invoices.AsNoTracking()
            .Where(i => i.Status == InvoiceStatus.Approved && i.PaymentRunId == null && i.DueDate <= horizon)
            .Select(i => new { i.Total, i.AmountPaid })
            .ToListAsync(ct);
        if (due.Count == 0)
        {
            return 0;
        }

        var title = $"Payments due by {horizon:MMM d}";
        var startOfDay = new DateTimeOffset(today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        if (await db.Notifications.AnyAsync(n => n.Title == title && n.CreatedAt >= startOfDay, ct))
        {
            return 0;
        }

        var amount = due.Sum(i => i.Total - i.AmountPaid);
        db.Notifications.Add(new Notification(
            Roles.Controller,
            title,
            $"{due.Count} approved invoice(s) totalling {amount.ToString("C", CultureInfo.GetCultureInfo("en-US"))} are not in a payment run yet.",
            "/payment-runs/new",
            now));
        await db.SaveChangesAsync(ct);
        return due.Count;
    }
}
