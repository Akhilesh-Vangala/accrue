namespace Accrue.Application.Reporting;

public sealed class ApAgingRow
{
    public int InvoiceId { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public int VendorId { get; set; }

    public string VendorName { get; set; } = string.Empty;

    public DateOnly InvoiceDate { get; set; }

    public DateOnly DueDate { get; set; }

    public string Status { get; set; } = string.Empty;

    public decimal Outstanding { get; set; }

    public int DaysPastDue { get; set; }

    public string Bucket { get; set; } = string.Empty;

    public int BucketOrder { get; set; }
}
