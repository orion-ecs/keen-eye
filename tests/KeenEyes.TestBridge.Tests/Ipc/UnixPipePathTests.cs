using KeenEyes.TestBridge.Ipc.Transport;

namespace KeenEyes.TestBridge.Tests.Ipc;

/// <summary>
/// Tests for the Unix domain socket path limit that named pipes are subject to on
/// macOS and Linux.
/// </summary>
/// <remarks>
/// These drive the pure validation function with an explicit temp path and limit, so the
/// macOS behaviour is verified on any platform. That matters: the failure they describe
/// was found by the first macOS CI run and cannot be reproduced on Linux, whose temp
/// directory is short enough that the same names fit comfortably.
/// </remarks>
public class UnixPipePathTests
{
    /// <summary>A realistic macOS per-user temp directory (48 characters).</summary>
    private const string MacTempPath = "/var/folders/8j/sfr9qqcj73j4p6nhwcfpr0th0000gn/T/";

    [Fact]
    public void TryValidate_MacOsTempWithVerboseNameAndFullGuid_Rejects()
    {
        // The exact shape that failed on the first macOS CI run: 118 characters against
        // a 104 limit.
        var pipeName = $"KeenEyes.TestBridge.Tests.{new string('a', 32)}";

        var fits = UnixPipePath.TryValidate(MacTempPath, pipeName, UnixPipePath.MacOsPathLimit, out var error);

        Assert.False(fits);
        Assert.NotNull(error);
        Assert.Contains(pipeName, error, StringComparison.Ordinal);
        Assert.Contains("118", error, StringComparison.Ordinal);
        Assert.Contains("104", error, StringComparison.Ordinal);
        Assert.Contains("14", error, StringComparison.Ordinal);
    }

    [Fact]
    public void TryValidate_ShortTestName_FitsWithHeadroom()
    {
        var pipeName = TestPipeName.Create();

        var fits = UnixPipePath.TryValidate(MacTempPath, pipeName, UnixPipePath.MacOsPathLimit, out var error);

        Assert.True(fits);
        Assert.Null(error);

        // Not merely under the limit — comfortably so, since a longer TMPDIR should not
        // start failing tests again.
        var path = UnixPipePath.BuildSocketPath(MacTempPath, pipeName);
        Assert.True(
            UnixPipePath.MacOsPathLimit - path.Length >= 20,
            $"Expected >= 20 characters of headroom, path was {path.Length}.");
    }

    [Fact]
    public void TryValidate_ProductionPipeNames_FitOnMacOs()
    {
        // The names real applications register; a regression here breaks shipping games,
        // not just tests.
        string[] production =
        [
            "KeenEyes.TestBridge",
            "KeenEyes.Editor.TestBridge",
            "KeenEyes.Editor.Scene.TestBridge",
            "KeenEyes.Sample.UI.TestBridge",
            "KeenEyes.InputDebugger.TestBridge",
            "KeenEyes.NovaFall.TestBridge",
        ];

        foreach (var pipeName in production)
        {
            var fits = UnixPipePath.TryValidate(MacTempPath, pipeName, UnixPipePath.MacOsPathLimit, out var error);
            Assert.True(fits, error);
        }
    }

    [Fact]
    public void TryValidate_AtExactlyTheLimit_Fits()
    {
        // Boundary: the limit is inclusive.
        var prefixLength = UnixPipePath.BuildSocketPath(MacTempPath, string.Empty).Length;
        var pipeName = new string('a', UnixPipePath.MacOsPathLimit - prefixLength);

        var fits = UnixPipePath.TryValidate(MacTempPath, pipeName, UnixPipePath.MacOsPathLimit, out var error);

        Assert.True(fits, error);
    }

    [Fact]
    public void TryValidate_OneCharacterOverTheLimit_Rejects()
    {
        var prefixLength = UnixPipePath.BuildSocketPath(MacTempPath, string.Empty).Length;
        var pipeName = new string('a', UnixPipePath.MacOsPathLimit - prefixLength + 1);

        var fits = UnixPipePath.TryValidate(MacTempPath, pipeName, UnixPipePath.MacOsPathLimit, out var error);

        Assert.False(fits);
        Assert.Contains("at least 1 character", error!, StringComparison.Ordinal);
    }

    [Fact]
    public void TryValidate_LinuxAllowsSlightlyLongerPaths()
    {
        // Linux permits 108 where macOS permits 104, so a name can be valid on one and
        // not the other; the check must use the platform's own limit.
        var prefixLength = UnixPipePath.BuildSocketPath(MacTempPath, string.Empty).Length;
        var pipeName = new string('a', UnixPipePath.LinuxPathLimit - prefixLength);

        Assert.True(UnixPipePath.TryValidate(MacTempPath, pipeName, UnixPipePath.LinuxPathLimit, out _));
        Assert.False(UnixPipePath.TryValidate(MacTempPath, pipeName, UnixPipePath.MacOsPathLimit, out _));
    }

    [Fact]
    public void CurrentPlatformLimit_IsNullOnWindowsAndSetOnUnix()
    {
        var limit = UnixPipePath.CurrentPlatformLimit;

        if (OperatingSystem.IsWindows())
        {
            Assert.Null(limit);
        }
        else if (OperatingSystem.IsLinux())
        {
            Assert.Equal(UnixPipePath.LinuxPathLimit, limit);
        }
        else
        {
            Assert.Equal(UnixPipePath.MacOsPathLimit, limit);
        }
    }

    [Fact]
    public void NamedPipeTransport_WithOverlongName_ThrowsNamingThePipeAndLimit()
    {
        // On Windows the limit does not apply, so the constructor must accept the name.
        var pipeName = new string('a', 300);

        if (OperatingSystem.IsWindows())
        {
            using var transport = new NamedPipeTransport(pipeName, isServer: true);
            Assert.False(transport.IsConnected);
            return;
        }

        var exception = Assert.Throws<ArgumentException>(
            () => new NamedPipeTransport(pipeName, isServer: true));

        // The whole point of the guard: the message must identify the pipe and the limit,
        // unlike the BCL's bare "invalid length for use with domain sockets".
        Assert.Contains("too long", exception.Message, StringComparison.Ordinal);
        Assert.Contains(UnixPipePath.CurrentPlatformLimit!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), exception.Message, StringComparison.Ordinal);
        Assert.Equal("pipeName", exception.ParamName);
    }

    [Fact]
    public void NamedPipeTransport_WithShortName_Constructs()
    {
        using var transport = new NamedPipeTransport(TestPipeName.Create(), isServer: true);

        Assert.False(transport.IsConnected);
    }
}
