namespace KeenEyes.Audio.Abstractions;

/// <summary>
/// Audio sample format for audio data.
/// </summary>
public enum AudioFormat
{
    /// <summary>
    /// 8-bit mono audio (1 channel, 8 bits per sample).
    /// </summary>
    Mono8,

    /// <summary>
    /// 16-bit mono audio (1 channel, 16 bits per sample).
    /// </summary>
    Mono16,

    /// <summary>
    /// 8-bit stereo audio (2 channels, 8 bits per sample).
    /// </summary>
    Stereo8,

    /// <summary>
    /// 16-bit stereo audio (2 channels, 16 bits per sample).
    /// </summary>
    Stereo16
}

/// <summary>
/// Playback state of an audio source.
/// </summary>
public enum AudioPlayState
{
    /// <summary>
    /// The source is stopped and at the beginning.
    /// </summary>
    Stopped,

    /// <summary>
    /// The source is currently playing.
    /// </summary>
    Playing,

    /// <summary>
    /// The source is paused at its current position.
    /// </summary>
    Paused
}

/// <summary>
/// Distance attenuation curve for 3D spatial audio.
/// </summary>
/// <remarks>
/// <para>
/// Controls how audio volume decreases as the listener moves away from the source.
/// Different modes are suitable for different types of audio sources.
/// </para>
/// </remarks>
public enum AudioRolloffMode
{
    /// <summary>
    /// Linear falloff: Volume decreases linearly from MinDistance to MaxDistance.
    /// </summary>
    /// <remarks>
    /// Volume = 1 - (distance - minDist) / (maxDist - minDist)
    /// </remarks>
    Linear,

    /// <summary>
    /// Logarithmic (inverse) falloff: Realistic sound propagation.
    /// </summary>
    /// <remarks>
    /// Volume = minDist / (minDist + rolloff * (distance - minDist))
    /// </remarks>
    Logarithmic,

    /// <summary>
    /// Exponential falloff: Faster drop-off than logarithmic.
    /// </summary>
    /// <remarks>
    /// Volume = pow(distance / minDist, -rolloff)
    /// </remarks>
    Exponential,

    /// <summary>
    /// User-defined custom attenuation curve.
    /// </summary>
    /// <remarks>
    /// When using custom rolloff, the application must compute and set
    /// the source gain manually based on distance. The audio backend
    /// will not apply automatic distance attenuation.
    /// </remarks>
    Custom
}
