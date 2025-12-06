using System.Text.Json;

// Simple argument parsing
string? baselinePath = null;
string? currentPath = null;
string? outputPath = null;
double threshold = 5.0;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--baseline" when i + 1 < args.Length:
            baselinePath = args[++i];
            break;
        case "--current" when i + 1 < args.Length:
            currentPath = args[++i];
            break;
        case "--output" when i + 1 < args.Length:
            outputPath = args[++i];
            break;
        case "--threshold" when i + 1 < args.Length:
            threshold = double.Parse(args[++i]);
            break;
        case "--help" or "-h":
            Console.WriteLine("BenchmarkCompare - Compare BenchmarkDotNet results");
            Console.WriteLine();
            Console.WriteLine("Usage: BenchmarkCompare --baseline <dir> --current <dir> [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --baseline <dir>    Directory containing baseline benchmark results");
            Console.WriteLine("  --current <dir>     Directory containing current benchmark results");
            Console.WriteLine("  --threshold <pct>   Regression threshold percentage (default: 5.0)");
            Console.WriteLine("  --output <file>     Output file for the comparison report");
            Console.WriteLine("  --help, -h          Show this help message");
            return 0;
    }
}

if (string.IsNullOrEmpty(baselinePath) || string.IsNullOrEmpty(currentPath))
{
    Console.Error.WriteLine("Error: --baseline and --current are required.");
    Console.Error.WriteLine("Use --help for usage information.");
    return 1;
}

var baselineDir = new DirectoryInfo(baselinePath);
var currentDir = new DirectoryInfo(currentPath);

// Handle missing directories gracefully - this can happen when:
// - New benchmarks are added in a PR (baseline won't have them)
// - Benchmark filter matches nothing in baseline
var baselineMissing = !baselineDir.Exists;
var currentMissing = !currentDir.Exists;

if (baselineMissing)
{
    Console.Error.WriteLine($"Warning: Baseline directory not found: {baselinePath}");
    Console.Error.WriteLine("Comparison will show all current benchmarks as 'New'.");
}

if (currentMissing)
{
    Console.Error.WriteLine($"Error: Current directory not found: {currentPath}");
    Console.Error.WriteLine("Cannot compare without current benchmark results.");
    return 1;
}

var baselineResults = LoadBenchmarkResults(baselineDir);
var currentResults = LoadBenchmarkResults(currentDir);

if (baselineResults.Count == 0)
{
    Console.Error.WriteLine($"Warning: No benchmark results found in baseline directory: {baselineDir}");
}

if (currentResults.Count == 0)
{
    Console.Error.WriteLine($"Warning: No benchmark results found in current directory: {currentDir}");
}

var comparison = CompareBenchmarks(baselineResults, currentResults, threshold);
var report = GenerateMarkdownReport(comparison, threshold);

if (!string.IsNullOrEmpty(outputPath))
{
    var outputDir = Path.GetDirectoryName(outputPath);
    if (!string.IsNullOrEmpty(outputDir))
    {
        Directory.CreateDirectory(outputDir);
    }
    await File.WriteAllTextAsync(outputPath, report);
    Console.WriteLine($"Comparison report written to: {outputPath}");
}
else
{
    Console.WriteLine(report);
}

return 0;

