using System.Diagnostics;

namespace KeenEyes.Debugging;

/// <summary>
/// Tracks system execution times for performance profiling.
/// </summary>
/// <remarks>
/// <para>
/// The Profiler collects detailed timing metrics for each system execution,
/// including total time, call count, average time, minimum time, and maximum time.
/// It uses high-resolution timing via <see cref="Stopwatch"/> for accurate measurements.
/// </para>
/// <para>
/// This class is designed to be used with SystemHooks for automatic profiling of all systems
/// in a world, but can also be used manually for custom profiling scenarios.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var profiler = world.GetExtension&lt;Profiler&gt;();
///
/// // Get profile for a specific system
/// var profile = profiler.GetSystemProfile("MovementSystem");
/// Console.WriteLine($"Avg: {profile.AverageTime.TotalMilliseconds:F2}ms");
///
/// // Get all profiles
/// foreach (var profile in profiler.GetAllSystemProfiles())
/// {
///     Console.WriteLine($"{profile.Name}: {profile.AverageTime.TotalMilliseconds:F2}ms");
/// }
///
/// // Reset all profiles
/// profiler.Reset();
/// </code>
/// </example>
public sealed class Profiler
{
    private readonly Dictionary<string, SystemProfile> profiles = new();
    private readonly Dictionary<string, long> activeSamples = new();

    /// <summary>
    /// Begins profiling a sample with the specified name.
    /// </summary>
    /// <param name="name">The name of the sample (typically the system name).</param>
    /// <remarks>
    /// This method records the current timestamp. Call <see cref="EndSample"/> with the same
    /// name to complete the measurement. If a sample with the same name is already active,
    /// it will be overwritten.
    /// </remarks>
    public void BeginSample(string name)
    {
        activeSamples[name] = Stopwatch.GetTimestamp();
    }

    /// <summary>
    /// Ends profiling a sample and records the elapsed time.
    /// </summary>
    /// <param name="name">The name of the sample (must match the name passed to <see cref="BeginSample"/>).</param>
    /// <remarks>
    /// If no active sample with the specified name exists, this method does nothing.
    /// The elapsed time is calculated and added to the profile statistics for the named sample.
    /// </remarks>
    public void EndSample(string name)
    {
        if (!activeSamples.TryGetValue(name, out var startTimestamp))
        {
            return;
        }

        var elapsed = Stopwatch.GetElapsedTime(startTimestamp);
        activeSamples.Remove(name);

        if (!profiles.TryGetValue(name, out var profile))
        {
            profile = new SystemProfile
            {
                Name = name,
                TotalTime = TimeSpan.Zero,
                CallCount = 0,
                AverageTime = TimeSpan.Zero,
                MinTime = TimeSpan.MaxValue,
                MaxTime = TimeSpan.Zero
            };
            profiles[name] = profile;
        }

        var updatedProfile = profile with
        {
            TotalTime = profile.TotalTime + elapsed,
            CallCount = profile.CallCount + 1,
            MinTime = elapsed < profile.MinTime ? elapsed : profile.MinTime,
            MaxTime = elapsed > profile.MaxTime ? elapsed : profile.MaxTime
        };

        updatedProfile = updatedProfile with
        {
            AverageTime = TimeSpan.FromTicks(updatedProfile.TotalTime.Ticks / updatedProfile.CallCount)
        };

        profiles[name] = updatedProfile;
    }

    /// <summary>
    /// Gets the profile for a specific system.
    /// </summary>
    /// <param name="name">The name of the system.</param>
    /// <returns>The system profile if it exists; otherwise, an empty profile.</returns>
    /// <remarks>
    /// If no profile exists for the specified name (because the system has never been profiled),
    /// this method returns a default profile with zero values.
    /// </remarks>
    public SystemProfile GetSystemProfile(string name)
    {
        if (profiles.TryGetValue(name, out var profile))
        {
            return profile;
        }

        return new SystemProfile
        {
            Name = name,
            TotalTime = TimeSpan.Zero,
            CallCount = 0,
            AverageTime = TimeSpan.Zero,
            MinTime = TimeSpan.Zero,
            MaxTime = TimeSpan.Zero
        };
    }

    /// <summary>
    /// Gets all system profiles.
    /// </summary>
    /// <returns>A read-only list of all system profiles.</returns>
    /// <remarks>
    /// The returned list is a snapshot of the current profiles. Subsequent calls to profiling
    /// methods will not affect the returned list.
    /// </remarks>
    public IReadOnlyList<SystemProfile> GetAllSystemProfiles()
    {
        return profiles.Values.ToList();
    }

    /// <summary>
    /// Resets all profiling data.
    /// </summary>
    /// <remarks>
    /// This clears all accumulated profile statistics. Active samples (those started with
    /// <see cref="BeginSample"/> but not yet ended) are also cleared.
    /// </remarks>
    public void Reset()
    {
        profiles.Clear();
        activeSamples.Clear();
    }
}

/// <summary>
/// Represents profiling statistics for a system.
/// </summary>
/// <remarks>
/// This record contains timing metrics collected during system execution, including
/// total execution time, number of calls, average time per call, and minimum/maximum times.
/// </remarks>
public readonly record struct SystemProfile
{
    /// <summary>
    /// Gets the name of the system.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the total time spent executing this system across all calls.
    /// </summary>
    public required TimeSpan TotalTime { get; init; }

    /// <summary>
    /// Gets the number of times this system has been executed.
    /// </summary>
    public required int CallCount { get; init; }

    /// <summary>
    /// Gets the average time per system execution.
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
