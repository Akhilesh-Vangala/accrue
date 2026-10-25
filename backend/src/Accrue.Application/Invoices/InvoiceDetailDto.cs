namespace Accrue.Application.Invoices;

public sealed record InvoiceDetailDto(
    InvoiceSummaryDto Summary,
    decimal Subtotal,
    decimal TaxAmount,
    string? Notes,
    IReadOnlyList<InvoiceLineDto> Lines,
    IReadOnlyList<InvoiceEventDto> Events,
    IReadOnlyList<string> AllowedActions,
    string RowVersion);
