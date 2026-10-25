namespace Accrue.Application.Invoices;

public sealed record InvoiceLineDto(int Id, string Description, string GlAccount, decimal Quantity, decimal UnitPrice, decimal LineTotal);
