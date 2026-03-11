using System.Globalization;
using LedgerFlow.Application.Abstractions;
using LedgerFlow.Domain.Common;
using LedgerFlow.Domain.Operations;
using Microsoft.EntityFrameworkCore;

namespace LedgerFlow.Application.Reporting;

public sealed class ReportService(ILedgerFlowDb db, IReportRenderer renderer, CsvReportRenderer csv, TimeProvider time)
{
    public ReportCapabilitiesDto Capabilities() =>
        new(renderer.Engine, renderer.SupportsPdf, renderer.SupportsPdf, ReportNames.All);

    public async Task<IReadOnlyList<ReportArchiveDto>> ArchiveAsync(CancellationToken ct) =>
        await db.ReportArchives.AsNoTracking()
            .OrderByDescending(r => r.GeneratedAt)
            .Select(r => new ReportArchiveDto(r.Id, r.ReportName, r.FileName, r.ContentType, r.SizeBytes, r.GeneratedBy, r.GeneratedAt))
            .Take(100)
            .ToListAsync(ct);

    public async Task<RenderedReport> DownloadArchivedAsync(int id, CancellationToken ct)
    {
        var archive = await db.ReportArchives.AsNoTracking().SingleOrDefaultAsync(r => r.Id == id, ct)
            ?? throw NotFoundException.For<ReportArchive>(id);
        return new RenderedReport(archive.FileName, archive.ContentType, archive.Content);
    }

    /// <summary>Renders a report on demand as <c>pdf</c> (Telerik) or <c>csv</c>.</summary>
    public Task<RenderedReport> RenderAsync(string reportName, string format, IReadOnlyDictionary<string, string?> query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);
        var parameters = ParseParameters(reportName, query);
        return format.ToLowerInvariant() switch
        {
            "csv" => csv.RenderAsync(reportName, parameters, ct),
            "pdf" when renderer.SupportsPdf => renderer.RenderAsync(reportName, parameters, ct),
            "pdf" => throw new DomainException("pdf_unavailable", "PDF rendering needs Telerik Reporting. Build with -p:UseTelerik=true or download CSV."),
            _ => throw new DomainException("unknown_format", $"Unknown report format '{format}'."),
        };
    }

    /// <summary>Renders with the best available engine and stores the output for audit. Used by the weekly job.</summary>
    public async Task<int> RenderAndArchiveAsync(string reportName, IReadOnlyDictionary<string, object?> parameters, string actor, CancellationToken ct)
    {
        var engine = renderer.SupportsPdf ? renderer : (IReportRenderer)csv;
        var rendered = await engine.RenderAsync(reportName, parameters, ct);
        var archive = new ReportArchive(reportName, rendered.FileName, rendered.ContentType, rendered.Content, actor, time.GetUtcNow());
        db.ReportArchives.Add(archive);
        await db.SaveChangesAsync(ct);
        return archive.Id;
    }

    private Dictionary<string, object?> ParseParameters(string reportName, IReadOnlyDictionary<string, string?> query)
    {
        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        return reportName switch
        {
            ReportNames.ApAging => [],
            ReportNames.VendorSpend => new()
            {
                ["from"] = ParseDate(query, "from") ?? new DateOnly(today.Year, 1, 1),
                ["to"] = ParseDate(query, "to") ?? today,
            },
            ReportNames.PaymentRemittance => new()
            {
                ["paymentRunId"] = query.TryGetValue("paymentRunId", out var raw) && int.TryParse(raw, CultureInfo.InvariantCulture, out var id)
                    ? id
                    : throw new DomainException("missing_parameter", "paymentRunId is required."),
            },
            _ => throw new NotFoundException($"Report '{reportName}' does not exist."),
        };
    }

    private static DateOnly? ParseDate(IReadOnlyDictionary<string, string?> query, string key) =>
        query.TryGetValue(key, out var raw) && DateOnly.TryParse(raw, CultureInfo.InvariantCulture, out var date) ? date : null;
}
