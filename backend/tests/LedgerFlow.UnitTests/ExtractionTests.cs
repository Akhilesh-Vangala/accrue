using LedgerFlow.Application.Extraction;
using LedgerFlow.Infrastructure.Extraction;

namespace LedgerFlow.UnitTests;

public class ExtractionTests
{
    private static readonly DateOnly Today = new(2026, 10, 1);

    private static readonly VendorCandidate[] Vendors =
    [
        new(1, "Contoso Software", "11-3551001", "Software", true),
        new(2, "Adatum Legal LLP", "13-9941846", "Professional Services", true),
        new(3, "City Power & Light", "26-5588615", "Utilities", false),
    ];

    private static ExtractedInvoice Invoice(
        string? vendor = "Contoso Software",
        string? taxId = null,
        decimal? subtotal = 300m,
        decimal? tax = 24m,
        decimal? total = 324m,
        string? currency = "USD",
        DateOnly? date = null,
        params ExtractedLine[] lines) =>
        new(vendor, taxId, "INV-77", date ?? new DateOnly(2026, 9, 15), null, currency, subtotal, tax, total,
            lines.Length > 0 ? lines : [new ExtractedLine("Seat subscription", 3, 100m, 300m, "6100")], []);

    [Fact]
    public void Consistent_invoice_has_no_findings()
    {
        Assert.Empty(ExtractionChecks.Run(Invoice(), Today));
    }

    [Fact]
    public void Line_amount_that_disagrees_with_quantity_times_price_is_flagged()
    {
        var findings = ExtractionChecks.Run(Invoice(subtotal: 310m, total: 334m, lines: new ExtractedLine("Seats", 3, 100m, 310m, "6100")), Today);

        var finding = Assert.Single(findings, f => f.Code == "line_amount_mismatch");
        Assert.Equal("lines.0", finding.Field);
    }

    [Fact]
    public void Subtotal_that_disagrees_with_the_lines_is_flagged()
    {
        var findings = ExtractionChecks.Run(Invoice(subtotal: 350m, total: 374m), Today);

        Assert.Contains(findings, f => f.Code == "subtotal_mismatch");
    }

    [Fact]
    public void Total_that_disagrees_with_subtotal_plus_tax_is_an_error()
    {
        var finding = Assert.Single(ExtractionChecks.Run(Invoice(total: 342m), Today));

        Assert.Equal(("total_mismatch", "error"), (finding.Code, finding.Severity));
    }

    [Fact]
    public void Rounding_within_one_cent_is_tolerated()
    {
        Assert.Empty(ExtractionChecks.Run(Invoice(total: 324.01m), Today));
    }

    [Fact]
    public void Future_dates_foreign_currency_and_missing_lines_are_errors()
    {
        var empty = new ExtractedInvoice("Contoso Software", null, "INV-1", Today.AddDays(3), null, "EUR", null, null, null, [], []);

        var codes = ExtractionChecks.Run(empty, Today).Select(f => f.Code).ToList();

        Assert.Equal(["future_invoice_date", "no_lines", "foreign_currency"], codes);
    }

    [Fact]
    public void Tax_id_match_wins_over_a_different_printed_name()
    {
        var match = VendorMatcher.Match(Invoice(vendor: "Contoso Software Holdings", taxId: "113551001"), Vendors);

        Assert.Equal((1, "tax_id"), (match!.Vendor.Id, match.MatchedBy));
    }

    [Theory]
    [InlineData("CONTOSO SOFTWARE, INC.")]
    [InlineData("Contoso Software LLC")]
    [InlineData("  contoso   software ")]
    public void Name_match_ignores_case_punctuation_and_legal_suffixes(string printed)
    {
        var match = VendorMatcher.Match(Invoice(vendor: printed), Vendors);

        Assert.Equal((1, "name"), (match!.Vendor.Id, match.MatchedBy));
    }

    [Fact]
    public void Suffix_is_not_stripped_when_it_is_the_whole_name()
    {
        Assert.Equal("adatum legal", VendorMatcher.NormalizeName("Adatum Legal LLP"));
        Assert.Equal("co", VendorMatcher.NormalizeName("Co."));
    }

    [Fact]
    public void Unknown_vendor_does_not_match()
    {
        Assert.Null(VendorMatcher.Match(Invoice(vendor: "Northwind Traders"), Vendors));
    }

    [Fact]
    public void Gl_default_follows_vendor_category()
    {
        Assert.Equal("6600", ChartOfAccounts.DefaultFor("utilities"));
        Assert.Null(ChartOfAccounts.DefaultFor("Travel"));
        Assert.False(ChartOfAccounts.IsValid("9999"));
    }

    [Fact]
    public void Model_json_is_mapped_including_nulls_and_negative_lines()
    {
        const string json = """
            {"vendor_name":" Contoso Software ","vendor_tax_id":null,"invoice_number":"INV-9","invoice_date":"2026-09-30",
             "due_date":"not a date","currency":"usd","subtotal":90.5,"tax_amount":null,"total":90.5,
             "line_items":[{"description":"Licence","quantity":1,"unit_price":100,"amount":100,"gl_account":"6100"},
                           {"description":"Loyalty credit","quantity":1,"unit_price":-9.5,"amount":-9.5,"gl_account":null}],
             "uncertain_fields":["tax_amount"]}
            """;

        var invoice = ExtractionJson.Parse(json);

        Assert.Equal("Contoso Software", invoice.VendorName);
        Assert.Null(invoice.VendorTaxId);
        Assert.Equal(new DateOnly(2026, 9, 30), invoice.InvoiceDate);
        Assert.Null(invoice.DueDate);
        Assert.Equal("USD", invoice.Currency);
        Assert.Null(invoice.TaxAmount);
        Assert.Equal(-9.5m, invoice.Lines[1].Amount);
        Assert.Null(invoice.Lines[1].GlAccount);
        Assert.Equal(["tax_amount"], invoice.UncertainFields);
    }
}
