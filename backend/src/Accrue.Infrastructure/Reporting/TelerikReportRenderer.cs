#if TELERIK
using System.Collections;
using System.Globalization;
using Accrue.Application.Abstractions;
using Accrue.Application.Reporting;
using Microsoft.Extensions.Configuration;
using Telerik.Reporting;
using Telerik.Reporting.Processing;

namespace Accrue.Infrastructure.Reporting;

/// <summary>Renders the .trdx report definitions to PDF with the Telerik Reporting engine.</summary>
public sealed class TelerikReportRenderer(IConfiguration configuration, ReportingOptions options, TimeProvider time) : IReportRenderer
{
    public string Engine => "Telerik Reporting";

    public bool SupportsPdf => true;

    public Task<RenderedReport> RenderAsync(string reportName, IReadOnlyDictionary<string, object?> parameters, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        var path = Path.Combine(options.ReportsPath, reportName + ".trdx");
        if (!File.Exists(path))
        {
            throw new NotFoundException($"Report definition '{reportName}.trdx' was not found.");
        }

        var source = new UriReportSource { Uri = path };
        foreach (var (name, value) in parameters)
        {
            source.Parameters.Add(name, value is DateOnly d ? d.ToDateTime(TimeOnly.MinValue) : value);
        }

        var processor = new ReportProcessor(configuration);
        var result = processor.RenderReport("PDF", source, new Hashtable());
        if (result.HasErrors)
        {
            throw new InvalidOperationException($"Telerik failed to render {reportName}: {string.Join("; ", result.Errors.Select(e => e.Message))}");
        }

        var stamp = time.GetUtcNow().ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture);
        return Task.FromResult(new RenderedReport($"{reportName}-{stamp}.pdf", "application/pdf", result.DocumentBytes));
    }
}
#endif
