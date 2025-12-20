namespace KeenEyes.Audio.Abstractions;

/// <summary>
/// Options for audio playback, controlling volume, pitch, looping, and mixer channel.
/// </summary>
/// <remarks>
/// <para>
/// Use this to specify playback parameters when calling
/// <c>Play(AudioClipHandle, PlaybackOptions)</c> or
/// <c>PlayAt(AudioClipHandle, Vector3, PlaybackOptions)</c>.
/// </para>
/// <para>
/// This is a value type for performance. Use <see cref="Default"/> for standard playback,
/// or use <c>with</c> expressions to customize specific properties.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Play with default options
/// audio.Play(clip, PlaybackOptions.Default);
///
/// // Play looped music at reduced volume
/// audio.Play(musicClip, new PlaybackOptions
/// {
///     Volume = 0.5f,
///     Loop = true,
///     Channel = AudioChannel.Music
/// });
///
/// // Modify defaults using 'with'
/// audio.Play(clip, PlaybackOptions.Default with { Pitch = 1.5f });
/// </code>
/// </example>
public readonly record struct PlaybackOptions
{
    /// <summary>
    /// Playback volume (0.0 to 1.0, can exceed 1.0 for amplification).
    /// </summary>
    /// <remarks>
    /// Values above 1.0 may cause clipping/distortion.
    /// </remarks>
    public float Volume { get; init; }

    /// <summary>
    /// Playback pitch multiplier.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>0.5 = One octave down</item>
    /// <item>1.0 = Normal pitch</item>
    /// <item>2.0 = One octave up</item>
    /// </list>
    /// </remarks>
    public float Pitch { get; init; }

    /// <summary>
    /// Whether the sound should loop continuously.
    /// </summary>
    public bool Loop { get; init; }

    /// <summary>
    /// The mixer channel for volume grouping.
    /// </summary>
    /// <remarks>
    /// Allows per-category volume control (e.g., mute music but keep sound effects).
    /// </remarks>
    public AudioChannel Channel { get; init; }

    /// <summary>
    /// Default playback options with standard values.
    /// </summary>
    /// <remarks>
    /// Default values:
    /// <list type="bullet">
    /// <item>Volume: 1.0</item>
    /// <item>Pitch: 1.0</item>
    /// <item>Loop: false</item>
    /// <item>Channel: SFX</item>
    /// </list>
    /// </remarks>
    public static PlaybackOptions Default => new()
    {
        Volume = 1f,
        Pitch = 1f,
        Loop = false,
        Channel = AudioChannel.SFX
    };
}
