using System.Globalization;

namespace LedgerFlow.Application.Extraction;

/// <summary>
/// Deterministic checks on what the model read. The model is never asked to do arithmetic it could get wrong
/// silently: every total is recomputed here, so a misread digit or a vendor's own billing error surfaces as a finding.
/// </summary>
public static class ExtractionChecks
{
    /// <summary>Rounding slack, in dollars, before two amounts count as different.</summary>
    public const decimal Tolerance = 0.01m;

    public static IReadOnlyList<ExtractionFinding> Run(ExtractedInvoice invoice, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        var findings = new List<ExtractionFinding>();

        if (string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
        {
            findings.Add(new("missing_invoice_number", "error", "No invoice number was found on the document.", "invoiceNumber"));
        }

        if (invoice.InvoiceDate is null)
        {
            findings.Add(new("missing_invoice_date", "error", "No invoice date was found on the document.", "invoiceDate"));
        }
        else if (invoice.InvoiceDate > today)
        {
            findings.Add(new("future_invoice_date", "error", $"The invoice is dated {invoice.InvoiceDate:yyyy-MM-dd}, which is in the future.", "invoiceDate"));
        }

        if (invoice.Lines.Count == 0)
        {
            findings.Add(new("no_lines", "error", "No line items were found on the document.", "lines"));
        }

        if (invoice.Currency is { Length: > 0 } currency && !string.Equals(currency, "USD", StringComparison.OrdinalIgnoreCase))
        {
            findings.Add(new("foreign_currency", "error", $"The invoice is in {currency}; LedgerFlow pays in USD. Convert before entry."));
        }

        for (var i = 0; i < invoice.Lines.Count; i++)
        {
            var line = invoice.Lines[i];
            var computed = decimal.Round(line.Quantity * line.UnitPrice, 2, MidpointRounding.AwayFromZero);
            if (Math.Abs(computed - line.Amount) > Tolerance)
            {
                findings.Add(new(
                    "line_amount_mismatch",
                    "warning",
                    $"Line {i + 1}: {Quantity(line.Quantity)} × {Money(line.UnitPrice)} = {Money(computed)}, but the document prints {Money(line.Amount)}.",
                    $"lines.{i}"));
            }
        }

        var linesTotal = invoice.Lines.Sum(l => decimal.Round(l.Quantity * l.UnitPrice, 2, MidpointRounding.AwayFromZero));
        if (invoice.Subtotal is { } subtotal && invoice.Lines.Count > 0 && Math.Abs(subtotal - linesTotal) > Tolerance)
        {
            findings.Add(new("subtotal_mismatch", "warning", $"Lines add up to {Money(linesTotal)}, but the printed subtotal is {Money(subtotal)}."));
        }

        if (invoice.Total is { } total)
        {
            var expected = (invoice.Subtotal ?? linesTotal) + (invoice.TaxAmount ?? 0m);
            if (Math.Abs(expected - total) > Tolerance)
            {
                findings.Add(new("total_mismatch", "error", $"Subtotal plus tax is {Money(expected)}, but the printed total is {Money(total)}.", "taxAmount"));
            }
        }

        return findings;
    }

    private static string Money(decimal value) => (value < 0 ? "-$" : "$") + Math.Abs(value).ToString("#,0.00", CultureInfo.InvariantCulture);

    private static string Quantity(decimal value) => value.ToString("#,0.####", CultureInfo.InvariantCulture);
}
