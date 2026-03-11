namespace LedgerFlow.Domain.Operations;

/// <summary>A rendered report kept for audit (weekly AP aging PDF produced by Hangfire + Telerik).</summary>
public sealed class ReportArchive
{
    private ReportArchive()
    {
        ReportName = string.Empty;
        FileName = string.Empty;
        ContentType = string.Empty;
        Content = [];
        GeneratedBy = string.Empty;
    }

    public ReportArchive(string reportName, string fileName, string contentType, byte[] content, string generatedBy, DateTimeOffset generatedAt)
    {
        ReportName = reportName;
        FileName = fileName;
        ContentType = contentType;
        Content = content;
        SizeBytes = content.LongLength;
        GeneratedBy = generatedBy;
        GeneratedAt = generatedAt;
    }

    public int Id { get; private set; }

    public string ReportName { get; private set; }

    public string FileName { get; private set; }

    public string ContentType { get; private set; }

    public byte[] Content { get; private set; }

    public long SizeBytes { get; private set; }

    public string GeneratedBy { get; private set; }

    public DateTimeOffset GeneratedAt { get; private set; }
}
