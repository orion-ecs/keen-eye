namespace KeenEyes.Replay.Ghost;

/// <summary>
/// Specifies how a ghost's playback is synchronized with the game.
/// </summary>
/// <remarks>
/// <para>
/// Different sync modes are useful for various gameplay scenarios:
/// </para>
/// <list type="bullet">
/// <item><description>
/// <b>Time-synced:</b> Best for racing games where absolute time matters.
/// The ghost position is interpolated based on elapsed game time.
/// </description></item>
/// <item><description>
/// <b>Frame-synced:</b> Best for deterministic games. The ghost advances
/// one frame per game update, regardless of delta time.
/// </description></item>
/// <item><description>
/// <b>Distance-synced:</b> Best for route comparison. The ghost position
/// is based on how far the player has traveled along a track.
/// </description></item>
/// <item><description>
/// <b>Independent:</b> The ghost plays at its own pace, useful for
/// demos or asynchronous comparisons.
/// </description></item>
/// </list>
/// </remarks>
public enum GhostSyncMode
{
    /// <summary>
    /// Ghost position is synchronized with elapsed game time.
    /// </summary>
    /// <remarks>
    /// The ghost's position is interpolated based on the elapsed time since
    /// playback started. This mode is ideal for racing games where you want
    /// to see exactly where the ghost was at each moment in time.
    /// </remarks>
    TimeSynced = 0,

    /// <summary>
    /// Ghost advances one frame per game update.
    /// </summary>
    /// <remarks>
    /// The ghost advances exactly one frame per Update call, regardless of
    /// delta time. This mode is useful for deterministic games where frame
    /// count matters more than real time.
    /// </remarks>
    FrameSynced = 1,

    /// <summary>
    /// Ghost position is synchronized with player's traveled distance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The ghost's position is determined by how far the player has traveled
    /// along a track or path. This mode requires providing the player's current
    /// distance when updating the ghost.
    /// </para>
    /// <para>
    /// This is useful for comparing routes or showing optimal paths, as it
    /// reveals exactly where the ghost was at each distance milestone.
    /// </para>
    /// </remarks>
    DistanceSynced = 2,

    /// <summary>
    /// Ghost plays independently at its own pace.
    /// </summary>
    /// <remarks>
    /// The ghost plays back at its recorded speed, independent of the player's
    /// position or timing. This mode is useful for demos, tutorials, or when
    /// you want to show a recording without comparison to live gameplay.
    /// </remarks>
    Independent = 3
}
