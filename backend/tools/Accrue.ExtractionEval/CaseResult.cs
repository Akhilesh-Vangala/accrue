namespace Accrue.ExtractionEval;

/// <summary>Score for one invoice. <see cref="Fields"/> maps each header field to whether it was read correctly.</summary>
internal sealed record CaseResult(
    string Id,
    IReadOnlyList<string> Tags,
    string? Error,
    IReadOnlyDictionary<string, bool> Fields,
    int ExpectedLines,
    int CorrectLines,
    int CorrectGl,
    bool LineCountCorrect,
    IReadOnlyList<string> ExpectedFindings,
    IReadOnlyList<string> RaisedFindings,
    IReadOnlyList<string> Mismatches,
    long LatencyMs,
    long InputTokens,
    long OutputTokens,
    string? ServedBy)
{
    public bool DocumentCorrect => Error is null && Fields.Values.All(v => v) && LineCountCorrect && CorrectLines == ExpectedLines;
}
