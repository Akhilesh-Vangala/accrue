namespace LedgerFlow.Infrastructure.Extraction;

/// <summary>Bound from the <c>Anthropic</c> configuration section. The key falls back to <c>ANTHROPIC_API_KEY</c>.</summary>
public sealed class ClaudeExtractionOptions
{
    public const string Section = "Anthropic";

    public string? ApiKey { get; set; }

    public string Model { get; set; } = "claude-opus-5-5";

    /// <summary>low, medium, high, xhigh or max. Tune with the extraction eval (<c>evals/invoice-extraction</c>).</summary>
    public string Effort { get; set; } = "medium";

    public int MaxTokens { get; set; } = 16_000;

    public int TimeoutSeconds { get; set; } = 120;
}
