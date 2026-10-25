namespace Accrue.Application.Payments;

public sealed record PaymentRunDetailDto(PaymentRunSummaryDto Summary, IReadOnlyList<PaymentDto> Payments);
