namespace LedgerFlow.Application.Reporting;

public sealed class VendorSpendRow
{
    public int VendorId { get; set; }

    public string VendorName { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public DateOnly PeriodStart { get; set; }

    public int InvoiceCount { get; set; }

    public decimal TotalAmount { get; set; }
}
