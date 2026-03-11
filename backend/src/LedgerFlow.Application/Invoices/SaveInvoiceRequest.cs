using LedgerFlow.Domain.Invoices;

namespace LedgerFlow.Application.Invoices;

public sealed record SaveInvoiceRequest(
    int VendorId,
    string InvoiceNumber,
    DateOnly InvoiceDate,
    decimal TaxAmount,
    string? Notes,
    IReadOnlyList<InvoiceLineInput> Lines,
    string? RowVersion = null);
