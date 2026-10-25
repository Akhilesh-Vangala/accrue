using System.Text;

namespace LedgerFlow.Application.Extraction;

/// <summary>
/// Resolves the printed vendor against vendor master data. Tax ID wins because it is unique and unambiguous;
/// the name is a fallback after normalizing case, punctuation and legal suffixes ("Contoso Software, Inc." = "Contoso Software").
/// </summary>
public static class VendorMatcher
{
    private static readonly string[] LegalSuffixes = ["llc", "llp", "inc", "incorporated", "ltd", "limited", "corp", "corporation", "co", "company", "plc", "gmbh"];

    public static VendorMatch? Match(ExtractedInvoice invoice, IReadOnlyCollection<VendorCandidate> vendors)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentNullException.ThrowIfNull(vendors);

        var taxId = Digits(invoice.VendorTaxId);
        if (taxId.Length > 0 && vendors.FirstOrDefault(v => Digits(v.TaxId) == taxId) is { } byTaxId)
        {
            return new VendorMatch(byTaxId, "tax_id");
        }

        var name = NormalizeName(invoice.VendorName);
        if (name.Length == 0)
        {
            return null;
        }

        var byName = vendors.Where(v => NormalizeName(v.Name) == name).ToList();
        return byName.Count == 1 ? new VendorMatch(byName[0], "name") : null;
    }

    public static string NormalizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var cleaned = new StringBuilder(name.Length);
        foreach (var c in name.ToLowerInvariant())
        {
            cleaned.Append(char.IsLetterOrDigit(c) ? c : c == '&' ? '&' : ' ');
        }

        var words = cleaned.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        while (words.Count > 1 && LegalSuffixes.Contains(words[^1]))
        {
            words.RemoveAt(words.Count - 1);
        }

        return string.Join(' ', words);
    }

    private static string Digits(string? value) => value is null ? string.Empty : new string(value.Where(char.IsDigit).ToArray());
}
