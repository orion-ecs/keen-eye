namespace KeenEyes.Animation.Data;

/// <summary>
/// Defines how an animation clip behaves when it reaches its end.
/// </summary>
public enum WrapMode
{
    /// <summary>
    /// The animation stops at the end.
    /// </summary>
    Once,

    /// <summary>
    /// The animation loops from the beginning.
    /// </summary>
    Loop,

    /// <summary>
    /// The animation plays forward then backward repeatedly.
    /// </summary>
    PingPong,

    /// <summary>
    /// The animation holds its last frame.
    /// </summary>
    ClampForever
}

/// <summary>
/// A reusable animation clip asset containing keyframe data for skeletal animation.
/// </summary>
/// <remarks>
/// <para>
/// Animation clips are shared asset data, not components. Multiple entities can
/// reference the same clip. Animation state (current time, playing status) lives
/// in components like <see cref="Components.AnimationPlayer"/>.
/// </para>
/// <para>
/// This follows the ECS principle: clips are assets, state is components, logic is systems.
/// </para>
/// </remarks>
public sealed class AnimationClip
{
    private readonly Dictionary<string, BoneTrack> boneTracks = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets the name of this animation clip.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the duration of this clip in seconds.
    /// </summary>
    public float Duration { get; set; }

    /// <summary>
    /// Gets or sets the default wrap mode for this clip.
    /// </summary>
    public WrapMode WrapMode { get; set; } = WrapMode.Once;

    /// <summary>
    /// Gets or sets the playback speed multiplier.
    /// </summary>
    public float Speed { get; set; } = 1f;

    /// <summary>
    /// Gets the bone tracks in this clip.
    /// </summary>
    public IReadOnlyDictionary<string, BoneTrack> BoneTracks => boneTracks;

    /// <summary>
    /// Gets the animation event track for this clip.
    /// </summary>
    public AnimationEventTrack Events { get; } = new();

    /// <summary>
    /// Adds a bone track to this clip.
    /// </summary>
    /// <param name="track">The bone track to add.</param>
    public void AddBoneTrack(BoneTrack track)
    {
        boneTracks[track.BoneName] = track;

        // Update duration to encompass all tracks
        if (track.Duration > Duration)
        {
            Duration = track.Duration;
        }
    }

    /// <summary>
    /// Gets a bone track by name.
    /// </summary>
    /// <param name="boneName">The name of the bone.</param>
    /// <param name="track">The bone track, if found.</param>
    /// <returns>True if the track was found.</returns>
    public bool TryGetBoneTrack(string boneName, out BoneTrack? track)
    {
        return boneTracks.TryGetValue(boneName, out track);
    }

    /// <summary>
    /// Wraps a time value according to the clip's wrap mode.
    /// </summary>
    /// <param name="time">The current time.</param>
    /// <returns>The wrapped time value.</returns>
    public float WrapTime(float time)
    {
        if (Duration <= 0f)
        {
            return 0f;
        }

        return WrapMode switch
        {
            WrapMode.Once => Math.Clamp(time, 0f, Duration),
            WrapMode.Loop => time % Duration,
            WrapMode.PingPong => WrapPingPong(time),
            WrapMode.ClampForever => Math.Max(time, 0f),
            _ => time
        };
    }

    private float WrapPingPong(float time)
    {
        var cycles = (int)(time / Duration);
        var t = time % Duration;

        // Odd cycles play in reverse
        return (cycles % 2 == 1) ? Duration - t : t;
    }
}
