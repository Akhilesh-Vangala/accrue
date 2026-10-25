using Accrue.Domain.Invoices;

namespace Accrue.Application.Invoices;

public sealed record SaveInvoiceRequest(
    int VendorId,
    string InvoiceNumber,
    DateOnly InvoiceDate,
    decimal TaxAmount,
    string? Notes,
    IReadOnlyList<InvoiceLineInput> Lines,
    string? RowVersion = null);
