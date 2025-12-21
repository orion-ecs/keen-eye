using KeenEyes.Audio.Abstractions;

namespace KeenEyes.Assets;

/// <summary>
/// A loaded audio clip asset containing the audio handle and metadata.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="AudioClipAsset"/> wraps an <see cref="AudioClipHandle"/> from the audio
/// context along with metadata like duration and sample rate. It is created by
/// <see cref="AudioClipLoader"/> and managed by <see cref="AssetManager"/>.
/// </para>
/// <para>
/// Disposing an AudioClipAsset releases the underlying audio buffer. However,
/// when using through <see cref="AssetHandle{T}"/>, the asset manager handles
/// disposal based on reference counting.
/// </para>
/// </remarks>
public sealed class AudioClipAsset : IDisposable
{
    private readonly IAudioContext? audio;
    private bool disposed;

    /// <summary>
    /// Gets the underlying audio clip handle.
    /// </summary>
    public AudioClipHandle Handle { get; }

    /// <summary>
    /// Gets the duration of the audio clip.
    /// </summary>
    public TimeSpan Duration { get; }

    /// <summary>
    /// Gets the number of audio channels (1 = mono, 2 = stereo).
    /// </summary>
    public int Channels { get; }

    /// <summary>
    /// Gets the sample rate in Hz.
    /// </summary>
    public int SampleRate { get; }

    /// <summary>
    /// Gets the bit depth of the audio data.
    /// </summary>
    public int BitsPerSample { get; }

    /// <summary>
    /// Gets the estimated size of the audio data in bytes.
    /// </summary>
    public long SizeBytes => (long)(Duration.TotalSeconds * SampleRate * Channels * (BitsPerSample / 8));

    /// <summary>
    /// Creates a new audio clip asset.
    /// </summary>
    /// <param name="handle">The audio clip handle.</param>
    /// <param name="duration">Duration of the clip.</param>
    /// <param name="channels">Number of channels.</param>
    /// <param name="sampleRate">Sample rate in Hz.</param>
    /// <param name="bitsPerSample">Bits per sample.</param>
    /// <param name="audio">Audio context for resource cleanup.</param>
    internal AudioClipAsset(
        AudioClipHandle handle,
        TimeSpan duration,
        int channels,
        int sampleRate,
        int bitsPerSample,
        IAudioContext? audio)
    {
        Handle = handle;
        Duration = duration;
        Channels = channels;
        SampleRate = sampleRate;
        BitsPerSample = bitsPerSample;
        this.audio = audio;
    }

    /// <summary>
    /// Releases the audio buffer resource.
    /// </summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        if (Handle.IsValid && audio != null)
        {
            audio.UnloadClip(Handle);
        }
    }
}
