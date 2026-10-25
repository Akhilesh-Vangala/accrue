namespace Accrue.Application.Extraction;

/// <summary>
/// Fields read from an invoice document, exactly as printed. Nothing here is trusted: the extraction service
/// matches the vendor, checks the arithmetic and looks for duplicates before anything reaches the invoice form.
/// </summary>
public sealed record ExtractedInvoice(
    string? VendorName,
    string? VendorTaxId,
    string? InvoiceNumber,
    DateOnly? InvoiceDate,
    DateOnly? DueDate,
    string? Currency,
    decimal? Subtotal,
    decimal? TaxAmount,
    decimal? Total,
    IReadOnlyList<ExtractedLine> Lines,
    IReadOnlyList<string> UncertainFields);
