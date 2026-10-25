namespace Accrue.ExtractionEval;

/// <summary>One line of labels.jsonl, written by evals/invoice-extraction/generate.py.</summary>
internal sealed record Label(string Id, string File, IReadOnlyList<string> Tags, ExpectedInvoice Expected, IReadOnlyList<string> ExpectedFindings);
