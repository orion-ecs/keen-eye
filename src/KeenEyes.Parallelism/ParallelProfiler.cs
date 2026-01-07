using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace KeenEyes.Parallelism;

/// <summary>
/// Provides profiling and diagnostics for parallel system execution.
/// </summary>
/// <remarks>
/// <para>
/// The profiler collects execution timing data for systems and batches,
/// calculates performance metrics like parallel efficiency and thread utilization,
/// and helps identify bottlenecks in the system execution pipeline.
/// </para>
/// <para>
/// Enable profiling on the scheduler to begin collecting data. Use
/// <see cref="GetMetrics"/> to retrieve performance analysis and
/// <see cref="ExportDependencyGraph"/> to visualize system dependencies.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var scheduler = world.GetExtension&lt;ParallelSystemScheduler&gt;();
/// var profiler = new ParallelProfiler(scheduler);
///
/// // Enable profiling
/// profiler.Start();
///
/// // Run some updates
/// for (int i = 0; i &lt; 100; i++)
/// {
///     scheduler.UpdateParallel(0.016f);
/// }
///
/// // Analyze results
/// var metrics = profiler.GetMetrics();
/// Console.WriteLine($"Parallel efficiency: {metrics.ParallelEfficiency:P}");
///
/// // Export dependency graph for visualization
/// var dot = profiler.ExportDependencyGraph();
/// File.WriteAllText("dependencies.dot", dot);
/// </code>
/// </example>
/// <param name="scheduler">The parallel system scheduler to profile.</param>
public sealed class ParallelProfiler(ParallelSystemScheduler scheduler) : IDisposable
{
    private readonly ParallelSystemScheduler scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
    private readonly ConcurrentDictionary<Type, SystemTimings> systemTimings = new();
    private readonly ConcurrentBag<BatchTiming> batchTimings = [];
    private readonly ConcurrentBag<int> threadIdsUsed = [];
    private readonly Stopwatch frameStopwatch = new();

    private long totalFrameTime;
    private int frameCount;
    private volatile bool isEnabled;
    private volatile bool isDisposed;

    /// <summary>
    /// Gets whether profiling is currently enabled.
    /// </summary>
    public bool IsEnabled => isEnabled;

    /// <summary>
    /// Gets the total number of frames profiled.
    /// </summary>
    public int FrameCount => frameCount;

