namespace Accrue.Application.Dashboard;

public sealed record DashboardDto(
    KpiDto Kpis,
    IReadOnlyList<AgingBucketDto> Aging,
    IReadOnlyList<VendorBalanceDto> TopVendors,
    IReadOnlyList<MonthlySpendDto> MonthlySpend,
    IReadOnlyList<AgingTrendPointDto> AgingTrend,
    IReadOnlyList<StatusCountDto> StatusCounts);
