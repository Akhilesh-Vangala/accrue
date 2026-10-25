namespace Accrue.Application.Reporting;

public sealed class PaymentRemittanceRow
{
    public int PaymentId { get; set; }

    public int PaymentRunId { get; set; }

    public string RunName { get; set; } = string.Empty;

    public DateOnly PaymentDate { get; set; }

    public string RunStatus { get; set; } = string.Empty;

    public string VendorName { get; set; } = string.Empty;

    public string VendorEmail { get; set; } = string.Empty;

    public string InvoiceNumber { get; set; } = string.Empty;

    public DateOnly InvoiceDate { get; set; }

    public decimal Amount { get; set; }

    public string? Reference { get; set; }
}
