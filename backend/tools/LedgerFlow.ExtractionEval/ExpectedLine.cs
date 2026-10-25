namespace LedgerFlow.ExtractionEval;

internal sealed record ExpectedLine(string Description, decimal Quantity, decimal UnitPrice, decimal Amount, string GlAccount);
