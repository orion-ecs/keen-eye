namespace KeenEyes.TestBridge.Replay;

/// <summary>
/// Current state of replay playback.
/// </summary>
public sealed record PlaybackStateSnapshot
{
    /// <summary>
    /// Gets whether a replay is loaded.
    /// </summary>
    public required bool IsLoaded { get; init; }

    /// <summary>
    /// Gets whether playback is currently active.
    /// </summary>
    public required bool IsPlaying { get; init; }

    /// <summary>
    /// Gets whether playback is paused.
    /// </summary>
    public required bool IsPaused { get; init; }

    /// <summary>
    /// Gets whether playback is stopped.
    /// </summary>
    public required bool IsStopped { get; init; }

    /// <summary>
    /// Gets the current frame index (0-based).
    /// </summary>
    public required int CurrentFrame { get; init; }

    /// <summary>
    /// Gets the total number of frames in the replay.
    /// </summary>
    public required int TotalFrames { get; init; }

    /// <summary>
    /// Gets the current playback time in seconds.
    /// </summary>
    public required float CurrentTimeSeconds { get; init; }

    /// <summary>
    /// Gets the total duration in seconds.
    /// </summary>
    public required float TotalTimeSeconds { get; init; }

    /// <summary>
    /// Gets the current playback speed multiplier.
    /// </summary>
    public required float PlaybackSpeed { get; init; }

    /// <summary>
    /// Gets the name of the loaded replay.
    /// </summary>
    public string? ReplayName { get; init; }

    /// <summary>
    /// Gets the progress as a percentage (0-100).
    /// </summary>
    public float ProgressPercent => TotalFrames > 0
        ? (float)CurrentFrame / TotalFrames * 100f
        : 0f;
}
