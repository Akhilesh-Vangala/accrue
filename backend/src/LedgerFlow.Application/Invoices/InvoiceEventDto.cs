using LedgerFlow.Domain.Invoices;

namespace LedgerFlow.Application.Invoices;

public sealed record InvoiceEventDto(InvoiceEventType Type, string Actor, string? Comment, DateTimeOffset OccurredAt);
