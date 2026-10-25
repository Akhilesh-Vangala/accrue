namespace LedgerFlow.Application.Extraction;

/// <summary>
/// Something the reviewer must look at before saving. <see cref="Severity"/> is <c>error</c> (the draft would be wrong),
/// <c>warning</c> (needs a human decision) or <c>info</c>. <see cref="Field"/> names the form field it applies to, if any.
/// </summary>
public sealed record ExtractionFinding(string Code, string Severity, string Message, string? Field = null);
