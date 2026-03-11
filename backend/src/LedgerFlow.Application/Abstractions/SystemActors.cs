namespace LedgerFlow.Application.Abstractions;

public static class SystemActors
{
    public const string OverdueJob = "system:overdue-job";
    public const string ReminderJob = "system:reminder-job";
    public const string AgingJob = "system:aging-job";
    public const string PaymentRunJob = "system:payment-run-job";
    public const string ReportJob = "system:report-job";
    public const string Seeder = "system:seed";
}
