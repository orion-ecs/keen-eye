namespace KeenEyes.Platform.Silk.Tests;

/// <summary>
/// Tests for <see cref="WindowThreadGuard"/>, the macOS main-thread requirement for OS window
/// creation (#1364).
/// </summary>
/// <remarks>
/// The guard only fires on macOS, so its decision is exercised through
/// <see cref="WindowThreadGuard.Validate(bool, bool)"/>, which takes the two platform facts as
/// inputs and can therefore be tested from any operating system.
/// </remarks>
public class WindowThreadGuardTests
{
    #region Validate

    [Fact]
    public void Validate_OnMacOSOffTheMainThread_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => WindowThreadGuard.Validate(isMacOS: true, isProcessMainThread: false));

        // Distinguishing fragments only - the wording around them is free to change.
        Assert.Contains("main thread", exception.Message, StringComparison.Ordinal);
        Assert.Contains("macOS", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_OnMacOSOffTheMainThread_NamesTheLikelyCause()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => WindowThreadGuard.Validate(isMacOS: true, isProcessMainThread: false));

        Assert.Contains("await", exception.Message, StringComparison.Ordinal);
        Assert.Contains("thread-pool thread", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_OnMacOSOffTheMainThread_StatesTheRemedy()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => WindowThreadGuard.Validate(isMacOS: true, isProcessMainThread: false));

        Assert.Contains("GetAwaiter().GetResult()", exception.Message, StringComparison.Ordinal);
        Assert.Contains("after Run() returns", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_OnMacOSOnTheMainThread_DoesNotThrow()
    {
        var exception = Record.Exception(
            () => WindowThreadGuard.Validate(isMacOS: true, isProcessMainThread: true));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Validate_OffMacOS_DoesNotThrowRegardlessOfThread(bool isProcessMainThread)
    {
        var exception = Record.Exception(
            () => WindowThreadGuard.Validate(isMacOS: false, isProcessMainThread));

        Assert.Null(exception);
    }

    #endregion

    #region EnsureWindowCreationThread

    [Fact]
    public void EnsureWindowCreationThread_OnNonMacOSPlatform_DoesNotThrow()
    {
        Assert.SkipWhen(OperatingSystem.IsMacOS(), "The guard is expected to apply on macOS.");

        var exception = Record.Exception(WindowThreadGuard.EnsureWindowCreationThread);

        Assert.Null(exception);
    }

    [Fact]
    public async Task EnsureWindowCreationThread_OnNonMacOSFromThreadPoolThread_DoesNotThrow()
    {
        Assert.SkipWhen(OperatingSystem.IsMacOS(), "The guard is expected to apply on macOS.");

        // Windows and Linux allow window creation from any thread; the guard must not have
        // silently imposed the macOS restriction on them. The thread-pool thread here is exactly
        // what an 'await' before Run() would resume on.
        var exception = await Task.Run(
            () => Record.Exception(WindowThreadGuard.EnsureWindowCreationThread));

        Assert.Null(exception);
    }

    #endregion
}
