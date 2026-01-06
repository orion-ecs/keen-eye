using System.Diagnostics;

namespace KeenEyes.Debugging.Timeline;

/// <summary>
/// Records system execution history for performance analysis and debugging.
/// </summary>
/// <remarks>
/// <para>
/// The TimelineRecorder captures detailed execution history including system name,
/// phase, start time, duration, and frame number. It maintains a rolling buffer
/// of recent frames to manage memory usage.
/// </para>
/// <para>
/// Use this with SystemHooks for automatic recording of all system executions,
/// or manually instrument specific code paths.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var recorder = world.GetExtension&lt;TimelineRecorder&gt;();
///
/// // Get all entries for the current frame
/// var frameEntries = recorder.GetEntriesForFrame(recorder.CurrentFrame);
///
/// // Get recent execution history
/// var history = recorder.GetRecentEntries(100);
///
/// // Export to JSON for visualization
/// var json = TimelineExporter.ToJson(recorder.GetAllEntries());
/// </code>
/// </example>
/// <param name="maxFramesToKeep">Maximum number of frames to keep in history. Default is 300 (about 5 seconds at 60fps).</param>
public sealed class TimelineRecorder(int maxFramesToKeep = 300)
{
    private readonly Lock syncLock = new();
    private readonly List<TimelineEntry> entries = [];
    private readonly Dictionary<string, long> activeRecordings = [];
    private long recordingStartTicks = Stopwatch.GetTimestamp();
    private long currentFrame;
    private SystemPhase currentPhase;

    /// <summary>
    /// Gets the current frame number.
    /// </summary>
    public long CurrentFrame => currentFrame;

    /// <summary>
    /// Gets the total number of recorded entries.
    /// </summary>
    public int EntryCount
    {
        get
        {
            lock (syncLock)
            {
                return entries.Count;
            }
        }
    }

    /// <summary>
    /// Gets whether recording is currently enabled.
    /// </summary>
    public bool IsRecording { get; private set; } = true;

    /// <summary>
    /// Begins recording execution of a system.
    /// </summary>
    /// <param name="systemName">The name of the system being executed.</param>
    /// <remarks>
    /// Call <see cref="EndRecording"/> with the same system name to complete the recording.
    /// </remarks>
    public void BeginRecording(string systemName)
    {
        if (!IsRecording)
        {
            return;
        }

        lock (syncLock)
        {
            activeRecordings[systemName] = Stopwatch.GetTimestamp();
        }
    }

    /// <summary>
    /// Ends recording execution of a system and stores the entry.
    /// </summary>
    /// <param name="systemName">The name of the system that completed execution.</param>
    /// <param name="deltaTime">The delta time passed to the system's Update method.</param>
    public void EndRecording(string systemName, float deltaTime)
    {
        if (!IsRecording)
        {
            return;
        }

        lock (syncLock)
        {
            if (!activeRecordings.TryGetValue(systemName, out var startTimestamp))
            {
                return;
            }

            var duration = Stopwatch.GetElapsedTime(startTimestamp);
            var startTicks = startTimestamp - recordingStartTicks;

            activeRecordings.Remove(systemName);

            entries.Add(new TimelineEntry
            {
                FrameNumber = currentFrame,
                SystemName = systemName,
                Phase = currentPhase,
                StartTicks = startTicks,
                Duration = duration,
                DeltaTime = deltaTime
            });
        }
    }

    /// <summary>
    /// Advances to the next frame and cleans up old entries.
    /// </summary>
    /// <remarks>
    /// Call this at the end of each frame to properly separate entries
    /// and maintain the rolling buffer of recent frames.
    /// </remarks>
    public void AdvanceFrame()
    {
        lock (syncLock)
        {
            currentFrame++;

            // Clean up old entries
            if (currentFrame > maxFramesToKeep)
            {
                var cutoffFrame = currentFrame - maxFramesToKeep;
                entries.RemoveAll(e => e.FrameNumber < cutoffFrame);
            }
        }
    }

    /// <summary>
    /// Sets the current execution phase for subsequent recordings.
    /// </summary>
    /// <param name="phase">The current system phase.</param>
    public void SetPhase(SystemPhase phase)
    {
        currentPhase = phase;
    }

