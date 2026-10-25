using System.Globalization;
using System.Text.Json;
using Accrue.Application.Extraction;

namespace Accrue.Infrastructure.Extraction;

/// <summary>Maps the schema-constrained JSON the model returns onto <see cref="ExtractedInvoice"/>.</summary>
public static class ExtractionJson
{
    public static ExtractedInvoice Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var lines = new List<ExtractedLine>();
        if (root.TryGetProperty("line_items", out var items) && items.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in items.EnumerateArray())
            {
                lines.Add(new ExtractedLine(
                    String(item, "description") ?? string.Empty,
                    Number(item, "quantity") ?? 1m,
                    Number(item, "unit_price") ?? 0m,
                    Number(item, "amount") ?? 0m,
                    String(item, "gl_account")));
            }
        }

        var uncertain = new List<string>();
        if (root.TryGetProperty("uncertain_fields", out var fields) && fields.ValueKind == JsonValueKind.Array)
        {
            uncertain.AddRange(fields.EnumerateArray().Select(f => f.GetString()).OfType<string>());
        }

        return new ExtractedInvoice(
            String(root, "vendor_name"),
            String(root, "vendor_tax_id"),
            String(root, "invoice_number"),
            Date(root, "invoice_date"),
            Date(root, "due_date"),
            String(root, "currency")?.ToUpperInvariant(),
            Number(root, "subtotal"),
            Number(root, "tax_amount"),
            Number(root, "total"),
            lines,
            uncertain);
    }

    private static string? String(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && value.GetString() is { Length: > 0 } s
            ? s.Trim()
            : null;

    private static decimal? Number(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var d) ? d : null;

    private static DateOnly? Date(JsonElement element, string name) =>
        String(element, name) is { } s && DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
}
