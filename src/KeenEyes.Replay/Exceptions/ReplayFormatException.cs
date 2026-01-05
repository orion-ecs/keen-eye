namespace KeenEyes.Replay;

/// <summary>
/// Exception thrown when a replay file has an invalid or unrecognized format.
/// </summary>
/// <remarks>
/// <para>
/// This exception is thrown in the following scenarios:
/// <list type="bullet">
/// <item><description>The file does not have valid .kreplay magic bytes.</description></item>
/// <item><description>The file structure is corrupted or truncated.</description></item>
/// <item><description>Required data sections are missing or malformed.</description></item>
/// <item><description>The checksum validation fails (data corruption).</description></item>
/// </list>
/// </para>
/// <para>
/// For version-related errors where the format is valid but unsupported,
/// see <see cref="ReplayVersionException"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// try
/// {
///     player.LoadReplay("recording.kreplay");
/// }
/// catch (ReplayFormatException ex)
/// {
///     Console.WriteLine($"Invalid replay file: {ex.Message}");
///     if (ex.FilePath is not null)
///     {
///         Console.WriteLine($"File: {ex.FilePath}");
///     }
/// }
/// </code>
/// </example>
public class ReplayFormatException : ReplayException
{
    /// <summary>
    /// Gets the path of the replay file that had the format error, if available.
    /// </summary>
    /// <remarks>
    /// This property is null when the replay was loaded from a stream or byte array
    /// without an associated file path.
    /// </remarks>
    public string? FilePath { get; }

    /// <summary>
    /// Gets the specific format issue that was detected, if available.
    /// </summary>
    public string? FormatIssue { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayFormatException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ReplayFormatException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayFormatException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="filePath">The path of the replay file that had the format error.</param>
    public ReplayFormatException(string message, string? filePath)
        : base(message)
    {
        FilePath = filePath;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayFormatException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="filePath">The path of the replay file that had the format error.</param>
    /// <param name="formatIssue">The specific format issue that was detected.</param>
    public ReplayFormatException(string message, string? filePath, string? formatIssue)
        : base(message)
    {
        FilePath = filePath;
        FormatIssue = formatIssue;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayFormatException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public ReplayFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayFormatException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="filePath">The path of the replay file that had the format error.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public ReplayFormatException(string message, string? filePath, Exception innerException)
        : base(message, innerException)
    {
        FilePath = filePath;
    }

    /// <summary>
    /// Creates a format exception for invalid magic bytes.
    /// </summary>
    /// <param name="filePath">The path of the file, if available.</param>
    /// <returns>A new exception instance.</returns>
    public static ReplayFormatException InvalidMagicBytes(string? filePath = null)
        => new(
            "Invalid replay file: missing or incorrect magic bytes. The file is not a valid .kreplay file.",
            filePath,
            "InvalidMagicBytes");

    /// <summary>
    /// Creates a format exception for a corrupted file.
    /// </summary>
    /// <param name="filePath">The path of the file, if available.</param>
    /// <param name="details">Additional details about the corruption.</param>
    /// <returns>A new exception instance.</returns>
    public static ReplayFormatException Corrupted(string? filePath = null, string? details = null)
    {
        var message = "Replay file is corrupted or truncated.";
        if (details is not null)
        {
            message += $" {details}";
        }

        return new ReplayFormatException(message, filePath, "Corrupted");
    }

    /// <summary>
    /// Creates a format exception for checksum validation failure.
    /// </summary>
    /// <param name="expectedChecksum">The expected checksum value.</param>
    /// <param name="actualChecksum">The actual checksum value.</param>
    /// <param name="filePath">The path of the file, if available.</param>
    /// <returns>A new exception instance.</returns>
    public static ReplayFormatException ChecksumMismatch(
        string expectedChecksum,
        string actualChecksum,
        string? filePath = null)
        => new(
            $"Replay file checksum validation failed. Expected: {expectedChecksum}, Actual: {actualChecksum}. The file may be corrupted.",
            filePath,
            "ChecksumMismatch");

    /// <summary>
    /// Creates a format exception for deserialization failure.
    /// </summary>
    /// <param name="innerException">The exception from the deserializer.</param>
    /// <param name="filePath">The path of the file, if available.</param>
    /// <returns>A new exception instance.</returns>
    public static ReplayFormatException DeserializationFailed(Exception innerException, string? filePath = null)
        => new(
            $"Failed to deserialize replay data: {innerException.Message}",
            filePath,
            innerException);
}
