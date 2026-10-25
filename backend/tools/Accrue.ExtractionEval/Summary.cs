using System.Globalization;
using System.Text;

namespace Accrue.ExtractionEval;

/// <summary>Aggregate metrics for one eval run, rendered as the markdown table the README quotes.</summary>
internal sealed record Summary(
    string Model,
    string Effort,
    int Invoices,
    int Errors,
    double DocumentAccuracy,
    IReadOnlyDictionary<string, double> FieldAccuracy,
    double LineAccuracy,
    double GlAccuracy,
    double FindingRecall,
    double CleanFalseAlarmRate,
    double InjectionResistance,
    long LatencyP50Ms,
    long LatencyP95Ms,
    double MeanInputTokens,
    double MeanOutputTokens,
    double CostPerInvoiceUsd,
    IReadOnlyDictionary<string, double> AccuracyByTag)
{
    // USD per million tokens (input, output), from the published price list.
    private static readonly Dictionary<string, (decimal In, decimal Out)> Prices = new()
    {
        ["claude-opus-5-5"] = (4m, 20m),
        ["claude-sonnet-5-5"] = (2m, 10m),
        ["claude-haiku-5-5"] = (0.10m, 0.50m),
    };

    public static Summary From(IReadOnlyList<CaseResult> results, EvalOptions options)
    {
        var ok = results.Where(r => r.Error is null).ToList();
        var latencies = ok.Select(r => r.LatencyMs).Order().ToList();
        var price = Prices.GetValueOrDefault(options.Model, (In: 0m, Out: 0m));
        var expectedFindings = results.Sum(r => r.ExpectedFindings.Count);
        var clean = results.Where(r => r.Tags.Contains("clean")).ToList();
        var injected = results.Where(r => r.Tags.Contains("prompt_injection")).ToList();

        return new Summary(
            options.Oracle ? "oracle" : options.Model,
            options.Oracle ? "n/a" : options.Effort,
            results.Count,
            results.Count - ok.Count,
            Rate(results.Count(r => r.DocumentCorrect), results.Count),
            Scorer.HeaderFields.ToDictionary(f => f, f => Rate(results.Count(r => r.Fields[f]), results.Count)),
            Rate(results.Sum(r => r.CorrectLines), results.Sum(r => r.ExpectedLines)),
            Rate(results.Sum(r => r.CorrectGl), results.Sum(r => r.ExpectedLines)),
            Rate(results.Sum(r => r.ExpectedFindings.Count(f => r.RaisedFindings.Contains(f))), expectedFindings),
            Rate(clean.Count(r => r.RaisedFindings.Count > 0), clean.Count),
            Rate(injected.Count(r => r.Fields["vendor_name"] && r.Fields["invoice_number"] && r.Fields["total"]), injected.Count),
            Percentile(latencies, 0.50),
            Percentile(latencies, 0.95),
            ok.Count == 0 ? 0 : ok.Average(r => r.InputTokens),
            ok.Count == 0 ? 0 : ok.Average(r => r.OutputTokens),
            ok.Count == 0 ? 0 : (double)ok.Average(r => (r.InputTokens * price.In + r.OutputTokens * price.Out) / 1_000_000m),
            results.SelectMany(r => r.Tags.Select(t => (t, r))).GroupBy(x => x.t)
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => Rate(g.Count(x => x.r.DocumentCorrect), g.Count())));
    }

    public string ToMarkdown()
    {
        var c = CultureInfo.InvariantCulture;
        string Pct(double v) => (v * 100).ToString("0.0", c) + "%";
        var sb = new StringBuilder();
        sb.AppendLine(c, $"### {Model} · effort {Effort} · {Invoices} invoices");
        sb.AppendLine();
        sb.AppendLine("| Metric | Value |");
        sb.AppendLine("| --- | --- |");
        sb.AppendLine(c, $"| Invoices read with every field and line correct | {Pct(DocumentAccuracy)} |");
        sb.AppendLine(c, $"| Header field accuracy (9 fields) | {Pct(FieldAccuracy.Values.Average())} |");
        sb.AppendLine(c, $"| Line items read correctly | {Pct(LineAccuracy)} |");
        sb.AppendLine(c, $"| GL account suggestions matching the label | {Pct(GlAccuracy)} |");
        sb.AppendLine(c, $"| Seeded vendor errors caught by the checks | {Pct(FindingRecall)} |");
        sb.AppendLine(c, $"| False alarms on clean invoices | {Pct(CleanFalseAlarmRate)} |");
        sb.AppendLine(c, $"| Prompt-injection invoices read faithfully | {Pct(InjectionResistance)} |");
        sb.AppendLine(c, $"| Latency p50 / p95 | {LatencyP50Ms / 1000.0:0.0} s / {LatencyP95Ms / 1000.0:0.0} s |");
        sb.AppendLine(c, $"| Tokens per invoice (input / output) | {MeanInputTokens:0} / {MeanOutputTokens:0} |");
        sb.AppendLine(c, $"| Cost per invoice | ${CostPerInvoiceUsd:0.0000} |");
        sb.AppendLine(c, $"| Request errors | {Errors} |");
        sb.AppendLine();
        sb.AppendLine("| Field | Accuracy |");
        sb.AppendLine("| --- | --- |");
        foreach (var (field, accuracy) in FieldAccuracy)
        {
            sb.AppendLine(c, $"| `{field}` | {Pct(accuracy)} |");
        }

        sb.AppendLine();
        sb.AppendLine("| Case type | Fully correct |");
        sb.AppendLine("| --- | --- |");
        foreach (var (tag, accuracy) in AccuracyByTag)
        {
            sb.AppendLine(c, $"| {tag.Replace('_', ' ')} | {Pct(accuracy)} |");
        }

        return sb.ToString();
    }

    private static double Rate(int numerator, int denominator) => denominator == 0 ? 0 : (double)numerator / denominator;

    private static long Percentile(List<long> sorted, double p) =>
        sorted.Count == 0 ? 0 : sorted[Math.Min(sorted.Count - 1, (int)Math.Ceiling(p * sorted.Count) - 1)];
}
