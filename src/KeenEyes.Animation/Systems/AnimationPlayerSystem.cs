using KeenEyes.Animation.Components;
using KeenEyes.Animation.Data;

namespace KeenEyes.Animation.Systems;

/// <summary>
/// System that updates animation playback state for entities with AnimationPlayer components.
/// </summary>
/// <remarks>
/// <para>
/// This system advances the playback time for all active AnimationPlayer components
/// and handles wrap modes (loop, ping-pong, once).
/// </para>
/// <para>
/// For skeletal animation, a separate system should sample the clip data and
/// apply poses to bone entities based on the current playback time.
/// </para>
/// </remarks>
public sealed class AnimationPlayerSystem : SystemBase
{
    private AnimationManager? manager;

    /// <inheritdoc />
    protected override void OnInitialize()
    {
        World.TryGetExtension<AnimationManager>(out manager);
    }

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        manager ??= World.TryGetExtension<AnimationManager>(out var m) ? m : null;
        if (manager == null)
        {
            return;
        }

        foreach (var entity in World.Query<AnimationPlayer>())
        {
            ref var player = ref World.Get<AnimationPlayer>(entity);

            if (!player.IsPlaying || player.ClipId < 0)
            {
                continue;
            }

            if (!manager.TryGetClip(player.ClipId, out var clip) || clip == null)
            {
                continue;
            }

            // Save previous time for event detection
            player.PreviousTime = player.Time;

            // Signed timeline step for this frame
            var step = deltaTime * player.Speed * clip.Speed;

            // Get wrap mode
            var wrapMode = player.WrapModeOverride ?? clip.WrapMode;

            // Handle completion and wrap
            if (clip.Duration <= 0f)
            {
                player.Time = 0f;
                player.IsComplete = true;
                player.IsPlaying = false;
            }
            else if (wrapMode == WrapMode.PingPong)
            {
                // Ping-pong reflects the timeline, so the stored Time is not monotonic.
                // Advance from the current reflected position using the tracked direction
                // so the half-cycle parity survives across frames.
                (player.Time, player.PingPongReversed) =
                    AdvancePingPong(player.Time, player.PingPongReversed, step, clip.Duration);
                player.IsComplete = false;
            }
            else
            {
                player.Time += step;

                if (wrapMode == WrapMode.Once && player.Time >= clip.Duration)
                {
                    player.Time = clip.Duration;
                    player.IsComplete = true;
                    player.IsPlaying = false;
                }
                else if (wrapMode == WrapMode.ClampForever)
                {
                    player.Time = Math.Max(player.Time, 0f);
                    player.IsComplete = player.Time >= clip.Duration;
                }
                else
                {
                    // Wrap the time based on the effective wrap mode
                    player.Time = WrapTime(player.Time, clip.Duration, wrapMode);
                    player.IsComplete = false;
                }
            }
        }
    }

    private static float WrapTime(float time, float duration, WrapMode wrapMode)
    {
        return wrapMode switch
        {
            WrapMode.Once => Math.Clamp(time, 0f, duration),
            WrapMode.Loop => WrapMath.Repeat(time, duration),
            WrapMode.ClampForever => Math.Max(time, 0f),
            _ => time
        };
    }

    /// <summary>
    /// Advances a ping-pong timeline by a signed step, reflecting at both boundaries.
    /// </summary>
    /// <returns>The new reflected time and whether travel is now in reverse.</returns>
    private static (float Time, bool Reversed) AdvancePingPong(float time, bool reversed, float step, float duration)
    {
        var period = duration * 2f;

        // Reconstruct the monotonic phase from the reflected position + direction,
        // advance it, then fold back into a triangle wave.
        var phase = reversed ? period - time : time;
        var folded = WrapMath.Repeat(phase + step, period);

        return folded <= duration ? (folded, false) : (period - folded, true);
    }
}
