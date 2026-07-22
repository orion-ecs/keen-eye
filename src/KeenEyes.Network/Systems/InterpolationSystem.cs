using KeenEyes.Network.Components;
using KeenEyes.Network.Serialization;

namespace KeenEyes.Network.Systems;

/// <summary>
/// System that interpolates remote entity positions between snapshots.
/// </summary>
/// <remarks>
/// <para>
/// Remote entities are rendered behind server time to allow smooth interpolation
/// between received snapshots. This hides network jitter and provides visually
/// smooth movement.
/// </para>
/// <para>
/// The system maintains a render clock that advances with local frame time but shares
/// its origin with the tick-derived snapshot timestamps written by the network plugin
/// (<see cref="InterpolationState.FromTime"/>/<see cref="InterpolationState.ToTime"/>).
/// Whenever the clock drifts more than <see cref="MaxClockDriftSeconds"/> from the
/// latest snapshot timestamp (first snapshot after joining a long-running server, a
/// long stall, or a reconnect), it snaps to that timestamp so interpolation factors
/// are correct immediately instead of after the clock catches up.
/// </para>
/// </remarks>
/// <param name="interpolationDelayMs">The interpolation delay in milliseconds.</param>
/// <param name="interpolator">The network interpolator for component interpolation.</param>
/// <param name="getSnapshotBuffer">Function to get the snapshot buffer for an entity.</param>
public sealed class InterpolationSystem(
    float interpolationDelayMs = 100f,
    INetworkInterpolator? interpolator = null,
    Func<Entity, SnapshotBuffer?>? getSnapshotBuffer = null) : SystemBase
{
    /// <summary>
    /// The maximum allowed drift, in seconds, between the render clock and the latest
    /// snapshot timestamp before the clock snaps to the snapshot time basis.
    /// </summary>
    /// <remarks>
    /// Large enough to tolerate normal jitter between frame-time accumulation and
    /// tick-derived snapshot timestamps, small enough that joining a long-running
    /// server or resuming after a stall resynchronizes on the next update.
    /// </remarks>
    public const double MaxClockDriftSeconds = 2.0;

    private readonly float interpolationDelay = interpolationDelayMs / 1000f;
    private double serverTime;

    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        // Advance server time estimate
        serverTime += deltaTime;

        // Align the render clock with the tick-derived snapshot timestamps
        SynchronizeClock();

        // Calculate render time (behind server time by interpolation delay)
        var renderTime = serverTime - interpolationDelay;

        foreach (var entity in World.Query<Interpolated, InterpolationState>())
        {
            ref var interpState = ref World.Get<InterpolationState>(entity);

            // Calculate interpolation factor
            if (interpState.ToTime > interpState.FromTime)
            {
                var duration = interpState.ToTime - interpState.FromTime;
                var elapsed = renderTime - interpState.FromTime;
                interpState.Factor = Math.Clamp((float)(elapsed / duration), 0f, 1f);
            }

            // Apply interpolation to components if interpolator is available
            if (interpolator is not null && getSnapshotBuffer is not null)
            {
                var snapshotBuffer = getSnapshotBuffer(entity);
                if (snapshotBuffer is not null)
                {
                    ApplyInterpolation(entity, snapshotBuffer, interpState.Factor);
                }
            }
        }
    }

    private void SynchronizeClock()
    {
        // Find the newest snapshot timestamp written by the receive path. Entities with
        // no snapshots yet carry the default ToTime of 0 and are ignored.
        var latestToTime = 0.0;
        foreach (var entity in World.Query<Interpolated, InterpolationState>())
        {
            ref readonly var interpState = ref World.Get<InterpolationState>(entity);
            if (interpState.ToTime > latestToTime)
            {
                latestToTime = interpState.ToTime;
            }
        }

        if (latestToTime <= 0.0)
        {
            return;
        }

        // Snap the render clock onto the snapshot time basis when it has drifted too
        // far (first snapshots after joining, long pauses, reconnects). Within the
        // window, keep accumulating frame time so rendering stays smooth.
        if (Math.Abs(serverTime - latestToTime) > MaxClockDriftSeconds)
        {
            serverTime = latestToTime;
        }
    }

    private void ApplyInterpolation(Entity entity, SnapshotBuffer snapshotBuffer, float factor)
    {
        foreach (var (componentType, toValue) in snapshotBuffer.ToSnapshots)
        {
            if (!snapshotBuffer.FromSnapshots.TryGetValue(componentType, out var fromValue))
            {
                // No "from" snapshot yet, just apply "to" directly
                World.SetComponent(entity, componentType, toValue);
                continue;
            }

            // Interpolate between from and to
            var interpolated = interpolator!.Interpolate(componentType, fromValue, toValue, factor);
            if (interpolated is not null)
            {
                World.SetComponent(entity, componentType, interpolated);
            }
        }
    }
}
