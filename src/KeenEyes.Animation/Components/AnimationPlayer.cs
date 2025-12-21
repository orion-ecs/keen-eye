using KeenEyes.Animation.Data;

namespace KeenEyes.Animation.Components;

/// <summary>
/// Component for simple animation clip playback on an entity.
/// </summary>
/// <remarks>
/// <para>
/// AnimationPlayer provides basic playback control for a single animation clip.
/// For state machine-based animation with multiple clips and transitions,
/// use <see cref="Animator"/> instead.
/// </para>
/// <para>
/// The clip reference is stored as an ID that maps to a clip in the
/// <see cref="AnimationManager"/>. This keeps components as pure data.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var entity = world.Spawn()
///     .With(Transform3D.Identity)
///     .With(new AnimationPlayer { ClipId = walkClipId, IsPlaying = true })
///     .Build();
/// </code>
/// </example>
[Component]
public partial struct AnimationPlayer
{
    /// <summary>
    /// The ID of the animation clip to play.
    /// </summary>
    public int ClipId;

    /// <summary>
    /// The current playback time in seconds.
    /// </summary>
    [BuilderIgnore]
    public float Time;

    /// <summary>
    /// The previous frame's playback time (for event detection).
    /// </summary>
    [BuilderIgnore]
    public float PreviousTime;

    /// <summary>
    /// The playback speed multiplier (1 = normal, 2 = double speed, -1 = reverse).
    /// </summary>
    public float Speed;

    /// <summary>
    /// Whether the animation is currently playing.
    /// </summary>
    public bool IsPlaying;

    /// <summary>
    /// The wrap mode override, or null to use the clip's default.
    /// </summary>
    public WrapMode? WrapModeOverride;

    /// <summary>
    /// The blend weight for this animation (0-1).
    /// </summary>
    public float Weight;

    /// <summary>
    /// Whether the animation has completed (for non-looping animations).
    /// </summary>
    [BuilderIgnore]
    public bool IsComplete;

    /// <summary>
    /// Creates a default animation player.
    /// </summary>
    public static AnimationPlayer Default => new()
    {
        ClipId = -1,
        Time = 0f,
        PreviousTime = 0f,
        Speed = 1f,
        IsPlaying = false,
        WrapModeOverride = null,
        Weight = 1f,
        IsComplete = false
    };

    /// <summary>
    /// Creates an animation player for the specified clip.
    /// </summary>
    /// <param name="clipId">The clip ID.</param>
    /// <param name="autoPlay">Whether to start playing immediately.</param>
    /// <returns>A configured animation player.</returns>
    public static AnimationPlayer ForClip(int clipId, bool autoPlay = true) => Default with
    {
        ClipId = clipId,
        IsPlaying = autoPlay
    };
}
