using System.Numerics;

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

    /// <summary>
    /// Gets the internal backend buffer ID for a clip handle.
    /// </summary>
    /// <param name="handle">The clip handle.</param>
    /// <returns>The backend buffer ID, or 0 if the handle is invalid.</returns>
    /// <remarks>
    /// This is an advanced method for systems that manage their own audio sources
    /// directly through <see cref="IAudioDevice"/>. Most applications should use
    /// the <see cref="Play(AudioClipHandle, float)"/> methods instead.
    /// </remarks>
    uint GetBufferId(AudioClipHandle handle);

    #endregion

    #region Playback

    /// <summary>
    /// Plays an audio clip once (fire and forget).
    /// </summary>
    /// <param name="clip">The clip to play.</param>
    /// <param name="volume">The playback volume (0.0 to 1.0). Defaults to 1.0.</param>
    /// <returns>A handle to the playing sound for optional control.</returns>
    /// <remarks>
    /// Uses an internal source pool for efficient one-shot playback.
    /// The source is automatically recycled when playback completes.
    /// </remarks>
    SoundHandle Play(AudioClipHandle clip, float volume = 1f);

    /// <summary>
    /// Plays an audio clip with custom playback options.
    /// </summary>
    /// <param name="clip">The clip to play.</param>
    /// <param name="options">Playback options (volume, pitch, loop, channel).</param>
    /// <returns>A handle to the playing sound for optional control.</returns>
    SoundHandle Play(AudioClipHandle clip, PlaybackOptions options);

    /// <summary>
    /// Plays an audio clip at a 3D position.
    /// </summary>
    /// <param name="clip">The clip to play.</param>
    /// <param name="position">The world-space position of the sound.</param>
    /// <param name="volume">The playback volume (0.0 to 1.0). Defaults to 1.0.</param>
    /// <returns>A handle to the playing sound for optional control.</returns>
    SoundHandle PlayAt(AudioClipHandle clip, Vector3 position, float volume = 1f);

    /// <summary>
    /// Plays an audio clip at a 3D position with custom options.
    /// </summary>
    /// <param name="clip">The clip to play.</param>
    /// <param name="position">The world-space position of the sound.</param>
    /// <param name="options">Playback options (volume, pitch, loop, channel).</param>
    /// <returns>A handle to the playing sound for optional control.</returns>
    SoundHandle PlayAt(AudioClipHandle clip, Vector3 position, PlaybackOptions options);

    /// <summary>
    /// Stops a specific playing sound.
    /// </summary>
    /// <param name="sound">The sound handle to stop.</param>
    void Stop(SoundHandle sound);

    /// <summary>
    /// Pauses a specific playing sound.
    /// </summary>
    /// <param name="sound">The sound handle to pause.</param>
    void Pause(SoundHandle sound);

    /// <summary>
    /// Resumes a paused sound.
    /// </summary>
    /// <param name="sound">The sound handle to resume.</param>
    void Resume(SoundHandle sound);

    /// <summary>
    /// Sets the volume of a playing sound.
    /// </summary>
    /// <param name="sound">The sound handle.</param>
    /// <param name="volume">The new volume (0.0 to 1.0).</param>
    void SetVolume(SoundHandle sound, float volume);

    /// <summary>
    /// Sets the pitch of a playing sound.
    /// </summary>
    /// <param name="sound">The sound handle.</param>
    /// <param name="pitch">The new pitch multiplier.</param>
    void SetPitch(SoundHandle sound, float pitch);

    /// <summary>
    /// Sets the 3D position of a playing sound.
    /// </summary>
    /// <param name="sound">The sound handle.</param>
    /// <param name="position">The new world-space position.</param>
    void SetPosition(SoundHandle sound, Vector3 position);

    /// <summary>
    /// Checks whether a sound is currently playing.
    /// </summary>
    /// <param name="sound">The sound handle.</param>
    /// <returns>True if the sound is playing, false otherwise.</returns>
    bool IsPlaying(SoundHandle sound);

    /// <summary>
    /// Stops all currently playing sounds.
    /// </summary>
    void StopAll();

    /// <summary>
    /// Pauses all currently playing sounds.
    /// </summary>
    void PauseAll();

    /// <summary>
    /// Resumes all paused sounds.
    /// </summary>
    void ResumeAll();

    #endregion

    #region Channel Volume

    /// <summary>
    /// Gets the volume for a specific audio channel.
    /// </summary>
    /// <param name="channel">The audio channel.</param>
    /// <returns>The channel volume (0.0 to 1.0).</returns>
    float GetChannelVolume(AudioChannel channel);

    /// <summary>
    /// Sets the volume for a specific audio channel.
    /// </summary>
    /// <param name="channel">The audio channel.</param>
    /// <param name="volume">The new volume (0.0 to 1.0).</param>
    /// <remarks>
    /// Channel volume is multiplied with individual sound volumes.
    /// Setting a channel to 0 mutes all sounds on that channel.
    /// </remarks>
    void SetChannelVolume(AudioChannel channel, float volume);

    #endregion

    #region Listener

    /// <summary>
    /// Sets the listener position in 3D space.
    /// </summary>
    /// <param name="position">The world-space position of the listener.</param>
    /// <remarks>
    /// This is a convenience method that delegates to the underlying device.
    /// For ECS-based audio, use the <c>AudioListener</c> component instead.
    /// </remarks>
    void SetListenerPosition(Vector3 position);

    /// <summary>
    /// Sets the listener orientation in 3D space.
    /// </summary>
    /// <param name="forward">The forward direction vector (normalized).</param>
    /// <param name="up">The up direction vector (normalized).</param>
    /// <remarks>
    /// This is a convenience method that delegates to the underlying device.
    /// For ECS-based audio, use the <c>AudioListener</c> component instead.
    /// </remarks>
    void SetListenerOrientation(Vector3 forward, Vector3 up);

    /// <summary>
    /// Sets the listener velocity for Doppler calculations.
    /// </summary>
    /// <param name="velocity">The velocity vector (units per second).</param>
    /// <remarks>
    /// This is a convenience method that delegates to the underlying device.
    /// For ECS-based audio, use the <c>AudioListener</c> component instead.
    /// </remarks>
    void SetListenerVelocity(Vector3 velocity);

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
