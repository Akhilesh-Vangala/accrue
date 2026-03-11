namespace LedgerFlow.Application.Dashboard;

public sealed record MonthlySpendDto(DateOnly Month, decimal Amount, int InvoiceCount);
