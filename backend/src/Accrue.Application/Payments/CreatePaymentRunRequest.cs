namespace Accrue.Application.Payments;

public sealed record CreatePaymentRunRequest(string Name, DateOnly PaymentDate, IReadOnlyList<int> InvoiceIds);
