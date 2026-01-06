namespace KeenEyes.Replay.Ghost;

/// <summary>
/// Represents the playback state of a ghost player.
/// </summary>
public enum GhostPlaybackState
{
    /// <summary>
    /// Playback is stopped. The ghost is at the beginning or has been reset.
    /// </summary>
    Stopped = 0,

    /// <summary>
    /// Playback is active. The ghost position is being updated.
    /// </summary>
    Playing = 1,

    /// <summary>
    /// Playback is paused. The ghost position is frozen at the current frame.
    /// </summary>
    Paused = 2
}
