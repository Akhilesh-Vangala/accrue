namespace Accrue.Application.Dashboard;

public sealed record KpiDto(
    decimal OpenPayables,
    int OpenInvoices,
    decimal OverdueAmount,
    int OverdueInvoices,
    int PendingApprovals,
    decimal ReadyToPay,
    decimal PaidLast30Days,
    double? AverageApprovalHours);