    /// <summary>
    /// Gets all entries for a specific frame.
    /// </summary>
    /// <param name="frameNumber">The frame number to query.</param>
    /// <returns>All entries recorded during the specified frame.</returns>
    public IReadOnlyList<TimelineEntry> GetEntriesForFrame(long frameNumber)
    {
        lock (syncLock)
        {
            return entries.Where(e => e.FrameNumber == frameNumber).ToList();
        }
    }

    /// <summary>
    /// Gets the most recent entries.
    /// </summary>
    /// <param name="count">Maximum number of entries to return.</param>
    /// <returns>The most recent entries, up to the specified count.</returns>
    public IReadOnlyList<TimelineEntry> GetRecentEntries(int count)
    {
        lock (syncLock)
        {
            return entries.TakeLast(count).ToList();
        }
    }

    /// <summary>
    /// Gets all recorded entries.
    /// </summary>
    /// <returns>A snapshot of all recorded entries.</returns>
    public IReadOnlyList<TimelineEntry> GetAllEntries()
    {
        lock (syncLock)
        {
            return entries.ToList();
        }
    }

    /// <summary>
    /// Gets entries grouped by system name.
    /// </summary>
    /// <returns>A dictionary mapping system names to their entries.</returns>
    public Dictionary<string, List<TimelineEntry>> GetEntriesBySystem()
    {
        lock (syncLock)
        {
            return entries.GroupBy(e => e.SystemName)
                .ToDictionary(g => g.Key, g => g.ToList());
        }
    }

    /// <summary>
    /// Gets entries grouped by phase.
    /// </summary>
    /// <returns>A dictionary mapping phases to their entries.</returns>
    public Dictionary<SystemPhase, List<TimelineEntry>> GetEntriesByPhase()
    {
        lock (syncLock)
        {
            return entries.GroupBy(e => e.Phase)
                .ToDictionary(g => g.Key, g => g.ToList());
        }
    }

    /// <summary>
    /// Calculates summary statistics for each system.
    /// </summary>
    /// <returns>A dictionary mapping system names to their statistics.</returns>
    public Dictionary<string, TimelineSystemStats> GetSystemStats()
    {
        lock (syncLock)
        {
            return entries.GroupBy(e => e.SystemName)
                .ToDictionary(g => g.Key, g => new TimelineSystemStats
                {
                    SystemName = g.Key,
                    CallCount = g.Count(),
                    TotalTime = TimeSpan.FromTicks(g.Sum(e => e.Duration.Ticks)),
                    AverageTime = TimeSpan.FromTicks((long)g.Average(e => e.Duration.Ticks)),
                    MinTime = TimeSpan.FromTicks(g.Min(e => e.Duration.Ticks)),
                    MaxTime = TimeSpan.FromTicks(g.Max(e => e.Duration.Ticks))
                });
        }
    }

    /// <summary>
    /// Enables recording of timeline entries.
    /// </summary>
    public void EnableRecording()
    {
        IsRecording = true;
    }

    /// <summary>
    /// Disables recording of timeline entries.
    /// </summary>
    /// <remarks>
    /// While recording is disabled, BeginRecording and EndRecording calls are no-ops.
    /// Existing entries are preserved.
    /// </remarks>
    public void DisableRecording()
    {
        IsRecording = false;
    }

    /// <summary>
    /// Clears all recorded entries and resets the frame counter.
    /// </summary>
    public void Reset()
    {
        lock (syncLock)
        {
            entries.Clear();
            activeRecordings.Clear();
            currentFrame = 0;
            recordingStartTicks = Stopwatch.GetTimestamp();
        }
    }
}

/// <summary>
/// Summary statistics for a system's execution in the timeline.
/// </summary>
public readonly record struct TimelineSystemStats
{
    /// <summary>
    /// Gets the name of the system.
    /// </summary>
    public required string SystemName { get; init; }

    /// <summary>
    /// Gets the number of times the system was executed.
    /// </summary>
    public required int CallCount { get; init; }

    /// <summary>
    /// Gets the total execution time across all calls.
    /// </summary>
    public required TimeSpan TotalTime { get; init; }

    /// <summary>
    /// Gets the average execution time per call.
    /// </summary>
    public required TimeSpan AverageTime { get; init; }

    /// <summary>
    /// Gets the minimum execution time observed.
    /// </summary>
    public required TimeSpan MinTime { get; init; }

    /// <summary>
    /// Gets the maximum execution time observed.
    /// </summary>
    public required TimeSpan MaxTime { get; init; }
}
