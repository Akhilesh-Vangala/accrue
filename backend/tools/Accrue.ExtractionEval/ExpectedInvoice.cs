namespace Accrue.ExtractionEval;

internal sealed record ExpectedInvoice(
    string VendorName,
    string VendorTaxId,
    string InvoiceNumber,
    DateOnly InvoiceDate,
    DateOnly DueDate,
    string Currency,
    decimal? Subtotal,
    decimal? TaxAmount,
    decimal Total,
    IReadOnlyList<ExpectedLine> LineItems);
