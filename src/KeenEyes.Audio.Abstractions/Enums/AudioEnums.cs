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
