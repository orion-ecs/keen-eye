namespace KeenEyes.Replay.Ghost;

/// <summary>
/// Exception thrown when a ghost file has an unsupported version.
/// </summary>
/// <remarks>
/// <para>
/// This exception is thrown when attempting to load a ghost file that was
/// created with a newer version of the format than the current code supports.
/// The file structure is valid, but the version number indicates it may
/// contain features or data that cannot be properly interpreted.
/// </para>
/// <para>
/// For invalid or corrupted files, see <see cref="GhostFormatException"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// try
/// {
///     var (_, ghost) = GhostFileFormat.ReadFromFile("ghost.keghost");
/// }
/// catch (GhostVersionException ex)
/// {
///     Console.WriteLine($"Ghost version {ex.FileVersion} is not supported.");
///     Console.WriteLine($"Maximum supported version: {ex.SupportedVersion}");
///     Console.WriteLine("Please update KeenEyes to load this ghost.");
/// }
/// </code>
/// </example>
public class GhostVersionException : ReplayException
{
    /// <summary>
    /// Gets the version number found in the ghost file.
    /// </summary>
    public int FileVersion { get; }

    /// <summary>
    /// Gets the maximum version supported by this code.
    /// </summary>
    public int SupportedVersion { get; }

    /// <summary>
    /// Gets the path of the ghost file, if available.
    /// </summary>
    public string? FilePath { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GhostVersionException"/> class.
    /// </summary>
    /// <param name="fileVersion">The version found in the file.</param>
    /// <param name="supportedVersion">The maximum supported version.</param>
    /// <param name="filePath">The path of the file, if available.</param>
    public GhostVersionException(int fileVersion, int supportedVersion, string? filePath = null)
        : base($"Ghost file version {fileVersion} is not supported. Maximum supported version is {supportedVersion}.")
    {
        FileVersion = fileVersion;
        SupportedVersion = supportedVersion;
        FilePath = filePath;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GhostVersionException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="filePath">The path of the file, if available.</param>
    public GhostVersionException(string message, string? filePath = null)
        : base(message)
    {
        FilePath = filePath;
    }

    /// <summary>
    /// Creates a version exception for an unknown version.
    /// </summary>
    /// <param name="details">Additional details about the error.</param>
    /// <param name="filePath">The path of the file, if available.</param>
    /// <returns>A new exception instance.</returns>
    public static GhostVersionException UnknownVersion(string details, string? filePath = null)
        => new($"Ghost file has an unknown version format. {details}", filePath);
}
