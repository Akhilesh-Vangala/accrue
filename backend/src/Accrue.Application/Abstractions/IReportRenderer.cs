namespace Accrue.Application.Abstractions;

/// <summary>Renders a named report. Telerik produces PDF; the fallback produces CSV.</summary>
public interface IReportRenderer
{
    string Engine { get; }

    bool SupportsPdf { get; }

    Task<RenderedReport> RenderAsync(string reportName, IReadOnlyDictionary<string, object?> parameters, CancellationToken cancellationToken);
}
