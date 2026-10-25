namespace LedgerFlow.Application.Extraction;

public sealed record InvoiceExtractionDto(
    ExtractedDraftDto Draft,
    string? VendorMatchedBy,
    IReadOnlyList<string> UncertainFields,
    IReadOnlyList<ExtractionFinding> Findings,
    ReadingUsage Usage);
