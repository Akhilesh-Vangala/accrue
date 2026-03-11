namespace LedgerFlow.Domain.Invoices;

public sealed class InvoiceLine
{
    private InvoiceLine()
    {
        Description = string.Empty;
        GlAccount = string.Empty;
    }

    internal InvoiceLine(string description, string glAccount, decimal quantity, decimal unitPrice)
    {
        Description = description;
        GlAccount = glAccount;
        Quantity = quantity;
        UnitPrice = unitPrice;
        LineTotal = decimal.Round(quantity * unitPrice, 2, MidpointRounding.AwayFromZero);
    }

    public int Id { get; private set; }

    public int InvoiceId { get; private set; }

    public string Description { get; private set; }

    /// <summary>General-ledger account the line posts to, e.g. 6100 (Software).</summary>
    public string GlAccount { get; private set; }

    public decimal Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal LineTotal { get; private set; }
}
