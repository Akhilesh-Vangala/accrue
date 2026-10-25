using Accrue.Application.Extraction;

namespace Accrue.IntegrationTests;

/// <summary>Stands in for the Claude reader so HTTP tests exercise everything after the model call deterministically.</summary>
public sealed class FakeInvoiceDocumentReader : IInvoiceDocumentReader
{
    public ExtractedInvoice? Next { get; set; }

    public InvoiceDocument? LastDocument { get; private set; }

    public bool IsAvailable => true;

    public string Model => "fake-reader";

    public Task<InvoiceReading> ReadAsync(InvoiceDocument document, CancellationToken ct)
    {
        LastDocument = document;
        var invoice = Next ?? throw new InvalidOperationException("Set Next before calling the extraction endpoint.");
        return Task.FromResult(new InvoiceReading(invoice, new ReadingUsage(Model, 1_000, 200, 5)));
    }
}
