using System.Text.Json;

namespace LedgerFlow.ExtractionEval;

internal static class EvalJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public static readonly JsonSerializerOptions Indented = new(Options) { WriteIndented = true };
}
