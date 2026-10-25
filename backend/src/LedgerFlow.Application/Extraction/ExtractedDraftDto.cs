using LedgerFlow.Domain.Invoices;

namespace LedgerFlow.Application.Extraction;

/// <summary>Values to prefill the invoice form with. Saving still goes through the normal create endpoint and its rules.</summary>
public sealed record ExtractedDraftDto(
    int? VendorId,
    string? VendorName,
    string? InvoiceNumber,
    DateOnly? InvoiceDate,
    decimal TaxAmount,
    decimal? StatedTotal,
    IReadOnlyList<InvoiceLineInput> Lines);
