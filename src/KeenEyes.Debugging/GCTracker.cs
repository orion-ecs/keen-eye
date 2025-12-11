namespace KeenEyes.Debugging;

/// <summary>
/// Tracks garbage collection allocations during system execution.
/// </summary>
/// <remarks>
/// <para>
/// The GCTracker monitors memory allocations per system to identify allocation hotspots.
/// It uses <see cref="GC.GetAllocatedBytesForCurrentThread"/> to measure allocations,
/// which provides accurate per-thread allocation tracking without significant overhead.
/// </para>
/// <para>
/// Note: This class only tracks allocations on the thread where systems execute. If your
/// ECS uses multi-threading, allocations on worker threads will not be captured.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var gcTracker = world.GetExtension&lt;GCTracker&gt;();
///
/// // Get allocations for a specific system
/// var allocations = gcTracker.GetSystemAllocations("MovementSystem");
/// Console.WriteLine($"Total allocations: {allocations.TotalBytes} bytes");
/// Console.WriteLine($"Avg per call: {allocations.AverageBytes} bytes");
///
/// // Get all allocation profiles
/// foreach (var profile in gcTracker.GetAllAllocationProfiles())
/// {
///     if (profile.TotalBytes > 1024) // Warn about systems allocating more than 1KB
///     {
///         Console.WriteLine($"⚠️ {profile.Name} allocated {profile.TotalBytes} bytes");
///     }
/// }
/// </code>
/// </example>
public sealed class GCTracker
{
    private readonly Dictionary<string, AllocationProfile> profiles = [];
    private readonly Dictionary<string, long> activeSnapshots = [];

    /// <summary>
    /// Begins tracking allocations for a sample with the specified name.
    /// </summary>
    /// <param name="name">The name of the sample (typically the system name).</param>
    /// <remarks>
    /// This method captures the current thread's allocated byte count. Call <see cref="EndTracking"/>
    /// with the same name to measure allocations that occurred between the calls.
    /// </remarks>
    public void BeginTracking(string name)
    {
        activeSnapshots[name] = GC.GetAllocatedBytesForCurrentThread();
    }

    /// <summary>
    /// Ends tracking allocations and records the amount allocated.
    /// </summary>
    /// <param name="name">The name of the sample (must match the name passed to <see cref="BeginTracking"/>).</param>
    /// <returns>The number of bytes allocated since <see cref="BeginTracking"/> was called, or 0 if no active tracking exists.</returns>
    /// <remarks>
    /// The allocation delta is calculated and added to the profile statistics for the named sample.
    /// </remarks>
    public long EndTracking(string name)
    {
        if (!activeSnapshots.TryGetValue(name, out var startBytes))
        {
            return 0;
        }

        var endBytes = GC.GetAllocatedBytesForCurrentThread();
        var allocatedBytes = endBytes - startBytes;
        activeSnapshots.Remove(name);

        if (!profiles.TryGetValue(name, out var profile))
        {
            profile = new AllocationProfile
            {
                Name = name,
                TotalBytes = 0,
                CallCount = 0,
                AverageBytes = 0,
                MinBytes = long.MaxValue,
                MaxBytes = 0
            };
            profiles[name] = profile;
        }

        var updatedProfile = profile with
        {
            TotalBytes = profile.TotalBytes + allocatedBytes,
            CallCount = profile.CallCount + 1,
            MinBytes = allocatedBytes < profile.MinBytes ? allocatedBytes : profile.MinBytes,
            MaxBytes = allocatedBytes > profile.MaxBytes ? allocatedBytes : profile.MaxBytes
        };

        updatedProfile = updatedProfile with
        {
            AverageBytes = updatedProfile.TotalBytes / updatedProfile.CallCount
        };

        profiles[name] = updatedProfile;
        return allocatedBytes;
    }

    /// <summary>
    /// Gets the allocation profile for a specific system.
    /// </summary>
    /// <param name="name">The name of the system.</param>
    /// <returns>The allocation profile if it exists; otherwise, an empty profile.</returns>
    public AllocationProfile GetSystemAllocations(string name)
    {
        if (profiles.TryGetValue(name, out var profile))
        {
            return profile;
        }

        return new AllocationProfile
        {
            Name = name,
            TotalBytes = 0,
            CallCount = 0,
            AverageBytes = 0,
            MinBytes = 0,
            MaxBytes = 0
        };
    }

    /// <summary>
    /// Gets all allocation profiles.
    /// </summary>
    /// <returns>A read-only list of all allocation profiles.</returns>
    public IReadOnlyList<AllocationProfile> GetAllAllocationProfiles()
    {
        return profiles.Values.ToList();
    }

    /// <summary>
    /// Resets all allocation tracking data.
    /// </summary>
    public void Reset()
    {
        profiles.Clear();
        activeSnapshots.Clear();
    }

    /// <summary>
    /// Gets a formatted allocation report as a string.
    /// </summary>
    /// <param name="threshold">Only include systems that allocated more than this many bytes in total. Default is 0 (include all).</param>
    /// <returns>A multi-line string containing formatted allocation statistics.</returns>
    public string GetAllocationReport(long threshold = 0)
    {
        var report = new System.Text.StringBuilder();
        report.AppendLine("=== GC Allocation Report ===");
        report.AppendLine($"{"System",-30} {"Calls",8} {"Total",12} {"Avg",12} {"Min",12} {"Max",12}");
        report.AppendLine(new string('-', 94));

        var sortedProfiles = profiles.Values
            .Where(p => p.TotalBytes >= threshold)
            .OrderByDescending(p => p.TotalBytes);

        foreach (var profile in sortedProfiles)
        {
            report.AppendLine(
                $"{profile.Name,-30} " +
                $"{profile.CallCount,8} " +
                $"{FormatBytes(profile.TotalBytes),12} " +
                $"{FormatBytes(profile.AverageBytes),12} " +
                $"{FormatBytes(profile.MinBytes),12} " +
                $"{FormatBytes(profile.MaxBytes),12}");
        }

        report.AppendLine();
        report.AppendLine($"Total tracked allocations: {FormatBytes(profiles.Values.Sum(p => p.TotalBytes))}");

        return report.ToString();
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes}B";
        }

        if (bytes < 1024 * 1024)
        {
            return $"{bytes / 1024.0:F1}KB";
        }

        return $"{bytes / (1024.0 * 1024):F1}MB";
    }
}

/// <summary>
/// Represents allocation statistics for a system.
/// </summary>
/// <remarks>
/// This record contains memory allocation metrics collected during system execution.
/// </remarks>
public readonly record struct AllocationProfile
{
    /// <summary>
    /// Gets the name of the system.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the total bytes allocated by this system across all calls.
    /// </summary>
    public required long TotalBytes { get; init; }

    /// <summary>
    /// Gets the number of times this system has been executed.
    /// </summary>
    public required int CallCount { get; init; }

    /// <summary>
    /// Gets the average bytes allocated per system execution.
    /// </summary>
    public required long AverageBytes { get; init; }

    /// <summary>
    /// Gets the minimum bytes allocated in a single execution.
    /// </summary>
    public required long MinBytes { get; init; }

    /// <summary>
    /// Gets the maximum bytes allocated in a single execution.
    /// </summary>
    public required long MaxBytes { get; init; }
}
