using Accrue.Domain.Invoices;

namespace Accrue.Application.Dashboard;

public sealed record StatusCountDto(InvoiceStatus Status, int Count);
