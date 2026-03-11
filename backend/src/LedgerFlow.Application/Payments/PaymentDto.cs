namespace LedgerFlow.Application.Payments;

public sealed record PaymentDto(int Id, int InvoiceId, string InvoiceNumber, string VendorName, DateOnly DueDate, decimal Amount, string? Reference);
