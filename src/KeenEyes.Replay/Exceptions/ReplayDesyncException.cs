namespace KeenEyes.Replay;

/// <summary>
/// Exception thrown when replay playback diverges from the recorded session.
/// </summary>
/// <remarks>
/// <para>
/// A desync occurs when the world state during playback differs from the
/// recorded state at the same frame. This typically indicates non-deterministic
/// behavior in the game logic, such as:
/// <list type="bullet">
/// <item><description>Use of unsynchronized random number generation</description></item>
/// <item><description>Reliance on system time or external state</description></item>
/// <item><description>Race conditions in multi-threaded code</description></item>
/// <item><description>Floating-point precision differences</description></item>
/// </list>
/// </para>
/// <para>
/// When a desync is detected, the <see cref="Frame"/>, <see cref="ExpectedChecksum"/>,
/// and <see cref="ActualChecksum"/> properties provide information for debugging.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// try
/// {
///     player.ValidateCurrentFrame();
/// }
/// catch (ReplayDesyncException ex)
/// {
///     Console.WriteLine($"Desync at frame {ex.Frame}");
///     Console.WriteLine($"Expected: 0x{ex.ExpectedChecksum:X8}");
///     Console.WriteLine($"Actual: 0x{ex.ActualChecksum:X8}");
/// }
/// </code>
/// </example>
public class ReplayDesyncException : ReplayException
{
    /// <summary>
    /// Gets the frame number where the desync was detected.
    /// </summary>
    /// <remarks>
    /// This is the 0-based frame index in the replay where the checksum mismatch occurred.
    /// </remarks>
    public int Frame { get; }

    /// <summary>
    /// Gets the expected checksum from the recorded replay.
    /// </summary>
    /// <remarks>
    /// This value was calculated during recording and represents the expected world state.
    /// </remarks>
    public uint ExpectedChecksum { get; }

    /// <summary>
    /// Gets the actual checksum calculated during playback.
    /// </summary>
    /// <remarks>
    /// This value was calculated from the current world state during playback.
    /// A mismatch with <see cref="ExpectedChecksum"/> indicates a desync.
    /// </remarks>
    public uint ActualChecksum { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayDesyncException"/> class.
    /// </summary>
    public ReplayDesyncException()
        : base("Replay desync detected.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayDesyncException"/> class
    /// with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ReplayDesyncException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayDesyncException"/> class
    /// with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of this exception.</param>
    public ReplayDesyncException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayDesyncException"/> class
    /// with frame and checksum details.
    /// </summary>
    /// <param name="frame">The frame number where the desync was detected.</param>
    /// <param name="expectedChecksum">The expected checksum from the recorded replay.</param>
    /// <param name="actualChecksum">The actual checksum calculated during playback.</param>
    public ReplayDesyncException(int frame, uint expectedChecksum, uint actualChecksum)
        : base(FormatMessage(frame, expectedChecksum, actualChecksum))
    {
        Frame = frame;
        ExpectedChecksum = expectedChecksum;
        ActualChecksum = actualChecksum;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayDesyncException"/> class
    /// with a custom message and frame/checksum details.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="frame">The frame number where the desync was detected.</param>
    /// <param name="expectedChecksum">The expected checksum from the recorded replay.</param>
    /// <param name="actualChecksum">The actual checksum calculated during playback.</param>
    public ReplayDesyncException(string message, int frame, uint expectedChecksum, uint actualChecksum)
        : base(message)
    {
        Frame = frame;
        ExpectedChecksum = expectedChecksum;
        ActualChecksum = actualChecksum;
    }

    /// <summary>
    /// Formats the default error message with frame and checksum details.
    /// </summary>
    private static string FormatMessage(int frame, uint expectedChecksum, uint actualChecksum)
    {
        return $"Replay desync detected at frame {frame}. " +
               $"Expected checksum: 0x{expectedChecksum:X8}, " +
               $"Actual checksum: 0x{actualChecksum:X8}.";
    }
}
