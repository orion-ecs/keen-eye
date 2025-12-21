using KeenEyes;

namespace KeenEyes.Animation.Events;

/// <summary>
/// Event published when an animation event is triggered during playback.
/// </summary>
/// <param name="Entity">The entity that triggered the event.</param>
/// <param name="EventName">The name of the triggered animation event.</param>
/// <param name="Parameter">Optional parameter string associated with the event.</param>
/// <param name="Time">The time in the animation clip when the event was triggered.</param>
/// <param name="ClipId">The ID of the animation clip containing the event.</param>
/// <remarks>
/// <para>
/// Animation events are defined in <see cref="Data.AnimationClip"/> and fire when playback
/// crosses the event's time threshold. This allows synchronizing game logic with animation
/// playback, such as:
/// </para>
/// <list type="bullet">
///   <item><description>Playing footstep sounds at specific frames</description></item>
///   <item><description>Spawning particles or effects mid-animation</description></item>
///   <item><description>Triggering damage windows in attack animations</description></item>
///   <item><description>Enabling/disabling hitboxes or collision</description></item>
/// </list>
/// <para>
/// Subscribe to these events via the world's event bus:
/// <code>
/// world.Events.Subscribe&lt;AnimationEventTriggeredEvent&gt;(e =>
/// {
///     if (e.EventName == "footstep")
///     {
///         PlayFootstepSound(e.Entity, e.Parameter);
///     }
/// });
/// </code>
/// </para>
/// </remarks>
public readonly record struct AnimationEventTriggeredEvent(
    Entity Entity,
    string EventName,
    string? Parameter,
    float Time,
    int ClipId);
