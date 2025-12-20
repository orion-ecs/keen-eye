namespace KeenEyes.Audio.Abstractions;

/// <summary>
/// Configuration options for the audio system.
/// </summary>
/// <remarks>
/// <para>
/// Use this to configure global audio settings that affect all sounds.
/// These values are typically set once when initializing the audio system.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var config = new AudioConfig
/// {
///     MaxSimultaneousSounds = 64,
///     DefaultMinDistance = 2f,
///     DefaultMaxDistance = 50f
/// };
/// </code>
/// </example>
public sealed class AudioConfig
{
    /// <summary>
    /// Maximum number of sounds that can play simultaneously.
    /// </summary>
    /// <remarks>
    /// Sounds beyond this limit will be silently ignored or oldest sounds
    /// may be stopped to make room for new ones, depending on implementation.
    /// </remarks>
    public int MaxSimultaneousSounds { get; init; } = 32;

    /// <summary>
    /// Default minimum distance for 3D spatial audio sources.
    /// </summary>
    /// <remarks>
    /// Within this distance, sound plays at full volume.
    /// </remarks>
    public float DefaultMinDistance { get; init; } = 1f;

    /// <summary>
    /// Default maximum distance for 3D spatial audio sources.
    /// </summary>
    /// <remarks>
    /// Beyond this distance, sound is inaudible (for linear rolloff)
    /// or attenuated to near-zero (for logarithmic/exponential rolloff).
    /// </remarks>
    public float DefaultMaxDistance { get; init; } = 100f;

    /// <summary>
    /// Default distance attenuation curve for 3D audio.
    /// </summary>
    public AudioRolloffMode DefaultRolloff { get; init; } = AudioRolloffMode.Logarithmic;

    /// <summary>
    /// Doppler effect intensity factor.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>0 = Doppler effect disabled</item>
    /// <item>1 = Normal Doppler effect</item>
    /// <item>2+ = Exaggerated Doppler effect</item>
    /// </list>
    /// </remarks>
    public float DopplerFactor { get; init; } = 1f;

    /// <summary>
    /// Speed of sound in units per second (for Doppler calculations).
    /// </summary>
    /// <remarks>
    /// The default value of 343 represents the speed of sound in air at 20°C
    /// in meters per second. Adjust this to match your game's unit system.
    /// </remarks>
    public float SpeedOfSound { get; init; } = 343f;

    /// <summary>
    /// Default configuration with sensible defaults for most games.
    /// </summary>
    public static AudioConfig Default => new();
}
