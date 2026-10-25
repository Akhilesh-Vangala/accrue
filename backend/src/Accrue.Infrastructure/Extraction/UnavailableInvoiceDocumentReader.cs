using Accrue.Application.Extraction;

namespace Accrue.Infrastructure.Extraction;

/// <summary>Registered when no Anthropic credentials are configured; the UI hides document upload.</summary>
public sealed class UnavailableInvoiceDocumentReader : IInvoiceDocumentReader
{
    public bool IsAvailable => false;

    public string Model => string.Empty;

    public Task<InvoiceReading> ReadAsync(InvoiceDocument document, CancellationToken ct) =>
        throw new InvalidOperationException("Document extraction is not configured.");
}
