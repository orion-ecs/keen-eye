using KeenEyes.Audio.Abstractions;

namespace KeenEyes.Audio.Silk.Backend;

/// <summary>
/// Represents audio data stored in an OpenAL buffer.
/// </summary>
internal sealed class AudioBuffer
{
    /// <summary>
    /// Gets the OpenAL buffer identifier.
    /// </summary>
    public required uint BufferId { get; init; }

    /// <summary>
    /// Gets the audio sample format.
    /// </summary>
    public required AudioFormat Format { get; init; }

    /// <summary>
    /// Gets the sample rate in Hz.
    /// </summary>
    public required int SampleRate { get; init; }

    /// <summary>
    /// Gets the number of channels.
    /// </summary>
    public required int Channels { get; init; }

    /// <summary>
    /// Gets the bits per sample.
    /// </summary>
    public required int BitsPerSample { get; init; }

    /// <summary>
    /// Gets the duration in seconds.
    /// </summary>
    public required float Duration { get; init; }
}
