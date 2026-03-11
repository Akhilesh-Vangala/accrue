using LedgerFlow.Domain.Invoices;

namespace LedgerFlow.Application.Invoices;

public sealed record InvoiceQuery(
    InvoiceStatus? Status = null,
    int? VendorId = null,
    bool? Overdue = null,
    string? Search = null,
    string? Sort = null,
    int Page = 1,
    int PageSize = 25);
