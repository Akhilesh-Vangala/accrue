namespace Accrue.Application.Extraction;

/// <summary>Reads structured invoice fields out of a document. Implemented with Claude in Infrastructure.</summary>
public interface IInvoiceDocumentReader
{
    /// <summary>False when no model credentials are configured; the upload option is hidden in that case.</summary>
    bool IsAvailable { get; }

    string Model { get; }

    Task<InvoiceReading> ReadAsync(InvoiceDocument document, CancellationToken ct);
}
