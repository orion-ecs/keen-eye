using KeenEyes.Animation.Components;
using KeenEyes.Animation.Data;
using KeenEyes.Animation.Events;

namespace KeenEyes.Animation.Systems;

/// <summary>
/// System that detects and publishes animation events during playback.
/// </summary>
/// <remarks>
/// <para>
/// This system runs after <see cref="AnimationPlayerSystem"/> and checks each playing animation
/// for events that occurred between the previous frame's time and the current time.
/// When events are detected, they are published via the world's event bus.
/// </para>
/// <para>
/// Events are defined in <see cref="AnimationClip.Events"/> and subscribers can react to them:
/// <code>
/// world.Events.Subscribe&lt;AnimationEventTriggeredEvent&gt;(e =>
/// {
///     Console.WriteLine($"Event {e.EventName} triggered on entity {e.Entity}");
/// });
/// </code>
/// </para>
/// </remarks>
public sealed class AnimationEventSystem : SystemBase
{
    private AnimationManager? manager;
    private readonly List<AnimationEvent> triggeredEvents = [];

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

            if (player.ClipId < 0)
            {
                continue;
            }

            // Process while playing, and also on the frame playback completes: the player
            // system (which runs first) clears IsPlaying on the completion frame, so gating
            // solely on IsPlaying would drop the final advanced range's events.
            if (!player.IsPlaying && !player.IsComplete)
            {
                continue;
            }

            if (!manager.TryGetClip(player.ClipId, out var clip) || clip == null)
            {
                continue;
            }

            // Get events that occurred during this frame
            triggeredEvents.Clear();
            clip.Events.GetEventsInRange(player.PreviousTime, player.Time, triggeredEvents);

            // Publish each triggered event
            foreach (var evt in triggeredEvents)
            {
                World.Send(new AnimationEventTriggeredEvent(
                    entity,
                    evt.EventName,
                    evt.Parameter,
                    evt.Time,
                    player.ClipId));
            }

            // A completed, stopped player is no longer advanced by AnimationPlayerSystem,
            // so its [PreviousTime, Time] window would repeat forever. Collapse it once the
            // final events have been dispatched to prevent re-firing on subsequent frames.
            if (player.IsComplete && !player.IsPlaying)
            {
                player.PreviousTime = player.Time;
            }
        }
    }
}
