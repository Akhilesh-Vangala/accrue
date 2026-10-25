using Accrue.Domain.Invoices;

namespace Accrue.Application.Invoices;

public sealed record InvoiceSummaryDto(
    int Id,
    string InvoiceNumber,
    int VendorId,
    string VendorName,
    DateOnly InvoiceDate,
    DateOnly DueDate,
    decimal Total,
    decimal Outstanding,
    InvoiceStatus Status,
    bool IsOverdue,
    string? CreatedBy,
    int? PaymentRunId);
