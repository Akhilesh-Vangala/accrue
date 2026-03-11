namespace LedgerFlow.Domain.Invoices;

public sealed record InvoiceLineInput(string Description, string GlAccount, decimal Quantity, decimal UnitPrice);
