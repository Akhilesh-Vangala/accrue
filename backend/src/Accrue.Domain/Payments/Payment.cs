using Accrue.Domain.Invoices;

namespace Accrue.Domain.Payments;

public sealed class Payment
{
    private Payment()
    {
    }

    internal Payment(Invoice invoice, decimal amount)
    {
        Invoice = invoice;
        InvoiceId = invoice.Id;
        Amount = amount;
    }

    public int Id { get; private set; }

    public int PaymentRunId { get; private set; }

    public int InvoiceId { get; private set; }

    public Invoice? Invoice { get; private set; }

    public decimal Amount { get; private set; }

    /// <summary>Remittance reference printed on the Telerik remittance advice.</summary>
    public string? Reference { get; private set; }

    internal void Settle(string reference) => Reference = reference;
}
