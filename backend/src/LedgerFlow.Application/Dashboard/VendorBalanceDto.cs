namespace LedgerFlow.Application.Dashboard;

public sealed record VendorBalanceDto(int VendorId, string VendorName, decimal OpenBalance, int OpenInvoices);
