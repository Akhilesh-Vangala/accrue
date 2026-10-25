namespace Accrue.Application.Abstractions;

/// <summary>Background job scheduling, implemented with Hangfire.</summary>
public interface IJobScheduler
{
    void EnqueueInvoiceDecisionNotification(int invoiceId);

    IReadOnlyList<RecurringJobInfo> GetRecurringJobs();

    /// <summary>Triggers a recurring job immediately and returns the background job id.</summary>
    string TriggerNow(string recurringJobId);
}
