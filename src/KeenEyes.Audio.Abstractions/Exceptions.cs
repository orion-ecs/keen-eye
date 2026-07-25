namespace KeenEyes.Audio.Abstractions;

/// <summary>
/// Base exception for audio-related errors.
/// </summary>
public class AudioException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AudioException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public AudioException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public AudioException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Thrown when audio device initialization fails.
/// </summary>
/// <remarks>
/// Typical causes are a missing native OpenAL runtime and a machine with no usable
/// audio output device. Audio is optional hardware, so callers should treat this as a
/// recoverable loss of capability rather than a fatal error.
/// </remarks>
public class AudioInitializationException : AudioException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AudioInitializationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public AudioInitializationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioInitializationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The backend exception that caused the failure.</param>
    public AudioInitializationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Thrown when loading an audio file fails.
/// </summary>
public class AudioLoadException : AudioException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AudioLoadException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public AudioLoadException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioLoadException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public AudioLoadException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
