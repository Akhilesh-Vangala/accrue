using Accrue.Application.Extraction;

namespace Accrue.ExtractionEval;

/// <summary>Returns each invoice's label as if it had been read perfectly. Used to validate the harness.</summary>
internal sealed class OracleReader(IReadOnlyList<Label> labels) : IInvoiceDocumentReader
{
    public bool IsAvailable => true;

    public string Model => "oracle";

    public Task<InvoiceReading> ReadAsync(InvoiceDocument document, CancellationToken ct)
    {
        var e = labels.Single(l => l.File == document.FileName).Expected;
        var invoice = new ExtractedInvoice(
            e.VendorName, e.VendorTaxId, e.InvoiceNumber, e.InvoiceDate, e.DueDate, e.Currency, e.Subtotal, e.TaxAmount, e.Total,
            [.. e.LineItems.Select(l => new ExtractedLine(l.Description, l.Quantity, l.UnitPrice, l.Amount, l.GlAccount))],
            []);
        return Task.FromResult(new InvoiceReading(invoice, new ReadingUsage(Model, 0, 0, 0)));
    }
}
