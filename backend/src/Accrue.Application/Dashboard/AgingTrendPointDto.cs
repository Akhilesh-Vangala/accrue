namespace Accrue.Application.Dashboard;

public sealed record AgingTrendPointDto(DateOnly Date, decimal Current, decimal Overdue);
