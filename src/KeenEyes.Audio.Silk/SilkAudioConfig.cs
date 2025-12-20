namespace KeenEyes.Audio.Silk;

/// <summary>
/// Configuration options for the Silk.NET audio plugin.
/// </summary>
public sealed class SilkAudioConfig
{
    /// <summary>
    /// Gets or sets the maximum number of simultaneous one-shot sounds.
    /// </summary>
    /// <remarks>
    /// This determines the source pool size. Increase for games with many
    /// simultaneous sound effects. Default is 32.
    /// </remarks>
    public int MaxOneShotSources { get; set; } = 32;

    /// <summary>
    /// Gets or sets the initial master volume (0.0 to 1.0).
    /// </summary>
    public float InitialMasterVolume { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets the audio device name to use, or null for default device.
    /// </summary>
    public string? DeviceName { get; set; }
}
