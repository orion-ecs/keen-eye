namespace KeenEyes.Animation;

/// <summary>
/// Configuration for the animation plugin.
/// </summary>
public sealed class AnimationConfig
{
    /// <summary>
    /// Gets the default configuration.
    /// </summary>
    public static AnimationConfig Default => new();

    /// <summary>
    /// Gets or sets whether to enable animation event dispatch.
    /// </summary>
    /// <remarks>
    /// Animation events can be fired at specific keyframe times.
    /// Disabling this reduces overhead if events are not used.
    /// </remarks>
    public bool EnableEvents { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of simultaneous active tweens per entity.
    /// </summary>
    /// <remarks>
    /// This limit helps prevent runaway tween creation.
    /// </remarks>
    public int MaxTweensPerEntity { get; set; } = 16;

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    /// <returns>An error message if invalid, or null if valid.</returns>
    public string? Validate()
    {
        if (MaxTweensPerEntity <= 0)
        {
            return "MaxTweensPerEntity must be positive";
        }

        return null;
    }
}
