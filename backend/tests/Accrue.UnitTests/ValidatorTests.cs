using Accrue.Application.Invoices;
using Accrue.Application.Payments;
using Accrue.Application.Vendors;
using Accrue.Domain.Invoices;

namespace Accrue.UnitTests;

public class ValidatorTests
{
    private static readonly FixedTime Clock = new(new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero));

    private static SaveInvoiceRequest Invoice(DateOnly? date = null, params InvoiceLineInput[] lines) =>
        new(1, "INV-100", date ?? new DateOnly(2026, 9, 30), 0m, null,
            lines.Length == 0 ? [new InvoiceLineInput("Paper", "6100", 1, 10m)] : lines);

    [Fact]
    public void Valid_invoice_passes() =>
        Assert.True(new SaveInvoiceValidator(Clock).Validate(Invoice()).IsValid);

    [Fact]
    public void Invoice_needs_lines()
    {
        var result = new SaveInvoiceValidator(Clock).Validate(Invoice() with { Lines = [] });

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SaveInvoiceRequest.Lines));
    }

    [Fact]
    public void Invoice_date_cannot_be_in_the_future()
    {
        var result = new SaveInvoiceValidator(Clock).Validate(Invoice(new DateOnly(2026, 10, 5)));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SaveInvoiceRequest.InvoiceDate));
    }

    [Theory]
    [InlineData("61", 1, 10)]
    [InlineData("6100", 0, 10)]
    [InlineData("6100", 1, -1)]
    public void Invalid_lines_are_reported_by_index(string gl, decimal quantity, decimal price)
    {
        var result = new SaveInvoiceValidator(Clock).Validate(
            Invoice(null, new InvoiceLineInput("ok", "6100", 1, 1m), new InvoiceLineInput("bad", gl, quantity, price)));

        Assert.All(result.Errors, e => Assert.StartsWith("Lines[1].", e.PropertyName, StringComparison.Ordinal));
        Assert.NotEmpty(result.Errors);
    }

    [Theory]
    [InlineData("12-3456789", true)]
    [InlineData("123456789", false)]
    [InlineData("12-345678", false)]
    public void Vendor_tax_id_format(string taxId, bool valid)
    {
        var request = new SaveVendorRequest("Contoso", taxId, "ap@contoso.test", "Software", 30);

        Assert.Equal(valid, new SaveVendorValidator().Validate(request).IsValid);
    }

    [Fact]
    public void Payment_run_rejects_past_dates_and_duplicate_invoices()
    {
        var result = new CreatePaymentRunValidator(Clock).Validate(
            new CreatePaymentRunRequest("Run", new DateOnly(2026, 9, 30), [1, 1]));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePaymentRunRequest.PaymentDate));
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Invoice ids must be unique.");
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
