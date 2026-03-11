using LedgerFlow.Domain.Payments;

namespace LedgerFlow.Application.Payments;

public sealed record PaymentRunSummaryDto(
    int Id,
    string Name,
    DateOnly PaymentDate,
    PaymentRunStatus Status,
    decimal Total,
    int PaymentCount,
    string? CreatedBy,
    string? ReleasedBy,
    DateTimeOffset? ReleasedAt);
