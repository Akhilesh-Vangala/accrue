namespace LedgerFlow.Application.Extraction;

public sealed record VendorCandidate(int Id, string Name, string TaxId, string Category, bool IsActive);
