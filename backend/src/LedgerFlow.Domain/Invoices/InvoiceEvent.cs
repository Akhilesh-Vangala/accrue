namespace LedgerFlow.Domain.Invoices;

/// <summary>Append-only audit trail entry for an invoice: who did what, when, and why.</summary>
public sealed class InvoiceEvent
{
    private InvoiceEvent()
    {
        Actor = string.Empty;
    }

    internal InvoiceEvent(InvoiceEventType type, string actor, string? comment, DateTimeOffset occurredAt)
    {
        Type = type;
        Actor = actor;
        Comment = comment;
        OccurredAt = occurredAt;
    }

    public int Id { get; private set; }

    public int InvoiceId { get; private set; }

    public InvoiceEventType Type { get; private set; }

    public string Actor { get; private set; }

    public string? Comment { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }
}
