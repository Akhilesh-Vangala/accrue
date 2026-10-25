namespace LedgerFlow.Application.Extraction;

/// <summary>An uploaded vendor invoice: a PDF or a scanned image.</summary>
public sealed record InvoiceDocument(string FileName, string MediaType, byte[] Content);
