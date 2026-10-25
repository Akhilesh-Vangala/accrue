namespace Accrue.Application.Abstractions;

public sealed record RenderedReport(string FileName, string ContentType, byte[] Content);
