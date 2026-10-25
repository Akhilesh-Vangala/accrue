using LedgerFlow.Application.Extraction;

namespace LedgerFlow.ExtractionEval;

/// <summary>
/// Compares a reading with its label. Text fields are compared after the same normalization the app uses for
/// matching (vendor names, tax-ID digits); amounts must match to the cent; lines are aligned by position.
/// </summary>
internal static class Scorer
{
    /// <summary>Fixed "today" for the deterministic checks, after every invoice date in the set.</summary>
    private static readonly DateOnly EvalToday = new(2026, 12, 31);

    public static readonly string[] HeaderFields =
        ["vendor_name", "vendor_tax_id", "invoice_number", "invoice_date", "due_date", "currency", "subtotal", "tax_amount", "total"];

    public static CaseResult Score(Label label, InvoiceReading reading)
    {
        var got = reading.Invoice;
        var want = label.Expected;
        var mismatches = new List<string>();

        bool Check(string field, bool ok, object? expected, object? actual)
        {
            if (!ok)
            {
                mismatches.Add($"{field}: expected {expected ?? "null"}, got {actual ?? "null"}");
            }

            return ok;
        }

        var fields = new Dictionary<string, bool>
        {
            ["vendor_name"] = Check("vendor_name", VendorMatcher.NormalizeName(got.VendorName) == VendorMatcher.NormalizeName(want.VendorName), want.VendorName, got.VendorName),
            ["vendor_tax_id"] = Check("vendor_tax_id", Digits(got.VendorTaxId) == Digits(want.VendorTaxId), want.VendorTaxId, got.VendorTaxId),
            ["invoice_number"] = Check("invoice_number", string.Equals(got.InvoiceNumber?.Trim(), want.InvoiceNumber, StringComparison.OrdinalIgnoreCase), want.InvoiceNumber, got.InvoiceNumber),
            ["invoice_date"] = Check("invoice_date", got.InvoiceDate == want.InvoiceDate, want.InvoiceDate, got.InvoiceDate),
            ["due_date"] = Check("due_date", got.DueDate == want.DueDate, want.DueDate, got.DueDate),
            ["currency"] = Check("currency", string.Equals(got.Currency, want.Currency, StringComparison.OrdinalIgnoreCase), want.Currency, got.Currency),
            ["subtotal"] = Check("subtotal", Same(got.Subtotal, want.Subtotal), want.Subtotal, got.Subtotal),
            ["tax_amount"] = Check("tax_amount", Same(got.TaxAmount, want.TaxAmount), want.TaxAmount, got.TaxAmount),
            ["total"] = Check("total", Same(got.Total, want.Total), want.Total, got.Total),
        };

        var lineCountCorrect = Check("line_count", got.Lines.Count == want.LineItems.Count, want.LineItems.Count, got.Lines.Count);
        int correctLines = 0, correctGl = 0;
        for (var i = 0; i < Math.Min(got.Lines.Count, want.LineItems.Count); i++)
        {
            var (g, w) = (got.Lines[i], want.LineItems[i]);
            var ok = Text(g.Description) == Text(w.Description) && g.Quantity == w.Quantity && Same(g.UnitPrice, w.UnitPrice) && Same(g.Amount, w.Amount);
            if (Check($"line {i + 1}", ok, $"{w.Description} {w.Quantity} x {w.UnitPrice} = {w.Amount}", $"{g.Description} {g.Quantity} x {g.UnitPrice} = {g.Amount}"))
            {
                correctLines++;
            }

            if (g.GlAccount == w.GlAccount)
            {
                correctGl++;
            }
        }

        var raised = ExtractionChecks.Run(got, EvalToday).Where(f => f.Severity != "info").Select(f => f.Code).Distinct().ToList();
        return new CaseResult(
            label.Id, label.Tags, null, fields, want.LineItems.Count, correctLines, correctGl, lineCountCorrect,
            label.ExpectedFindings, raised, mismatches, reading.Usage.LatencyMs, reading.Usage.InputTokens, reading.Usage.OutputTokens, reading.Usage.Model);
    }

    public static CaseResult Failed(Label label, string error) => new(
        label.Id, label.Tags, error, HeaderFields.ToDictionary(f => f, _ => false), label.Expected.LineItems.Count, 0, 0, false,
        label.ExpectedFindings, [], [error], 0, 0, 0, null);

    private static bool Same(decimal? a, decimal? b) => a is null || b is null ? a == b : Math.Abs(a.Value - b.Value) < 0.005m;

    private static string Digits(string? s) => s is null ? string.Empty : new string(s.Where(char.IsDigit).ToArray());

    private static string Text(string s) => string.Join(' ', s.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
}