static Dictionary<string, BenchmarkResult> LoadBenchmarkResults(DirectoryInfo dir)
{
    var results = new Dictionary<string, BenchmarkResult>();

    // Handle missing directory gracefully
    if (!dir.Exists)
    {
        return results;
    }

    // Find all JSON report files in the directory tree
    // BenchmarkDotNet exports to *-report-full-compressed.json by default
    var jsonFiles = dir.GetFiles("*-report-full-compressed.json", SearchOption.AllDirectories)
        .Concat(dir.GetFiles("*-report-full.json", SearchOption.AllDirectories))
        .Concat(dir.GetFiles("*-report.json", SearchOption.AllDirectories))
        .ToList();

    foreach (var file in jsonFiles)
    {
        try
        {
            var json = File.ReadAllText(file.FullName);
            var report = JsonSerializer.Deserialize<BenchmarkReport>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (report?.Benchmarks != null)
            {
                foreach (var benchmark in report.Benchmarks)
                {
                    var key = GetBenchmarkKey(benchmark);
                    if (!string.IsNullOrEmpty(key) && benchmark.Statistics != null)
                    {
                        results[key] = new BenchmarkResult
                        {
                            Name = key,
                            MeanNs = benchmark.Statistics.Mean,
                            StdDevNs = benchmark.Statistics.StandardDeviation,
                            AllocatedBytes = benchmark.Memory?.BytesAllocatedPerOperation ?? 0
                        };
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"Warning: Failed to parse {file.FullName}: {ex.Message}");
        }
    }

    return results;
}

static string GetBenchmarkKey(Benchmark benchmark)
{
    var name = $"{benchmark.Type}.{benchmark.Method}";

    if (!string.IsNullOrEmpty(benchmark.Parameters))
    {
        name += $"({benchmark.Parameters})";
    }

    return name;
}

static List<ComparisonResult> CompareBenchmarks(
    Dictionary<string, BenchmarkResult> baseline,
    Dictionary<string, BenchmarkResult> current,
    double threshold)
{
    var comparisons = new List<ComparisonResult>();

    // Find common benchmarks
    var allKeys = baseline.Keys.Union(current.Keys).OrderBy(k => k);

    foreach (var key in allKeys)
    {
        var hasBaseline = baseline.TryGetValue(key, out var baselineResult);
        var hasCurrent = current.TryGetValue(key, out var currentResult);

        if (hasBaseline && hasCurrent)
        {
            var percentChange = ((currentResult!.MeanNs - baselineResult!.MeanNs) / baselineResult.MeanNs) * 100;
            var allocChange = currentResult.AllocatedBytes - baselineResult.AllocatedBytes;

            comparisons.Add(new ComparisonResult
            {
                Name = key,
                BaselineMeanNs = baselineResult.MeanNs,
                CurrentMeanNs = currentResult.MeanNs,
                PercentChange = percentChange,
                BaselineAllocBytes = baselineResult.AllocatedBytes,
                CurrentAllocBytes = currentResult.AllocatedBytes,
                AllocDelta = allocChange,
                Status = GetStatus(percentChange, threshold)
            });
        }
        else if (hasCurrent)
        {
            comparisons.Add(new ComparisonResult
            {
                Name = key,
                CurrentMeanNs = currentResult!.MeanNs,
                CurrentAllocBytes = currentResult.AllocatedBytes,
                Status = ComparisonStatus.New
            });
        }
        else if (hasBaseline)
        {
            comparisons.Add(new ComparisonResult
            {
                Name = key,
                BaselineMeanNs = baselineResult!.MeanNs,
                BaselineAllocBytes = baselineResult.AllocatedBytes,
                Status = ComparisonStatus.Removed
            });
        }
    }

    return comparisons;
}

static ComparisonStatus GetStatus(double percentChange, double threshold)
{
    if (percentChange > threshold)
    {
        return ComparisonStatus.Regression;
    }

    if (percentChange < -threshold)
    {
        return ComparisonStatus.Improvement;
    }

    return ComparisonStatus.Unchanged;
}

static string GenerateMarkdownReport(List<ComparisonResult> comparisons, double threshold)
{
    var sb = new System.Text.StringBuilder();

    var regressions = comparisons.Where(c => c.Status == ComparisonStatus.Regression).ToList();
    var improvements = comparisons.Where(c => c.Status == ComparisonStatus.Improvement).ToList();
    var unchanged = comparisons.Where(c => c.Status == ComparisonStatus.Unchanged).ToList();
    var newBenchmarks = comparisons.Where(c => c.Status == ComparisonStatus.New).ToList();
    var removed = comparisons.Where(c => c.Status == ComparisonStatus.Removed).ToList();

    // Handle case where all benchmarks are new (no baseline available)
    if (comparisons.Count > 0 && comparisons.All(c => c.Status == ComparisonStatus.New))
    {
        sb.AppendLine("### No Baseline Available");
        sb.AppendLine();
        sb.AppendLine("> **Note:** No baseline benchmarks found for comparison. This typically happens when:");
        sb.AppendLine("> - New benchmark files are added in this PR");
        sb.AppendLine("> - The benchmark filter doesn't match any benchmarks in the base branch");
        sb.AppendLine();
        sb.AppendLine("The benchmarks below establish a new baseline:");
        sb.AppendLine();
        sb.AppendLine("| Benchmark | Time | Allocated |");
        sb.AppendLine("|-----------|------|-----------|");
        foreach (var b in newBenchmarks.OrderBy(x => x.Name))
        {
            sb.AppendLine($"| `{TruncateName(b.Name)}` | {FormatTime(b.CurrentMeanNs)} | {FormatBytes(b.CurrentAllocBytes)} |");
        }
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine("*No regressions detected (no baseline to compare against)*");
        return sb.ToString();
    }

    // Summary
    sb.AppendLine("### Summary");
    sb.AppendLine();
    sb.AppendLine("| Category | Count |");
    sb.AppendLine("|----------|-------|");
    sb.AppendLine($"| :red_circle: Regressions (>{threshold}% slower) | {regressions.Count} |");
    sb.AppendLine($"| :green_circle: Improvements (>{threshold}% faster) | {improvements.Count} |");
    sb.AppendLine($"| :white_circle: Unchanged | {unchanged.Count} |");
    if (newBenchmarks.Count > 0)
    {
        sb.AppendLine($"| :blue_circle: New | {newBenchmarks.Count} |");
    }

    if (removed.Count > 0)
    {
        sb.AppendLine($"| :orange_circle: Removed | {removed.Count} |");
    }

    sb.AppendLine();

    // Regressions (always show)
    if (regressions.Count > 0)
    {
        sb.AppendLine("### :red_circle: REGRESSION - Performance Regressions");
        sb.AppendLine();
        sb.AppendLine("| Benchmark | Baseline | Current | Change | Alloc Delta |");
        sb.AppendLine("|-----------|----------|---------|--------|-------------|");
        foreach (var r in regressions.OrderByDescending(x => x.PercentChange))
        {
            sb.AppendLine($"| `{TruncateName(r.Name)}` | {FormatTime(r.BaselineMeanNs)} | {FormatTime(r.CurrentMeanNs)} | **+{r.PercentChange:F1}%** | {FormatAllocDelta(r.AllocDelta)} |");
        }
        sb.AppendLine();
    }

    // Improvements
    if (improvements.Count > 0)
    {
        sb.AppendLine("### :green_circle: Performance Improvements");
        sb.AppendLine();
        sb.AppendLine("<details>");
        sb.AppendLine("<summary>Show improvements</summary>");
        sb.AppendLine();
        sb.AppendLine("| Benchmark | Baseline | Current | Change | Alloc Delta |");
        sb.AppendLine("|-----------|----------|---------|--------|-------------|");
        foreach (var r in improvements.OrderBy(x => x.PercentChange))
        {
            sb.AppendLine($"| `{TruncateName(r.Name)}` | {FormatTime(r.BaselineMeanNs)} | {FormatTime(r.CurrentMeanNs)} | **{r.PercentChange:F1}%** | {FormatAllocDelta(r.AllocDelta)} |");
        }
        sb.AppendLine();
        sb.AppendLine("</details>");
        sb.AppendLine();
    }

    // Unchanged (collapsed)
    if (unchanged.Count > 0)
    {
        sb.AppendLine("### :white_circle: Unchanged");
        sb.AppendLine();
        sb.AppendLine("<details>");
        sb.AppendLine("<summary>Show unchanged benchmarks</summary>");
        sb.AppendLine();
        sb.AppendLine("| Benchmark | Baseline | Current | Change |");
        sb.AppendLine("|-----------|----------|---------|--------|");
        foreach (var r in unchanged.OrderBy(x => x.Name))
        {
            var changeStr = r.PercentChange >= 0 ? $"+{r.PercentChange:F1}%" : $"{r.PercentChange:F1}%";
            sb.AppendLine($"| `{TruncateName(r.Name)}` | {FormatTime(r.BaselineMeanNs)} | {FormatTime(r.CurrentMeanNs)} | {changeStr} |");
        }
        sb.AppendLine();
        sb.AppendLine("</details>");
        sb.AppendLine();
    }

    // Footer
    sb.AppendLine("---");
    sb.AppendLine($"*Threshold: {threshold}% | Generated by BenchmarkCompare*");

    return sb.ToString();
}

static string TruncateName(string name)
{
    // Remove namespace prefixes for readability
    var lastDot = name.LastIndexOf('.');
    if (lastDot > 0)
    {
        var secondLastDot = name.LastIndexOf('.', lastDot - 1);
        if (secondLastDot > 0)
        {
            name = name[(secondLastDot + 1)..];
        }
    }

    return name.Length > 60 ? name[..57] + "..." : name;
}

static string FormatTime(double nanoseconds)
{
    return nanoseconds switch
    {
        < 1_000 => $"{nanoseconds:F2} ns",
        < 1_000_000 => $"{nanoseconds / 1_000:F2} us",
        < 1_000_000_000 => $"{nanoseconds / 1_000_000:F2} ms",
        _ => $"{nanoseconds / 1_000_000_000:F2} s"
    };
}

static string FormatAllocDelta(long bytes)
{
    if (bytes == 0)
    {
        return "-";
    }

    var sign = bytes > 0 ? "+" : "";
    return bytes switch
    {
        > -1024 and < 1024 => $"{sign}{bytes} B",
        _ => $"{sign}{bytes / 1024.0:F1} KB"
    };
}

static string FormatBytes(long bytes)
{
    if (bytes == 0)
    {
        return "-";
    }

    return bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024.0):F1} MB"
    };
}

