namespace LedgerFlow.Application.Reporting;

public sealed record ReportDefinition(string Name, string Title, string Description, IReadOnlyList<string> Parameters);
