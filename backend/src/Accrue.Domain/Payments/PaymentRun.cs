using Accrue.Domain.Common;
using Accrue.Domain.Invoices;

namespace Accrue.Domain.Payments;

/// <summary>
/// A batch of approved invoices paid together. Drafted by a person or the weekly job, released by a controller.
/// Release is the only path that marks invoices paid.
/// </summary>
public sealed class PaymentRun : Entity
{
    private readonly List<Payment> _payments = [];

    private PaymentRun()
    {
        Name = string.Empty;
    }

    public PaymentRun(string name, DateOnly paymentDate)
    {
        Name = name;
        PaymentDate = paymentDate;
        Status = PaymentRunStatus.Draft;
    }

    public string Name { get; private set; }

    public DateOnly PaymentDate { get; private set; }

    public PaymentRunStatus Status { get; private set; }

    public decimal Total { get; private set; }

    public string? ReleasedBy { get; private set; }

    public DateTimeOffset? ReleasedAt { get; private set; }

    public IReadOnlyCollection<Payment> Payments => _payments.AsReadOnly();

    public void AddInvoice(Invoice invoice, string actor, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        EnsureDraft();
        if (Id == 0)
        {
            throw new InvalidOperationException("Persist the payment run before adding invoices so they can reference it.");
        }

        if (_payments.Any(p => p.InvoiceId == invoice.Id))
        {
            return;
        }

        invoice.Schedule(Id, actor, now);
        _payments.Add(new Payment(invoice, invoice.Outstanding));
        Total = _payments.Sum(p => p.Amount);
    }

    public void Release(string actor, IReadOnlyCollection<string> actorRoles, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(actorRoles);
        EnsureDraft();
        if (!actorRoles.Contains(Roles.Controller))
        {
            throw new DomainException("release_forbidden", "Only controllers can release a payment run.");
        }

        if (_payments.Count == 0)
        {
            throw new DomainException("payment_run_empty", "A payment run needs at least one invoice.");
        }

        var sequence = 1;
        foreach (var payment in _payments)
        {
            var reference = $"ACH-{PaymentDate:yyyyMMdd}-{Id:D4}-{sequence++:D3}";
            payment.Settle(reference);
            payment.Invoice!.MarkPaid(reference, actor, now);
        }

        Status = PaymentRunStatus.Released;
        ReleasedBy = actor;
        ReleasedAt = now;
    }

    public void Cancel(string actor, DateTimeOffset now)
    {
        EnsureDraft();
        foreach (var payment in _payments)
        {
            payment.Invoice!.Unschedule(actor, now);
        }

        Status = PaymentRunStatus.Cancelled;
    }

    private void EnsureDraft()
    {
        if (Status != PaymentRunStatus.Draft)
        {
            throw new DomainException("payment_run_locked", $"Payment run is {Status} and can no longer change.");
        }
    }
}
