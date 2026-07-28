using KeenEyes.Platform.Silk.Tests.Mocks;

namespace KeenEyes.Platform.Silk.Tests;

/// <summary>
/// Tests for <see cref="SilkLoopProvider"/>, focused on the thread check that runs immediately
/// before the OS window is created (#1364).
/// </summary>
public class SilkLoopProviderTests
{
    #region Run

    [Fact]
    public void Run_OnNonMacOS_ReachesWindowCreation()
    {
        Assert.SkipWhen(OperatingSystem.IsMacOS(), "The guard is expected to apply on macOS.");

        using var windowProvider = new WindowlessSilkWindowProvider();
        var loopProvider = new SilkLoopProvider(windowProvider);

        // The sentinel proves Run() got all the way to the window, so nothing before it -
        // including the thread check - short-circuited the call.
        var exception = Assert.Throws<NotSupportedException>(loopProvider.Run);

        Assert.Equal(WindowlessSilkWindowProvider.WindowAccessMessage, exception.Message);
    }

    [Fact]
    public async Task Run_OnNonMacOSFromThreadPoolThread_ReachesWindowCreation()
    {
        Assert.SkipWhen(OperatingSystem.IsMacOS(), "The guard is expected to apply on macOS.");

        using var windowProvider = new WindowlessSilkWindowProvider();
        var loopProvider = new SilkLoopProvider(windowProvider);

        // Task.Run reproduces exactly what an 'await' before Run() does to the calling thread.
        // Platforms other than macOS must be unaffected by the new check.
        var exception = await Task.Run(() => Record.Exception(loopProvider.Run));

        var notSupported = Assert.IsType<NotSupportedException>(exception);
        Assert.Equal(WindowlessSilkWindowProvider.WindowAccessMessage, notSupported.Message);
    }

    #endregion
}
