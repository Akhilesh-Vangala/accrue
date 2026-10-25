namespace Accrue.Application.Extraction;

/// <summary>Model, token usage and wall-clock latency of one document read.</summary>
public sealed record ReadingUsage(string Model, long InputTokens, long OutputTokens, long LatencyMs);
