using System.Numerics;

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

    // === 3D Source Properties ===

    /// <summary>
    /// Sets the 3D position of an audio source.
    /// </summary>
    /// <param name="sourceId">The source.</param>
    /// <param name="position">The position in world space.</param>
    void SetSourcePosition(uint sourceId, Vector3 position);

    /// <summary>
    /// Sets the velocity of an audio source for Doppler effect calculations.
    /// </summary>
    /// <param name="sourceId">The source.</param>
    /// <param name="velocity">The velocity vector.</param>
    void SetSourceVelocity(uint sourceId, Vector3 velocity);

    /// <summary>
    /// Sets the pitch multiplier of an audio source.
    /// </summary>
    /// <param name="sourceId">The source.</param>
    /// <param name="pitch">The pitch multiplier (1.0 = normal, 0.5 = octave down, 2.0 = octave up).</param>
    void SetSourcePitch(uint sourceId, float pitch);

    /// <summary>
    /// Sets whether a source loops.
    /// </summary>
    /// <param name="sourceId">The source.</param>
    /// <param name="loop">True to loop, false to play once.</param>
    void SetSourceLooping(uint sourceId, bool loop);

    /// <summary>
    /// Sets the minimum distance for distance attenuation.
    /// </summary>
    /// <remarks>
    /// Within this distance, the source plays at full volume.
    /// </remarks>
    /// <param name="sourceId">The source.</param>
    /// <param name="distance">The minimum distance.</param>
    void SetSourceMinDistance(uint sourceId, float distance);

    /// <summary>
    /// Sets the maximum distance for distance attenuation.
    /// </summary>
    /// <remarks>
    /// Beyond this distance, the source is inaudible (or clamped to minimum).
    /// </remarks>
    /// <param name="sourceId">The source.</param>
    /// <param name="distance">The maximum distance.</param>
    void SetSourceMaxDistance(uint sourceId, float distance);

    /// <summary>
    /// Sets the rolloff factor for distance attenuation.
    /// </summary>
    /// <param name="sourceId">The source.</param>
    /// <param name="rolloff">The rolloff factor (higher = faster falloff).</param>
    void SetSourceRolloff(uint sourceId, float rolloff);

    /// <summary>
    /// Pauses a playing source.
    /// </summary>
    /// <param name="sourceId">The source to pause.</param>
    void PauseSource(uint sourceId);

    // === 3D Listener Properties ===

    /// <summary>
    /// Sets the listener position in 3D space.
    /// </summary>
    /// <param name="position">The position in world space.</param>
    void SetListenerPosition(Vector3 position);

    /// <summary>
    /// Sets the listener velocity for Doppler effect calculations.
    /// </summary>
    /// <param name="velocity">The velocity vector.</param>
    void SetListenerVelocity(Vector3 velocity);

    /// <summary>
    /// Sets the listener orientation in 3D space.
    /// </summary>
    /// <param name="forward">The forward direction vector (normalized).</param>
    /// <param name="up">The up direction vector (normalized).</param>
    void SetListenerOrientation(Vector3 forward, Vector3 up);

    // === Global Settings ===

    /// <summary>
    /// Sets the global distance attenuation model.
    /// </summary>
    /// <param name="mode">The rolloff mode to use.</param>
    void SetDistanceModel(AudioRolloffMode mode);

    /// <summary>
    /// Sets the speed of sound for Doppler effect calculations.
    /// </summary>
    /// <param name="speed">The speed of sound in units per second (default: 343 m/s).</param>
    void SetSpeedOfSound(float speed);

    /// <summary>
    /// Sets the global Doppler effect factor.
    /// </summary>
    /// <param name="factor">The Doppler factor (0 = disabled, 1 = normal, 2+ = exaggerated).</param>
    void SetDopplerFactor(float factor);
}
