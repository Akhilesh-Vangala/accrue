namespace LedgerFlow.Domain.Common;

public static class ApprovalPolicy
{
    /// <summary>Invoices above this total need a controller's approval.</summary>
    public const decimal ApproverLimit = 10_000m;
}
