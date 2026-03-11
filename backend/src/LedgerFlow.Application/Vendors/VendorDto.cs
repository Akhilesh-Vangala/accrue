namespace LedgerFlow.Application.Vendors;

public sealed record VendorDto(
    int Id,
    string Name,
    string TaxId,
    string Email,
    string Category,
    int PaymentTermsDays,
    bool IsActive,
    decimal OpenBalance,
    int OpenInvoices,
    string RowVersion);
