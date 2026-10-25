namespace Accrue.Application.Dashboard;

public sealed record AgingBucketDto(string Bucket, int Order, int InvoiceCount, decimal Amount);
