using Accrue.Domain.Invoices;

namespace Accrue.Application.Invoices;

public sealed record InvoiceEventDto(InvoiceEventType Type, string Actor, string? Comment, DateTimeOffset OccurredAt);
