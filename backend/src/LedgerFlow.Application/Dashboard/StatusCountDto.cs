using LedgerFlow.Domain.Invoices;

namespace LedgerFlow.Application.Dashboard;

public sealed record StatusCountDto(InvoiceStatus Status, int Count);
