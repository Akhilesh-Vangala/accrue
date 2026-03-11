using LedgerFlow.Application.Reporting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LedgerFlow.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public sealed class ReportsController(ReportService reports) : ControllerBase
{
    [HttpGet("capabilities")]
    public ReportCapabilitiesDto Capabilities() => reports.Capabilities();

    [HttpGet("archive")]
    public Task<IReadOnlyList<ReportArchiveDto>> Archive(CancellationToken ct) => reports.ArchiveAsync(ct);

    [HttpGet("archive/{id:int}")]
    public async Task<IActionResult> DownloadArchived(int id, CancellationToken ct)
    {
        var file = await reports.DownloadArchivedAsync(id, ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>Renders a report on demand. Query string values become report parameters (from, to, paymentRunId).</summary>
    [HttpGet("{name}.{format}")]
    public async Task<IActionResult> Render(string name, string format, CancellationToken ct)
    {
        var parameters = Request.Query.ToDictionary(q => q.Key, q => (string?)q.Value.ToString(), StringComparer.OrdinalIgnoreCase);
        var file = await reports.RenderAsync(name, format, parameters, ct);
        return File(file.Content, file.ContentType, file.FileName);
    }
}
