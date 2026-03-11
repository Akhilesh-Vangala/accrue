namespace LedgerFlow.Application.Reporting;

public static class ReportNames
{
    public const string ApAging = "ApAging";
    public const string VendorSpend = "VendorSpend";
    public const string PaymentRemittance = "PaymentRemittance";

    public static readonly IReadOnlyList<ReportDefinition> All =
    [
        new(ApAging, "AP aging", "Open payables by days past due: not yet due, 1-30, 31-60, 61-90 and 90+.", []),
        new(VendorSpend, "Vendor spend", "Monthly invoiced spend by vendor and category.", ["from", "to"]),
        new(PaymentRemittance, "Payment remittance", "Remittance advice for a released payment run.", ["paymentRunId"]),
    ];
}
