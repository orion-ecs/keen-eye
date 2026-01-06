using System.Text;
using System.Text.Json;

namespace KeenEyes.Debugging.Timeline;

/// <summary>
/// Provides export functionality for timeline data in various formats.
/// </summary>
/// <remarks>
/// <para>
/// The TimelineExporter supports exporting timeline entries to JSON and CSV formats
/// for use with external visualization tools and analysis.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var recorder = world.GetExtension&lt;TimelineRecorder&gt;();
/// var entries = recorder.GetAllEntries();
///
/// // Export to JSON
/// var json = TimelineExporter.ToJson(entries);
/// File.WriteAllText("timeline.json", json);
///
/// // Export to CSV
/// var csv = TimelineExporter.ToCsv(entries);
/// File.WriteAllText("timeline.csv", csv);
/// </code>
/// </example>
public static class TimelineExporter
{
    /// <summary>
    /// Exports timeline entries to JSON format.
    /// </summary>
    /// <param name="entries">The entries to export.</param>
    /// <param name="indented">Whether to format the JSON with indentation.</param>
    /// <returns>A JSON string representing the timeline data.</returns>
    public static string ToJson(IReadOnlyList<TimelineEntry> entries, bool indented = true)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = indented
        };

        var exportData = entries.Select(e => new
        {
            e.FrameNumber,
            e.SystemName,
            Phase = e.Phase.ToString(),
            e.StartTicks,
            DurationMs = e.Duration.TotalMilliseconds,
            e.DeltaTime
        }).ToList();

        return JsonSerializer.Serialize(exportData, options);
    }

    /// <summary>
    /// Exports timeline entries to CSV format.
    /// </summary>
    /// <param name="entries">The entries to export.</param>
    /// <returns>A CSV string representing the timeline data.</returns>
    public static string ToCsv(IReadOnlyList<TimelineEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("FrameNumber,SystemName,Phase,StartTicks,DurationMs,DeltaTime");

        foreach (var entry in entries)
        {
            sb.AppendLine($"{entry.FrameNumber},{EscapeCsv(entry.SystemName)},{entry.Phase},{entry.StartTicks},{entry.Duration.TotalMilliseconds:F4},{entry.DeltaTime:F6}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Exports system statistics to JSON format.
    /// </summary>
    /// <param name="stats">The statistics to export.</param>
    /// <param name="indented">Whether to format the JSON with indentation.</param>
    /// <returns>A JSON string representing the statistics.</returns>
    public static string StatsToJson(IReadOnlyDictionary<string, TimelineSystemStats> stats, bool indented = true)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = indented
        };

        var exportData = stats.Select(kvp => new
        {
            kvp.Value.SystemName,
            kvp.Value.CallCount,
            TotalTimeMs = kvp.Value.TotalTime.TotalMilliseconds,
            AverageTimeMs = kvp.Value.AverageTime.TotalMilliseconds,
            MinTimeMs = kvp.Value.MinTime.TotalMilliseconds,
            MaxTimeMs = kvp.Value.MaxTime.TotalMilliseconds
        }).ToList();

        return JsonSerializer.Serialize(exportData, options);
    }

    /// <summary>
    /// Exports system statistics to CSV format.
    /// </summary>
    /// <param name="stats">The statistics to export.</param>
    /// <returns>A CSV string representing the statistics.</returns>
    public static string StatsToCsv(IReadOnlyDictionary<string, TimelineSystemStats> stats)
    {
        var sb = new StringBuilder();
        sb.AppendLine("SystemName,CallCount,TotalTimeMs,AverageTimeMs,MinTimeMs,MaxTimeMs");

        foreach (var (_, stat) in stats)
        {
            sb.AppendLine($"{EscapeCsv(stat.SystemName)},{stat.CallCount},{stat.TotalTime.TotalMilliseconds:F4},{stat.AverageTime.TotalMilliseconds:F4},{stat.MinTime.TotalMilliseconds:F4},{stat.MaxTime.TotalMilliseconds:F4}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generates a formatted timeline report string.
    /// </summary>
    /// <param name="recorder">The timeline recorder to generate a report for.</param>
    /// <returns>A multi-line string containing the timeline report.</returns>
    public static string GenerateReport(TimelineRecorder recorder)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Timeline Report ===");
        sb.AppendLine($"Current Frame: {recorder.CurrentFrame}");
        sb.AppendLine($"Total Entries: {recorder.EntryCount}");
        sb.AppendLine($"Recording: {(recorder.IsRecording ? "Enabled" : "Disabled")}");
        sb.AppendLine();

        var stats = recorder.GetSystemStats();
        if (stats.Count > 0)
        {
            sb.AppendLine("System Statistics:");
            sb.AppendLine($"  {"System",-30} {"Calls",8} {"Avg (ms)",12} {"Min (ms)",12} {"Max (ms)",12}");
            sb.AppendLine($"  {new string('-', 30)} {new string('-', 8)} {new string('-', 12)} {new string('-', 12)} {new string('-', 12)}");

            foreach (var stat in stats.Values.OrderByDescending(s => s.TotalTime))
            {
                sb.AppendLine($"  {stat.SystemName,-30} {stat.CallCount,8} {stat.AverageTime.TotalMilliseconds,12:F3} {stat.MinTime.TotalMilliseconds,12:F3} {stat.MaxTime.TotalMilliseconds,12:F3}");
            }
        }
        else
        {
            sb.AppendLine("No timeline entries recorded.");
        }

        return sb.ToString();
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
