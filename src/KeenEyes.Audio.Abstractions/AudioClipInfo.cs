namespace KeenEyes.Audio.Abstractions;

/// <summary>
/// Metadata about a loaded audio clip.
/// </summary>
/// <param name="Handle">The handle to the audio clip.</param>
/// <param name="Format">The audio sample format.</param>
/// <param name="SampleRate">The sample rate in Hz (e.g., 44100).</param>
/// <param name="Channels">The number of channels (1 for mono, 2 for stereo).</param>
/// <param name="BitsPerSample">The bits per sample (8 or 16).</param>
/// <param name="Duration">The duration of the clip in seconds.</param>
public readonly record struct AudioClipInfo(
    AudioClipHandle Handle,
    AudioFormat Format,
    int SampleRate,
    int Channels,
    int BitsPerSample,
    float Duration);
