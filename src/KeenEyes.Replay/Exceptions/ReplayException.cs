namespace KeenEyes.Replay;

/// <summary>
/// Base exception for all replay-related errors.
/// </summary>
/// <remarks>
/// <para>
/// This exception serves as the base class for all replay system exceptions,
/// enabling catch blocks to handle all replay errors uniformly when specific
/// handling is not required.
/// </para>
/// <para>
/// Derived exceptions include:
/// <list type="bullet">
/// <item><description><see cref="ReplayFormatException"/> - Invalid replay file format.</description></item>
/// <item><description><see cref="ReplayVersionException"/> - Replay format version mismatch.</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// try
/// {
///     player.LoadReplay("recording.kreplay");
/// }
/// catch (ReplayException ex)
/// {
///     Console.WriteLine($"Failed to load replay: {ex.Message}");
/// }
/// </code>
/// </example>
public class ReplayException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayException"/> class.
    /// </summary>
    public ReplayException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayException"/> class
    /// with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ReplayException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayException"/> class
    /// with a specified error message and a reference to the inner exception
    /// that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">
    /// The exception that is the cause of the current exception, or null if no inner exception is specified.
    /// </param>
    public ReplayException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
