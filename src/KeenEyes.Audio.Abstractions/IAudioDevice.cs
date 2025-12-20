namespace KeenEyes.Audio.Abstractions;

/// <summary>
/// Low-level audio device interface for backend-specific operations.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides direct access to audio backend operations.
/// Most applications should use <see cref="IAudioContext"/> instead for
/// high-level audio operations.
/// </para>
/// </remarks>
public interface IAudioDevice : IDisposable
{
    /// <summary>
    /// Gets whether the audio device is initialized and ready.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Gets the name of the audio device.
    /// </summary>
    string DeviceName { get; }

    /// <summary>
    /// Creates an audio buffer for storing audio data.
    /// </summary>
    /// <returns>The buffer identifier.</returns>
    uint CreateBuffer();

    /// <summary>
    /// Deletes an audio buffer.
    /// </summary>
    /// <param name="bufferId">The buffer to delete.</param>
    void DeleteBuffer(uint bufferId);

    /// <summary>
    /// Uploads audio data to a buffer.
    /// </summary>
    /// <param name="bufferId">The target buffer.</param>
    /// <param name="format">The audio format.</param>
    /// <param name="data">The audio sample data.</param>
    /// <param name="sampleRate">The sample rate in Hz.</param>
    void BufferData(uint bufferId, AudioFormat format, ReadOnlySpan<byte> data, int sampleRate);

    /// <summary>
    /// Creates an audio source for playback.
    /// </summary>
    /// <returns>The source identifier.</returns>
    uint CreateSource();

    /// <summary>
    /// Deletes an audio source.
    /// </summary>
    /// <param name="sourceId">The source to delete.</param>
    void DeleteSource(uint sourceId);

    /// <summary>
    /// Attaches a buffer to a source.
    /// </summary>
    /// <param name="sourceId">The source.</param>
    /// <param name="bufferId">The buffer to attach.</param>
    void SetSourceBuffer(uint sourceId, uint bufferId);

    /// <summary>
    /// Sets the gain (volume) of a source.
    /// </summary>
    /// <param name="sourceId">The source.</param>
    /// <param name="gain">The gain value (0.0 to 1.0+).</param>
    void SetSourceGain(uint sourceId, float gain);

    /// <summary>
    /// Plays a source.
    /// </summary>
    /// <param name="sourceId">The source to play.</param>
    void PlaySource(uint sourceId);

    /// <summary>
    /// Stops a source.
    /// </summary>
    /// <param name="sourceId">The source to stop.</param>
    void StopSource(uint sourceId);

    /// <summary>
    /// Gets the current playback state of a source.
    /// </summary>
    /// <param name="sourceId">The source.</param>
    /// <returns>The playback state.</returns>
    AudioPlayState GetSourceState(uint sourceId);

    /// <summary>
    /// Sets the listener (master) gain.
    /// </summary>
    /// <param name="gain">The gain value (0.0 to 1.0+).</param>
    void SetListenerGain(float gain);
}
