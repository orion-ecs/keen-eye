namespace KeenEyes.Audio.Abstractions;

/// <summary>
/// High-level audio API for sound playback operations.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides the primary application-level API for audio operations
/// in KeenEyes. It abstracts resource management (audio clips) and provides
/// playback control without exposing low-level audio backend details.
/// </para>
/// <para>
/// Unlike <see cref="IAudioDevice"/>, which exposes raw audio operations,
/// <see cref="IAudioContext"/> uses opaque handles (<see cref="AudioClipHandle"/>)
/// to reference resources.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Load an audio clip
/// var clip = audio.LoadClip("assets/sounds/explosion.wav");
///
/// // Play the clip (fire and forget)
/// audio.Play(clip);
///
/// // Adjust master volume
/// audio.MasterVolume = 0.5f;
/// </code>
/// </example>
public interface IAudioContext : IDisposable
{
    #region State

    /// <summary>
    /// Gets the low-level audio device.
    /// </summary>
    IAudioDevice? Device { get; }

    /// <summary>
    /// Gets whether the audio context is initialized and ready for use.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Gets or sets the master volume (0.0 to 1.0).
    /// </summary>
    /// <remarks>
    /// This affects all audio playback. Values above 1.0 may cause clipping.
    /// </remarks>
    float MasterVolume { get; set; }

    #endregion

    #region Clip Operations

    /// <summary>
    /// Loads an audio clip from a file.
    /// </summary>
    /// <param name="path">The path to the audio file (WAV format in Phase 1).</param>
    /// <returns>The audio clip handle.</returns>
    /// <exception cref="AudioLoadException">Thrown when the file cannot be loaded.</exception>
    AudioClipHandle LoadClip(string path);

    /// <summary>
    /// Loads an audio clip from raw audio data.
    /// </summary>
    /// <param name="data">The raw audio sample data.</param>
    /// <param name="format">The audio format.</param>
    /// <param name="sampleRate">The sample rate in Hz.</param>
    /// <returns>The audio clip handle.</returns>
    AudioClipHandle CreateClip(ReadOnlySpan<byte> data, AudioFormat format, int sampleRate);

    /// <summary>
    /// Gets information about a loaded audio clip.
    /// </summary>
    /// <param name="handle">The clip handle.</param>
    /// <returns>The clip metadata, or null if handle is invalid.</returns>
    AudioClipInfo? GetClipInfo(AudioClipHandle handle);

    /// <summary>
    /// Unloads an audio clip and frees its resources.
    /// </summary>
    /// <param name="handle">The clip handle.</param>
    void UnloadClip(AudioClipHandle handle);

    #endregion

    #region Playback

    /// <summary>
    /// Plays an audio clip once (fire and forget).
    /// </summary>
    /// <param name="handle">The clip to play.</param>
    /// <param name="volume">The playback volume (0.0 to 1.0). Defaults to 1.0.</param>
    /// <remarks>
    /// Uses an internal source pool for efficient one-shot playback.
    /// The source is automatically recycled when playback completes.
    /// </remarks>
    void Play(AudioClipHandle handle, float volume = 1f);

    /// <summary>
    /// Stops all currently playing sounds.
    /// </summary>
    void StopAll();

    #endregion

    #region Lifecycle

    /// <summary>
    /// Updates the audio context (recycles finished sources, etc.).
    /// </summary>
    /// <remarks>
    /// Call this once per frame to maintain the source pool.
    /// </remarks>
    void Update();

    #endregion
}
