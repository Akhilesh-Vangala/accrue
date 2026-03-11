namespace LedgerFlow.Application.Reporting;

public sealed record ReportArchiveDto(int Id, string ReportName, string FileName, string ContentType, long SizeBytes, string GeneratedBy, DateTimeOffset GeneratedAt);
