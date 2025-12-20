namespace KeenEyes.Audio.Abstractions;

/// <summary>
/// Audio channel categories for mixer control and volume grouping.
/// </summary>
/// <remarks>
/// <para>
/// Each channel can have its own volume level, allowing users to independently
/// control different types of audio (e.g., mute music while keeping sound effects).
/// </para>
/// </remarks>
public enum AudioChannel
{
    /// <summary>
    /// Master channel affecting all audio output.
    /// </summary>
    Master,

    /// <summary>
    /// Background music and soundtrack.
    /// </summary>
    Music,

    /// <summary>
    /// Sound effects (explosions, footsteps, UI sounds).
    /// </summary>
    SFX,

    /// <summary>
    /// Voice and dialogue audio.
    /// </summary>
    Voice,

    /// <summary>
    /// Ambient and environmental sounds (wind, rain, crowd noise).
    /// </summary>
    Ambient
}