    /// <summary>
    /// Starts profiling and clears any previous data.
    /// </summary>
    public void Start()
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);

        Clear();
        isEnabled = true;
    }

    /// <summary>
    /// Stops profiling while preserving collected data.
    /// </summary>
    public void Stop()
    {
        isEnabled = false;
    }

    /// <summary>
    /// Clears all collected profiling data.
    /// </summary>
    public void Clear()
    {
        systemTimings.Clear();
        batchTimings.Clear();
        threadIdsUsed.Clear();
        totalFrameTime = 0;
        frameCount = 0;
    }

    /// <summary>
    /// Records the start of a profiling frame.
    /// </summary>
    /// <remarks>
    /// Call this at the beginning of each update loop when manually profiling.
    /// </remarks>
    public void BeginFrame()
    {
        if (!isEnabled)
        {
            return;
        }

        frameStopwatch.Restart();
    }

    /// <summary>
    /// Records the end of a profiling frame.
    /// </summary>
    /// <remarks>
    /// Call this at the end of each update loop when manually profiling.
    /// </remarks>
    public void EndFrame()
    {
        if (!isEnabled)
        {
            return;
        }

        frameStopwatch.Stop();
        Interlocked.Add(ref totalFrameTime, frameStopwatch.ElapsedTicks);
        Interlocked.Increment(ref frameCount);
    }

    /// <summary>
    /// Records timing data for a system execution.
    /// </summary>
    /// <param name="systemType">The type of system that was executed.</param>
    /// <param name="elapsedTicks">The execution time in stopwatch ticks.</param>
    public void RecordSystemExecution(Type systemType, long elapsedTicks)
    {
        if (!isEnabled)
        {
            return;
        }

        threadIdsUsed.Add(Environment.CurrentManagedThreadId);

        var timings = systemTimings.GetOrAdd(systemType, _ => new SystemTimings());
        timings.Record(elapsedTicks);
    }

    /// <summary>
    /// Records timing data for a batch execution.
    /// </summary>
    /// <param name="batchIndex">The index of the batch.</param>
    /// <param name="systemCount">The number of systems in the batch.</param>
    /// <param name="elapsedTicks">The total execution time in stopwatch ticks.</param>
    public void RecordBatchExecution(int batchIndex, int systemCount, long elapsedTicks)
    {
        if (!isEnabled)
        {
            return;
        }

        batchTimings.Add(new BatchTiming(batchIndex, systemCount, elapsedTicks));
    }

    /// <summary>
    /// Gets comprehensive performance metrics based on collected data.
    /// </summary>
    /// <returns>A metrics object containing performance analysis.</returns>
    public ProfilerMetrics GetMetrics()
    {
        var analysis = scheduler.GetAnalysis();
        var systemStats = new Dictionary<Type, SystemStats>();

        foreach (var kvp in systemTimings)
        {
            var timings = kvp.Value;
            systemStats[kvp.Key] = new SystemStats(
                ExecutionCount: timings.ExecutionCount,
                TotalTimeMs: TicksToMs(timings.TotalTicks),
                AverageTimeMs: timings.ExecutionCount > 0
                    ? TicksToMs(timings.TotalTicks / timings.ExecutionCount)
                    : 0,
                MinTimeMs: TicksToMs(timings.MinTicks),
                MaxTimeMs: TicksToMs(timings.MaxTicks));
        }

        // Calculate batch statistics
        var batchStats = new List<BatchStats>();
        var batchGroups = batchTimings.GroupBy(b => b.BatchIndex).OrderBy(g => g.Key);
        foreach (var group in batchGroups)
        {
            var timingsArr = group.ToArray();
            var totalTicks = timingsArr.Sum(t => t.ElapsedTicks);
            var avgTicks = totalTicks / timingsArr.Length;
            batchStats.Add(new BatchStats(
                BatchIndex: group.Key,
                SystemCount: timingsArr[0].SystemCount,
                ExecutionCount: timingsArr.Length,
                AverageTimeMs: TicksToMs(avgTicks)));
        }

        // Calculate parallel efficiency
        var totalSystemTime = systemTimings.Values.Sum(t => t.TotalTicks);
        var avgFrameTime = frameCount > 0 ? totalFrameTime / frameCount : 0;
        var parallelEfficiency = avgFrameTime > 0 && totalSystemTime > 0
            ? Math.Min(1.0, (double)totalSystemTime / (avgFrameTime * frameCount) / Environment.ProcessorCount)
            : 0;

        // Calculate thread utilization
        var uniqueThreads = threadIdsUsed.Distinct().Count();
        var threadUtilization = Environment.ProcessorCount > 0
            ? (double)uniqueThreads / Environment.ProcessorCount
            : 0;

        // Identify bottlenecks
        var bottlenecks = IdentifyBottlenecks(analysis, systemStats);

        return new ProfilerMetrics(
            TotalFrames: frameCount,
            AverageFrameTimeMs: frameCount > 0 ? TicksToMs(totalFrameTime / frameCount) : 0,
            ParallelEfficiency: parallelEfficiency,
            ThreadUtilization: threadUtilization,
            UniqueThreadsUsed: uniqueThreads,
            BatchCount: analysis.BatchCount,
            ConflictCount: analysis.ConflictCount,
            MaxParallelism: analysis.MaxParallelism,
            SystemStats: systemStats,
            BatchStats: batchStats,
            Bottlenecks: bottlenecks);
    }

    /// <summary>
    /// Exports the system dependency graph in DOT format for visualization.
    /// </summary>
    /// <returns>A DOT format string representing the dependency graph.</returns>
    /// <remarks>
    /// <para>
    /// The generated DOT file can be visualized using Graphviz or similar tools.
    /// Systems with conflicts are connected with red edges.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var dot = profiler.ExportDependencyGraph();
    /// File.WriteAllText("dependencies.dot", dot);
    /// // Then run: dot -Tpng dependencies.dot -o dependencies.png
    /// </code>
    /// </example>
    public string ExportDependencyGraph()
    {
        var analysis = scheduler.GetAnalysis();
        var sb = new StringBuilder();

        sb.AppendLine("digraph SystemDependencies {");
        sb.AppendLine("    rankdir=LR;");
        sb.AppendLine("    node [shape=box, style=filled, fillcolor=lightblue];");
        sb.AppendLine();

        // Add batch subgraphs
        var batches = scheduler.GetBatches();
        for (int i = 0; i < batches.Count; i++)
        {
            var batch = batches[i];
            sb.AppendLine($"    subgraph cluster_{i} {{");
            sb.AppendLine($"        label=\"Batch {i}\";");
            sb.AppendLine("        style=filled;");
            sb.AppendLine("        fillcolor=lightyellow;");

            foreach (var system in batch.Systems)
            {
                var typeName = system.GetType().Name;
                var safeName = SanitizeNodeName(typeName);
                sb.AppendLine($"        {safeName} [label=\"{typeName}\"];");
            }
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        // Add conflict edges (red)
        sb.AppendLine("    // Conflicts (prevents parallelization)");
        foreach (var conflict in analysis.Conflicts)
        {
            var nameA = SanitizeNodeName(conflict.SystemA.Name);
            var nameB = SanitizeNodeName(conflict.SystemB.Name);
            var components = string.Join(", ", conflict.ConflictingComponents.Select(c => c.Name));
            sb.AppendLine($"    {nameA} -> {nameB} [style=dashed, color=red, label=\"{components}\"];");
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Exports detailed per-system timing breakdown.
    /// </summary>
    /// <returns>A formatted string containing timing data for each system.</returns>
    public string ExportTimingReport()
    {
        var metrics = GetMetrics();
        var sb = new StringBuilder();

        sb.AppendLine("=== Parallel System Profiler Report ===");
        sb.AppendLine();
        sb.AppendLine($"Total Frames: {metrics.TotalFrames}");
        sb.AppendLine($"Average Frame Time: {metrics.AverageFrameTimeMs:F3} ms");
        sb.AppendLine($"Parallel Efficiency: {metrics.ParallelEfficiency:P1}");
        sb.AppendLine($"Thread Utilization: {metrics.ThreadUtilization:P1} ({metrics.UniqueThreadsUsed}/{Environment.ProcessorCount} threads)");
        sb.AppendLine($"Batch Count: {metrics.BatchCount}");
        sb.AppendLine($"Conflict Count: {metrics.ConflictCount}");
        sb.AppendLine($"Max Parallelism: {metrics.MaxParallelism}");
        sb.AppendLine();

        sb.AppendLine("--- System Timings ---");
        foreach (var kvp in metrics.SystemStats.OrderByDescending(s => s.Value.TotalTimeMs))
        {
            var stats = kvp.Value;
            sb.AppendLine($"  {kvp.Key.Name}:");
            sb.AppendLine($"    Executions: {stats.ExecutionCount}");
            sb.AppendLine($"    Total: {stats.TotalTimeMs:F3} ms");
            sb.AppendLine($"    Average: {stats.AverageTimeMs:F3} ms");
            sb.AppendLine($"    Min/Max: {stats.MinTimeMs:F3} / {stats.MaxTimeMs:F3} ms");
        }

        sb.AppendLine();
        sb.AppendLine("--- Batch Timings ---");
        foreach (var batch in metrics.BatchStats)
        {
            sb.AppendLine($"  Batch {batch.BatchIndex}: {batch.SystemCount} systems, avg {batch.AverageTimeMs:F3} ms ({batch.ExecutionCount} runs)");
        }

        if (metrics.Bottlenecks.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("--- Bottlenecks ---");
            foreach (var bottleneck in metrics.Bottlenecks)
            {
                sb.AppendLine($"  {bottleneck.SystemType.Name}: {bottleneck.Reason}");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Releases resources used by the profiler.
    /// </summary>
    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        isDisposed = true;
        isEnabled = false;
        Clear();
    }

    private static double TicksToMs(long ticks)
    {
        return (double)ticks / Stopwatch.Frequency * 1000;
    }

    private static string SanitizeNodeName(string name)
    {
        return name.Replace(".", "_").Replace("<", "_").Replace(">", "_").Replace(",", "_");
    }

    private static IReadOnlyList<BottleneckInfo> IdentifyBottlenecks(BatchAnalysis analysis, Dictionary<Type, SystemStats> systemStats)
    {
        var bottlenecks = new List<BottleneckInfo>();

        // Find systems that appear in most conflicts
        var conflictCounts = new Dictionary<Type, int>();
        foreach (var conflict in analysis.Conflicts)
        {
            conflictCounts.TryGetValue(conflict.SystemA, out var countA);
            conflictCounts[conflict.SystemA] = countA + 1;

            conflictCounts.TryGetValue(conflict.SystemB, out var countB);
            conflictCounts[conflict.SystemB] = countB + 1;
        }

        foreach (var kvp in conflictCounts.OrderByDescending(c => c.Value))
        {
            if (kvp.Value >= 2)
            {
                bottlenecks.Add(new BottleneckInfo(
                    kvp.Key,
                    $"Involved in {kvp.Value} dependency conflicts, preventing parallelization"));
            }
        }

        // Find slow systems that dominate batch time
        if (systemStats.Count > 0)
        {
            var totalTime = systemStats.Values.Sum(s => s.TotalTimeMs);
            foreach (var kvp in systemStats)
            {
                var percentage = kvp.Value.TotalTimeMs / totalTime;
                if (percentage > 0.5 && kvp.Value.AverageTimeMs > 1.0)
                {
                    bottlenecks.Add(new BottleneckInfo(
                        kvp.Key,
                        $"Dominates execution time ({percentage:P0}), avg {kvp.Value.AverageTimeMs:F2}ms"));
                }
            }
        }

        return bottlenecks;
    }
}

/// <summary>
/// Thread-safe container for accumulating system timing data.
/// </summary>
internal sealed class SystemTimings
{
    private long totalTicks;
    private long minTicks = long.MaxValue;
    private long maxTicks;
    private int executionCount;

    public long TotalTicks => totalTicks;
    public long MinTicks => minTicks == long.MaxValue ? 0 : minTicks;
    public long MaxTicks => maxTicks;
    public int ExecutionCount => executionCount;

    public void Record(long ticks)
    {
        Interlocked.Add(ref totalTicks, ticks);
        Interlocked.Increment(ref executionCount);

        // Update min/max with compare-exchange loop
        long currentMin;
        do
        {
            currentMin = Interlocked.Read(ref minTicks);
            if (ticks >= currentMin)
            {
                break;
            }
        } while (Interlocked.CompareExchange(ref minTicks, ticks, currentMin) != currentMin);

        long currentMax;
        do
        {
            currentMax = Interlocked.Read(ref maxTicks);
            if (ticks <= currentMax)
            {
                break;
            }
        } while (Interlocked.CompareExchange(ref maxTicks, ticks, currentMax) != currentMax);
    }
}

/// <summary>
/// Represents timing data for a single batch execution.
/// </summary>
/// <param name="BatchIndex">The index of the batch.</param>
/// <param name="SystemCount">The number of systems in the batch.</param>
/// <param name="ElapsedTicks">The execution time in stopwatch ticks.</param>
internal readonly record struct BatchTiming(int BatchIndex, int SystemCount, long ElapsedTicks);

/// <summary>
/// Contains comprehensive performance metrics from the profiler.
/// </summary>
/// <param name="TotalFrames">Total number of frames profiled.</param>
/// <param name="AverageFrameTimeMs">Average frame time in milliseconds.</param>
/// <param name="ParallelEfficiency">Ratio of actual parallelism achieved (0-1).</param>
/// <param name="ThreadUtilization">Ratio of threads used vs available (0-1).</param>
/// <param name="UniqueThreadsUsed">Number of unique threads that executed work.</param>
/// <param name="BatchCount">Number of execution batches.</param>
/// <param name="ConflictCount">Number of dependency conflicts detected.</param>
/// <param name="MaxParallelism">Maximum systems that can run in parallel.</param>
/// <param name="SystemStats">Per-system timing statistics.</param>
/// <param name="BatchStats">Per-batch timing statistics.</param>
/// <param name="Bottlenecks">Identified performance bottlenecks.</param>
public sealed record ProfilerMetrics(
    int TotalFrames,
    double AverageFrameTimeMs,
    double ParallelEfficiency,
    double ThreadUtilization,
    int UniqueThreadsUsed,
    int BatchCount,
    int ConflictCount,
    int MaxParallelism,
    IReadOnlyDictionary<Type, SystemStats> SystemStats,
    IReadOnlyList<BatchStats> BatchStats,
    IReadOnlyList<BottleneckInfo> Bottlenecks);

/// <summary>
/// Contains timing statistics for a single system.
/// </summary>
/// <param name="ExecutionCount">Number of times the system was executed.</param>
/// <param name="TotalTimeMs">Total execution time in milliseconds.</param>
/// <param name="AverageTimeMs">Average execution time in milliseconds.</param>
/// <param name="MinTimeMs">Minimum execution time in milliseconds.</param>
/// <param name="MaxTimeMs">Maximum execution time in milliseconds.</param>
public readonly record struct SystemStats(
    int ExecutionCount,
    double TotalTimeMs,
    double AverageTimeMs,
    double MinTimeMs,
    double MaxTimeMs);

/// <summary>
/// Contains timing statistics for a batch.
/// </summary>
/// <param name="BatchIndex">The batch index.</param>
/// <param name="SystemCount">Number of systems in the batch.</param>
/// <param name="ExecutionCount">Number of times the batch was executed.</param>
/// <param name="AverageTimeMs">Average execution time in milliseconds.</param>
public readonly record struct BatchStats(
    int BatchIndex,
    int SystemCount,
    int ExecutionCount,
    double AverageTimeMs);

/// <summary>
/// Describes a detected performance bottleneck.
/// </summary>
/// <param name="SystemType">The system type causing the bottleneck.</param>
/// <param name="Reason">Description of why this is a bottleneck.</param>
public readonly record struct BottleneckInfo(Type SystemType, string Reason);
