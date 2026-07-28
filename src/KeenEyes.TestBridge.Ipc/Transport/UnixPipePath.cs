namespace KeenEyes.TestBridge.Ipc.Transport;

/// <summary>
/// Validates named-pipe names against the Unix domain socket path limit.
/// </summary>
/// <remarks>
/// <para>
/// On Windows a pipe name becomes an entry in the <c>\\.\pipe\</c> namespace and is
/// effectively unbounded for our purposes. On Linux and macOS .NET emulates named pipes
/// with Unix domain sockets, placing a file at
/// <c>{TempPath}/CoreFxPipe_{pipeName}</c> — and a socket path must fit the
/// <c>sockaddr_un.sun_path</c> field, which is 104 bytes on macOS/BSD and 108 on Linux.
/// </para>
/// <para>
/// Exceeding it surfaces from the BCL as a bare
/// <see cref="ArgumentOutOfRangeException"/> about "an invalid length for use with domain
/// sockets", naming neither the pipe nor the limit — so this check exists to report which
/// name was too long, by how much, and what to do about it. macOS is where this bites
/// first: its per-user temp directory is ~48 characters before the name is even appended.
/// </para>
/// </remarks>
public static class UnixPipePath
{
    /// <summary>
    /// The prefix .NET prepends to the socket file backing a named pipe on Unix.
    /// </summary>
    /// <remarks>
    /// This mirrors the runtime's own convention; it is not configurable. If a future
    /// runtime changes it, this check becomes conservative rather than wrong.
    /// </remarks>
    private const string SocketFilePrefix = "CoreFxPipe_";

    /// <summary>
    /// Maximum socket path length on macOS and other BSD-derived platforms.
    /// </summary>
    public const int MacOsPathLimit = 104;

    /// <summary>
    /// Maximum socket path length on Linux.
    /// </summary>
    public const int LinuxPathLimit = 108;

    /// <summary>
    /// Gets the socket path limit for the current platform, or <c>null</c> on platforms
    /// where named pipes are not backed by Unix domain sockets (Windows).
    /// </summary>
    public static int? CurrentPlatformLimit =>
        OperatingSystem.IsWindows() ? null
        : OperatingSystem.IsLinux() ? LinuxPathLimit
        : MacOsPathLimit;

    /// <summary>
    /// Builds the socket path .NET would use to back a named pipe on Unix.
    /// </summary>
    /// <param name="tempPath">The temporary directory (<see cref="Path.GetTempPath"/>).</param>
    /// <param name="pipeName">The pipe name.</param>
    /// <returns>The full socket file path.</returns>
    public static string BuildSocketPath(string tempPath, string pipeName) =>
        Path.Combine(tempPath, SocketFilePrefix + pipeName);

    /// <summary>
    /// Checks whether a pipe name fits the socket path limit, and explains it if not.
    /// </summary>
    /// <param name="tempPath">The temporary directory the socket file would live in.</param>
    /// <param name="pipeName">The pipe name to validate.</param>
    /// <param name="limit">The platform's socket path limit, in characters.</param>
    /// <param name="error">
    /// When this method returns <c>false</c>, a message naming the pipe, the resulting
    /// path length, the limit, and how to resolve it; otherwise <c>null</c>.
    /// </param>
    /// <returns><c>true</c> when the resulting path fits; otherwise <c>false</c>.</returns>
    /// <remarks>
    /// Kept as a pure function over an explicit temp path and limit so the behaviour is
    /// testable on any platform, rather than only on the one that rejects the path.
    /// </remarks>
    public static bool TryValidate(string tempPath, string pipeName, int limit, out string? error)
    {
        var path = BuildSocketPath(tempPath, pipeName);
        if (path.Length <= limit)
        {
            error = null;
            return true;
        }

        var excess = path.Length - limit;
        error =
            $"The pipe name '{pipeName}' is too long for this platform. It resolves to a "
            + $"Unix domain socket path of {path.Length} characters ('{path}'), but the limit "
            + $"is {limit}. Shorten the pipe name by at least {excess} character(s), or set "
            + "TMPDIR to a shorter directory.";
        return false;
    }

    /// <summary>
    /// Throws when a pipe name cannot fit the current platform's socket path limit.
    /// </summary>
    /// <param name="pipeName">The pipe name to validate.</param>
    /// <param name="paramName">The parameter name to report on failure.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the resulting socket path would exceed the platform limit.
    /// </exception>
    /// <remarks>No-ops on Windows, where the limit does not apply.</remarks>
    public static void ThrowIfTooLong(string pipeName, string paramName)
    {
        if (CurrentPlatformLimit is not int limit)
        {
            return;
        }

        if (!TryValidate(Path.GetTempPath(), pipeName, limit, out var error))
        {
            throw new ArgumentException(error, paramName);
        }
    }
}