// JSON model classes for BenchmarkDotNet output
internal sealed record BenchmarkReport
{
    public List<Benchmark>? Benchmarks { get; init; }
}

internal sealed record Benchmark
{
    public string? Type { get; init; }
    public string? Method { get; init; }
    public string? Parameters { get; init; }
    public Statistics? Statistics { get; init; }
    public MemoryInfo? Memory { get; init; }
}

internal sealed record Statistics
{
    public double Mean { get; init; }
    public double StandardDeviation { get; init; }
}

internal sealed record MemoryInfo
{
    public long BytesAllocatedPerOperation { get; init; }
}

internal sealed record BenchmarkResult
{
    public required string Name { get; init; }
    public double MeanNs { get; init; }
    public double StdDevNs { get; init; }
    public long AllocatedBytes { get; init; }
}

internal sealed record ComparisonResult
{
    public required string Name { get; init; }
    public double BaselineMeanNs { get; init; }
    public double CurrentMeanNs { get; init; }
    public double PercentChange { get; init; }
    public long BaselineAllocBytes { get; init; }
    public long CurrentAllocBytes { get; init; }
    public long AllocDelta { get; init; }
    public ComparisonStatus Status { get; init; }
}

internal enum ComparisonStatus
{
    Unchanged,
    Improvement,
    Regression,
    New,
    Removed
}
