namespace LedgerFlow.Application.Reporting;

public sealed record ReportCapabilitiesDto(string Engine, bool SupportsPdf, bool InteractiveViewer, IReadOnlyList<ReportDefinition> Reports);
