namespace LedgerFlow.Application.Abstractions;

public sealed record RecurringJobInfo(
    string Id,
    string Description,
    string Cron,
    DateTimeOffset? LastExecution,
    string? LastJobState,
    DateTimeOffset? NextExecution);
