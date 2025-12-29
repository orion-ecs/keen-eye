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
/// </remarks>
/// <param name="interpolationDelayMs">The interpolation delay in milliseconds.</param>
/// <param name="interpolator">The network interpolator for component interpolation.</param>
/// <param name="getSnapshotBuffer">Function to get the snapshot buffer for an entity.</param>
public sealed class InterpolationSystem(
    float interpolationDelayMs = 100f,
    INetworkInterpolator? interpolator = null,
    Func<Entity, SnapshotBuffer?>? getSnapshotBuffer = null) : SystemBase
{
    private readonly float interpolationDelay = interpolationDelayMs / 1000f;
    private double serverTime;

    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        // Advance server time estimate
        serverTime += deltaTime;

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
