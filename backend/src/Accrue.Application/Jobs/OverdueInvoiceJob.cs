using System.Globalization;
using Accrue.Application.Abstractions;
using Accrue.Domain.Common;
using Accrue.Domain.Invoices;
using Accrue.Domain.Operations;
using Microsoft.EntityFrameworkCore;

namespace Accrue.Application.Jobs;

public sealed class OverdueInvoiceJob(IAccrueDb db, TimeProvider time)
{
    /// <returns>Number of invoices newly flagged overdue.</returns>
    public async Task<int> RunAsync(CancellationToken ct)
    {
        var now = time.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var candidates = await db.Invoices
            .Where(i => i.IsOverdue || (i.DueDate < today && (i.Status == InvoiceStatus.Submitted || i.Status == InvoiceStatus.Approved || i.Status == InvoiceStatus.Scheduled)))
            .ToListAsync(ct);

        var newlyOverdue = candidates.Where(i => i.RefreshOverdue(today, now) && i.IsOverdue).ToList();
        if (newlyOverdue.Count > 0)
        {
            var amount = newlyOverdue.Sum(i => i.Outstanding);
            foreach (var audience in new[] { Roles.Clerk, Roles.Controller })
            {
                db.Notifications.Add(new Notification(
                    audience,
                    $"{newlyOverdue.Count} invoice(s) went overdue",
                    $"{amount.ToString("C", CultureInfo.GetCultureInfo("en-US"))} is now past due. Oldest: {newlyOverdue.MinBy(i => i.DueDate)!.InvoiceNumber}.",
                    "/invoices?overdue=true",
                    now));
            }
        }

        await db.SaveChangesAsync(ct);
        return newlyOverdue.Count;
    }
}
