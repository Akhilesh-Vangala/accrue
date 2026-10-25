namespace LedgerFlow.Application.Extraction;

public sealed record ExtractionCapabilitiesDto(bool Enabled, string? Model, long MaxFileBytes, IReadOnlyList<string> AcceptedTypes);
