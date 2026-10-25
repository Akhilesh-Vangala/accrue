namespace Accrue.Infrastructure.Reporting;

public sealed class ReportingOptions
{
    /// <summary>Folder holding the Telerik .trdx definitions (copied next to the API binaries).</summary>
    public string ReportsPath { get; init; } = Path.Combine(AppContext.BaseDirectory, "Reports");

    public static bool TelerikEnabled =>
#if TELERIK
        true;
#else
        false;
#endif
}
