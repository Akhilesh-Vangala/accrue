using System.Text.Json;
using Anthropic;
using LedgerFlow.Application.Extraction;
using LedgerFlow.ExtractionEval;
using LedgerFlow.Infrastructure.Extraction;
using Microsoft.Extensions.Logging.Abstractions;

// Scores the production invoice reader against the labeled set in evals/invoice-extraction/dataset.
//   dotnet run --project backend/tools/LedgerFlow.ExtractionEval -- --effort medium
var options = EvalOptions.Parse(args);
var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
if (!options.Oracle && string.IsNullOrWhiteSpace(apiKey))
{
    Console.Error.WriteLine("Set ANTHROPIC_API_KEY to run the eval, or pass --oracle to check the harness against the labels.");
    return 1;
}

var labels = File.ReadAllLines(Path.Combine(options.Dataset, "labels.jsonl"))
    .Where(l => l.Length > 0)
    .Select(l => JsonSerializer.Deserialize<Label>(l, EvalJson.Options)!)
    .Take(options.Limit ?? int.MaxValue)
    .ToList();

// --oracle replays the labels as a perfect reading: every metric must come out at 100% (and 0% false alarms),
// which verifies the scorer, the labels and the deterministic checks before spending anything on model calls.
IInvoiceDocumentReader reader;
if (options.Oracle)
{
    reader = new OracleReader(labels);
}
else
{
    var readerOptions = new ClaudeExtractionOptions { ApiKey = apiKey, Model = options.Model, Effort = options.Effort };
    reader = new ClaudeInvoiceDocumentReader(
        new AnthropicClient { ApiKey = apiKey, Timeout = TimeSpan.FromSeconds(readerOptions.TimeoutSeconds) },
        readerOptions,
        NullLogger<ClaudeInvoiceDocumentReader>.Instance);
}

Console.WriteLine($"Reading {labels.Count} invoices with {reader.Model} (effort {options.Effort}, concurrency {options.Concurrency})...");
using var gate = new SemaphoreSlim(options.Concurrency);
var results = await Task.WhenAll(labels.Select(async label =>
{
    await gate.WaitAsync();
    try
    {
        var bytes = await File.ReadAllBytesAsync(Path.Combine(options.Dataset, label.File));
        var reading = await reader.ReadAsync(new InvoiceDocument(label.File, "application/pdf", bytes), CancellationToken.None);
        var result = Scorer.Score(label, reading);
        Console.WriteLine($"  {label.Id,-7} {(result.DocumentCorrect ? "pass" : "FAIL"),-5} {result.Mismatches.Count} mismatches, {reading.Usage.LatencyMs} ms");
        return result;
    }
    catch (Exception ex) when (ex is not OutOfMemoryException)
    {
        Console.WriteLine($"  {label.Id,-7} ERROR {ex.Message}");
        return Scorer.Failed(label, ex.Message);
    }
    finally
    {
        gate.Release();
    }
}));

var summary = Summary.From(results, options);
Directory.CreateDirectory(options.Output);
var stem = Path.Combine(options.Output, options.Oracle ? "oracle" : $"{options.Model}-{options.Effort}");
await File.WriteAllTextAsync(stem + ".json", JsonSerializer.Serialize(new { summary, results }, EvalJson.Indented));
await File.WriteAllTextAsync(stem + ".md", summary.ToMarkdown());
Console.WriteLine();
Console.WriteLine(summary.ToMarkdown());
Console.WriteLine($"Wrote {stem}.json and {stem}.md");
return 0;
