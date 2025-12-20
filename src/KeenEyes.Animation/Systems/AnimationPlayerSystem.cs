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
        World.TryGetExtension(out manager);
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

            // Advance time
            player.Time += deltaTime * player.Speed * clip.Speed;

            // Get wrap mode
            var wrapMode = player.WrapModeOverride ?? clip.WrapMode;

            // Handle completion
            if (wrapMode == WrapMode.Once && player.Time >= clip.Duration)
            {
                player.Time = clip.Duration;
                player.IsComplete = true;
                player.IsPlaying = false;
            }
            else
            {
                // Wrap the time
                player.Time = clip.WrapTime(player.Time);
                player.IsComplete = false;
            }
        }
    }
}
