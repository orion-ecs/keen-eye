namespace KeenEyes.Sample.Racing;

/// <summary>
/// Tracks how far a car has travelled along the track during the current lap.
/// </summary>
/// <remarks>
/// <para>
/// Distance is measured in world units from the start/finish line. It is the
/// key that drives <see cref="KeenEyes.Replay.Ghost.GhostSyncMode.DistanceSynced"/>
/// ghost playback: the ghost is positioned at wherever it was when it had
/// travelled the same distance, so the live car and its ghosts can be compared
/// at identical points on the track regardless of who is faster.
/// </para>
/// </remarks>
[Component(Serializable = true)]
public partial struct TrackPosition
{
    /// <summary>
    /// Cumulative distance travelled along the track this lap, in world units.
    /// </summary>
    public float Distance;
}
