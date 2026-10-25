namespace Accrue.Application.Extraction;

/// <summary>One invoice line as read from the document. <see cref="Amount"/> is the printed line total.</summary>
public sealed record ExtractedLine(string Description, decimal Quantity, decimal UnitPrice, decimal Amount, string? GlAccount);
