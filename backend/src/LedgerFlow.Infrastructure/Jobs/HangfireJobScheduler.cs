using Hangfire;
using Hangfire.Storage;
using LedgerFlow.Application.Abstractions;
using LedgerFlow.Application.Jobs;

namespace LedgerFlow.Infrastructure.Jobs;

public sealed class HangfireJobScheduler(IBackgroundJobClient client, IRecurringJobManagerV2 recurring, JobStorage storage) : IJobScheduler
{
    public void EnqueueInvoiceDecisionNotification(int invoiceId) =>
        client.Enqueue<InvoiceDecisionNotificationJob>(job => job.RunAsync(invoiceId, CancellationToken.None));

    public IReadOnlyList<RecurringJobInfo> GetRecurringJobs()
    {
        using var connection = storage.GetConnection();
        return connection.GetRecurringJobs()
            .Where(j => JobCatalog.Recurring.ContainsKey(j.Id))
            .OrderBy(j => j.Id)
            .Select(j => new RecurringJobInfo(
                j.Id,
                JobCatalog.Recurring[j.Id].Description,
                j.Cron,
                ToOffset(j.LastExecution),
                j.LastJobState,
                ToOffset(j.NextExecution)))
            .ToList();
    }

    public string TriggerNow(string recurringJobId)
    {
        if (!JobCatalog.Recurring.ContainsKey(recurringJobId))
        {
            throw new NotFoundException($"Job '{recurringJobId}' does not exist.");
        }

        return recurring.TriggerJob(recurringJobId) ?? string.Empty;
    }

    /// <summary>Registers (or updates) every recurring job from <see cref="JobCatalog"/>. Safe to call on each startup.</summary>
    public static void RegisterRecurringJobs(IRecurringJobManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);
        var options = new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc };
        manager.AddOrUpdate<OverdueInvoiceJob>(JobCatalog.OverdueInvoices, j => j.RunAsync(CancellationToken.None), Cron(JobCatalog.OverdueInvoices), options);
        manager.AddOrUpdate<PaymentReminderJob>(JobCatalog.PaymentReminders, j => j.RunAsync(CancellationToken.None), Cron(JobCatalog.PaymentReminders), options);
        manager.AddOrUpdate<AgingSnapshotJob>(JobCatalog.AgingSnapshot, j => j.RunAsync(CancellationToken.None), Cron(JobCatalog.AgingSnapshot), options);
        manager.AddOrUpdate<AutoPaymentRunJob>(JobCatalog.AutoPaymentRun, j => j.RunAsync(CancellationToken.None), Cron(JobCatalog.AutoPaymentRun), options);
        manager.AddOrUpdate<WeeklyAgingReportJob>(JobCatalog.WeeklyAgingReport, j => j.RunAsync(CancellationToken.None), Cron(JobCatalog.WeeklyAgingReport), options);
    }

    private static string Cron(string id) => JobCatalog.Recurring[id].Cron;

    private static DateTimeOffset? ToOffset(DateTime? utc) =>
        utc is { } value ? new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)) : null;
}
