namespace KeenEyes.Audio;

/// <summary>
/// ECS component marking an entity as the audio listener (the "ear" for 3D spatial audio).
/// </summary>
/// <remarks>
/// <para>
/// Only one listener should be active at a time. The audio system uses the entity's
/// <c>Transform3D</c> component to position the listener in 3D space.
/// </para>
/// <para>
/// Typically attached to the player character or camera entity. The listener's
/// position and orientation determine how 3D sounds are spatialized.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Attach listener to player/camera
/// var player = world.Spawn()
///     .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
///     .With(AudioListener.Active)
///     .Build();
/// </code>
/// </example>
public struct AudioListener : IComponent
{
    /// <summary>
    /// Whether this listener is active.
    /// </summary>
    /// <remarks>
    /// Only one listener should be active at a time. If multiple listeners
    /// are active, the first one found is used.
    /// </remarks>
    public bool IsActive;

    /// <summary>
    /// Doppler effect factor.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>0 = Doppler effect disabled</item>
    /// <item>1 = Normal Doppler effect</item>
    /// <item>2+ = Exaggerated Doppler effect</item>
    /// </list>
    /// </remarks>
    public float DopplerFactor;

    /// <summary>
    /// Volume multiplier applied to all audio.
    /// </summary>
    public float VolumeMultiplier;

    /// <summary>
    /// Speed of sound in units per second (for Doppler calculations).
    /// </summary>
    /// <remarks>
    /// The default value of 343 represents the speed of sound in air at 20°C
    /// in meters per second. Adjust this to match your game's unit system.
    /// For example, if your game uses 1 unit = 1 centimeter, use 34300.
    /// </remarks>
    public float SpeedOfSound;

    /// <summary>
    /// Creates an active listener with default settings.
    /// </summary>
    /// <remarks>
    /// Default values:
    /// <list type="bullet">
    /// <item>IsActive: true</item>
    /// <item>DopplerFactor: 1.0</item>
    /// <item>VolumeMultiplier: 1.0</item>
    /// <item>SpeedOfSound: 343.0 (m/s at 20°C)</item>
    /// </list>
    /// </remarks>
    public static AudioListener Active => new()
    {
        IsActive = true,
        DopplerFactor = 1f,
        VolumeMultiplier = 1f,
        SpeedOfSound = 343f
    };

    /// <summary>
    /// Creates an inactive listener with default settings.
    /// </summary>
    public static AudioListener Inactive => new()
    {
        IsActive = false,
        DopplerFactor = 1f,
        VolumeMultiplier = 1f,
        SpeedOfSound = 343f
    };
}
