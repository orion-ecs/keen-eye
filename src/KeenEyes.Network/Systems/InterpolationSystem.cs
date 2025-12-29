using KeenEyes.Network.Components;

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
public sealed class InterpolationSystem(float interpolationDelayMs = 100f) : SystemBase
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

            // TODO: Apply interpolation to replicated components
            // This requires the generated INetworkInterpolatable interface
        }
    }
}
