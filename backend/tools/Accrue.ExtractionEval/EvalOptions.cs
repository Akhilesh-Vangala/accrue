namespace Accrue.ExtractionEval;

internal sealed record EvalOptions(string Dataset, string Output, string Model, string Effort, int Concurrency, int? Limit, bool Oracle)
{
    public static EvalOptions Parse(string[] args)
    {
        string? Value(string name)
        {
            var i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        var root = FindRepoRoot();
        return new EvalOptions(
            Value("--dataset") ?? Path.Combine(root, "evals", "invoice-extraction", "dataset"),
            Value("--out") ?? Path.Combine(root, "evals", "invoice-extraction", "results"),
            Value("--model") ?? "claude-opus-5-5",
            Value("--effort") ?? "medium",
            int.TryParse(Value("--concurrency"), out var c) ? c : 4,
            int.TryParse(Value("--limit"), out var l) ? l : null,
            args.Contains("--oracle"));
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "evals")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? Directory.GetCurrentDirectory();
    }
}
