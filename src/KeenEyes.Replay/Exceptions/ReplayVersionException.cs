namespace KeenEyes.Replay;

/// <summary>
/// Exception thrown when a replay file version is not supported.
/// </summary>
/// <remarks>
/// <para>
/// This exception is thrown when:
/// <list type="bullet">
/// <item><description>
/// The replay file was created with a newer version of the format than the current player supports.
/// This typically means the application needs to be updated.
/// </description></item>
/// <item><description>
/// The replay file was created with an older version that is no longer supported and cannot be migrated.
/// </description></item>
/// </list>
/// </para>
/// <para>
/// The exception includes version information to help diagnose compatibility issues
/// and provide guidance to users.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// try
/// {
///     player.LoadReplay("recording.kreplay");
/// }
/// catch (ReplayVersionException ex)
/// {
///     Console.WriteLine($"Version mismatch: file is v{ex.FileVersion}, but player supports v{ex.CurrentVersion}");
///     if (ex.FileVersion > ex.CurrentVersion)
///     {
///         Console.WriteLine("Please update the application to play this replay.");
///     }
/// }
/// </code>
/// </example>
public class ReplayVersionException : ReplayException
{
    /// <summary>
    /// Gets the version of the replay file that was being loaded.
    /// </summary>
    public int FileVersion { get; }

    /// <summary>
    /// Gets the current version supported by the player.
    /// </summary>
    public int CurrentVersion { get; }

    /// <summary>
    /// Gets the minimum version supported by the player, if applicable.
    /// </summary>
    /// <remarks>
    /// This is null if there is no minimum version constraint.
    /// </remarks>
    public int? MinimumSupportedVersion { get; }

    /// <summary>
    /// Gets the path of the replay file that had the version error, if available.
    /// </summary>
    public string? FilePath { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayVersionException"/> class.
    /// </summary>
    /// <param name="fileVersion">The version of the replay file.</param>
    /// <param name="currentVersion">The current version supported by the player.</param>
    public ReplayVersionException(int fileVersion, int currentVersion)
        : base(CreateMessage(fileVersion, currentVersion, null))
    {
        FileVersion = fileVersion;
        CurrentVersion = currentVersion;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayVersionException"/> class.
    /// </summary>
    /// <param name="fileVersion">The version of the replay file.</param>
    /// <param name="currentVersion">The current version supported by the player.</param>
    /// <param name="filePath">The path of the replay file.</param>
    public ReplayVersionException(int fileVersion, int currentVersion, string? filePath)
        : base(CreateMessage(fileVersion, currentVersion, filePath))
    {
        FileVersion = fileVersion;
        CurrentVersion = currentVersion;
        FilePath = filePath;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayVersionException"/> class.
    /// </summary>
    /// <param name="fileVersion">The version of the replay file.</param>
    /// <param name="currentVersion">The current version supported by the player.</param>
    /// <param name="minimumSupportedVersion">The minimum version supported by the player.</param>
    /// <param name="filePath">The path of the replay file.</param>
    public ReplayVersionException(
        int fileVersion,
        int currentVersion,
        int? minimumSupportedVersion,
        string? filePath)
        : base(CreateMessage(fileVersion, currentVersion, filePath, minimumSupportedVersion))
    {
        FileVersion = fileVersion;
        CurrentVersion = currentVersion;
        MinimumSupportedVersion = minimumSupportedVersion;
        FilePath = filePath;
    }

    /// <summary>
    /// Gets a value indicating whether the file version is newer than the current player supports.
    /// </summary>
    /// <remarks>
    /// When true, the user should update their application to play this replay.
    /// </remarks>
    public bool IsNewerThanSupported => FileVersion > CurrentVersion;

    /// <summary>
    /// Gets a value indicating whether the file version is older than the minimum supported version.
    /// </summary>
    /// <remarks>
    /// When true, the replay file is too old to be played by this version of the player.
    /// </remarks>
    public bool IsOlderThanSupported =>
        MinimumSupportedVersion.HasValue && FileVersion < MinimumSupportedVersion.Value;

    /// <summary>
    /// Creates a version exception for an unknown or unparseable version error.
    /// </summary>
    /// <param name="details">Additional details about the version error.</param>
    /// <param name="filePath">The path of the file, if available.</param>
    /// <returns>A new exception instance.</returns>
    public static ReplayVersionException UnknownVersion(string details, string? filePath = null)
    {
        var fileInfo = filePath is not null ? $"'{filePath}' " : "";
        var message = $"Cannot load replay {fileInfo}due to a version error: {details}";

        return new ReplayVersionException(message, 0, ReplayData.CurrentVersion, filePath);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayVersionException"/> class
    /// with a custom message.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="fileVersion">The version of the replay file.</param>
    /// <param name="currentVersion">The current version supported by the player.</param>
    /// <param name="filePath">The path of the replay file.</param>
    private ReplayVersionException(string message, int fileVersion, int currentVersion, string? filePath)
        : base(message)
    {
        FileVersion = fileVersion;
        CurrentVersion = currentVersion;
        FilePath = filePath;
    }

    private static string CreateMessage(
        int fileVersion,
        int currentVersion,
        string? filePath,
        int? minimumSupportedVersion = null)
    {
        var fileInfo = filePath is not null ? $"'{filePath}' " : "";

        if (fileVersion > currentVersion)
        {
            return $"Cannot load replay {fileInfo}(version {fileVersion}). " +
                   $"The current player supports version {currentVersion}. " +
                   "Please update the application to play this replay.";
        }

        if (minimumSupportedVersion.HasValue && fileVersion < minimumSupportedVersion.Value)
        {
            return $"Cannot load replay {fileInfo}(version {fileVersion}). " +
                   $"The minimum supported version is {minimumSupportedVersion.Value}. " +
                   "This replay was created with an outdated version that is no longer supported.";
        }

        return $"Cannot load replay {fileInfo}(version {fileVersion}). " +
               $"Current player version is {currentVersion}. " +
               "Version compatibility check failed.";
    }
}
