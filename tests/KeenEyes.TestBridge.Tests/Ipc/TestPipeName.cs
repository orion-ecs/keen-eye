namespace KeenEyes.TestBridge.Tests.Ipc;

/// <summary>
/// Generates unique, deliberately short pipe names for IPC tests.
/// </summary>
/// <remarks>
/// On macOS a named pipe is a Unix domain socket under the per-user temp directory, whose
/// full path may not exceed 104 characters. That directory alone is around 48 characters
/// (<c>/var/folders/xx/xxxxxxxxxxxxxxxxxxxxxxxxxxxx/T/</c>), and .NET prepends
/// <c>CoreFxPipe_</c>, so a descriptive name plus a full GUID overruns the limit and every
/// IPC test fails on macOS. This keeps names small enough to leave real headroom rather
/// than only just fitting.
/// </remarks>
internal static class TestPipeName
{
    /// <summary>
    /// Creates a unique pipe name short enough for the macOS socket path limit.
    /// </summary>
    /// <returns>A pipe name of the form <c>ke-<em>12 hex digits</em></c>.</returns>
    /// <remarks>
    /// 48 bits of randomness is ample for isolating concurrently running tests, and keeps
    /// the resulting socket path near 75 characters on macOS.
    /// </remarks>
    internal static string Create() => $"ke-{Guid.NewGuid():N}"[..15];
}
