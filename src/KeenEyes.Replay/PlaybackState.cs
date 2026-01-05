namespace KeenEyes.Replay;

/// <summary>
/// Represents the current state of replay playback.
/// </summary>
/// <remarks>
/// The playback state controls how the <see cref="ReplayPlayer"/> behaves during
/// <see cref="ReplayPlayer.Update"/> calls:
/// <list type="bullet">
/// <item><description><see cref="Stopped"/> - Player is idle, no replay loaded or playback reset to beginning.</description></item>
/// <item><description><see cref="Playing"/> - Playback is active, frames advance during Update calls.</description></item>
/// <item><description><see cref="Paused"/> - Playback is suspended, Update calls do not advance frames.</description></item>
/// </list>
/// </remarks>
public enum PlaybackState
{
    /// <summary>
    /// Playback is stopped. No replay is loaded or playback has been reset.
    /// </summary>
    /// <remarks>
    /// In this state, <see cref="ReplayPlayer.CurrentFrame"/> is 0 and
    /// <see cref="ReplayPlayer.CurrentTime"/> is <see cref="TimeSpan.Zero"/>.
    /// Call <see cref="ReplayPlayer.Play"/> to begin playback.
    /// </remarks>
    Stopped = 0,

    /// <summary>
    /// Playback is actively progressing through the replay.
    /// </summary>
    /// <remarks>
    /// In this state, each call to <see cref="ReplayPlayer.Update"/> advances
    /// the playback position based on the provided delta time.
    /// </remarks>
    Playing = 1,

    /// <summary>
    /// Playback is paused at the current position.
    /// </summary>
    /// <remarks>
    /// In this state, <see cref="ReplayPlayer.Update"/> calls do not advance
    /// the playback position. Call <see cref="ReplayPlayer.Play"/> to resume
    /// or <see cref="ReplayPlayer.Stop"/> to reset to the beginning.
    /// </remarks>
    Paused = 2
}
