namespace Accrue.Domain.Operations;

/// <summary>Nightly point-in-time AP aging totals, used for trend charts and month-end reconciliation.</summary>
public sealed class AgingSnapshot
{
    private AgingSnapshot()
    {
        Bucket = string.Empty;
    }

    public AgingSnapshot(DateOnly snapshotDate, string bucket, int bucketOrder, int invoiceCount, decimal amount)
    {
        SnapshotDate = snapshotDate;
        Bucket = bucket;
        BucketOrder = bucketOrder;
        InvoiceCount = invoiceCount;
        Amount = amount;
    }

    public int Id { get; private set; }

    public DateOnly SnapshotDate { get; private set; }

    public string Bucket { get; private set; }

    public int BucketOrder { get; private set; }

    public int InvoiceCount { get; private set; }

    public decimal Amount { get; private set; }
}
