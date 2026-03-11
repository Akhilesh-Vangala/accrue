namespace LedgerFlow.Application.Jobs;

/// <summary>Recurring job ids and schedules (cron, UTC). Hangfire registration reads from here.</summary>
public static class JobCatalog
{
    public const string OverdueInvoices = "overdue-invoices";
    public const string PaymentReminders = "payment-reminders";
    public const string AgingSnapshot = "aging-snapshot";
    public const string AutoPaymentRun = "auto-payment-run";
    public const string WeeklyAgingReport = "weekly-aging-report";

    public static readonly IReadOnlyDictionary<string, (string Cron, string Description)> Recurring =
        new Dictionary<string, (string, string)>
        {
            [OverdueInvoices] = ("0 6 * * *", "Flags unpaid invoices past their due date and alerts AP."),
            [PaymentReminders] = ("15 6 * * *", "Reminds controllers about approved invoices due within 7 days."),
            [AgingSnapshot] = ("30 23 * * *", "Stores tonight's AP aging buckets for trend reporting (idempotent)."),
            [AutoPaymentRun] = ("0 7 * * 1", "Drafts Monday's payment run from approved invoices due within 10 days."),
            [WeeklyAgingReport] = ("0 8 * * 1", "Renders the AP aging report and files it in the report archive."),
        };
}
