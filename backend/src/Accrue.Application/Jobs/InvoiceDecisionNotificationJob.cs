using Accrue.Application.Abstractions;
using Accrue.Domain.Invoices;
using Accrue.Domain.Operations;
using Microsoft.EntityFrameworkCore;

namespace Accrue.Application.Jobs;

/// <summary>Fire-and-forget job enqueued after an approval decision so the clerk hears back without blocking the request.</summary>
public sealed class InvoiceDecisionNotificationJob(IAccrueDb db, TimeProvider time)
{
    public async Task RunAsync(int invoiceId, CancellationToken ct)
    {
        var invoice = await db.Invoices.AsNoTracking()
            .Include(i => i.Events)
            .SingleOrDefaultAsync(i => i.Id == invoiceId, ct);
        var decision = invoice?.Events
            .Where(e => e.Type is InvoiceEventType.Approved or InvoiceEventType.Rejected)
            .MaxBy(e => e.OccurredAt);
        if (invoice?.CreatedBy is null || decision is null)
        {
            return;
        }

        var verb = decision.Type == InvoiceEventType.Approved ? "approved" : "rejected";
        var body = $"{decision.Actor} {verb} invoice {invoice.InvoiceNumber}.";
        if (!string.IsNullOrWhiteSpace(decision.Comment))
        {
            body += $" \"{decision.Comment}\"";
        }

        db.Notifications.Add(new Notification(invoice.CreatedBy, $"Invoice {invoice.InvoiceNumber} {verb}", body, $"/invoices/{invoice.Id}", time.GetUtcNow()));
        await db.SaveChangesAsync(ct);
    }
}
