using KeenEyes.Input.Silk;
using KeenEyes.Platform.Silk;
using SilkInput = Silk.NET.Input;
using SilkWindowing = Silk.NET.Windowing;

namespace KeenEyes.Input.Silk.Tests;

/// <summary>
/// Covers what <see cref="SilkInputContext"/> says when a primary device is unavailable.
/// </summary>
/// <remarks>
/// Regression coverage for #1256. All three device getters used to throw the single message
/// "Input not initialized. Wait for window to load.", which made a perfectly initialized
/// context with no controller plugged in indistinguishable from a window that had not loaded —
/// and sent everyone debugging the wrong problem. The two states must now read differently.
/// </remarks>
public class SilkInputContextDeviceAvailabilityTests
{
    #region Initialized, but the device is absent

    [Fact]
    public void Gamepad_WhenInitializedWithNoGamepad_ThrowsAboutTheAbsentDevice()
    {
        using var provider = new FakeWindowProvider(new FakeSilkInputContext());
        using var input = CreateLoadedContext(provider);

        Assert.True(input.IsInitialized);

        var exception = Assert.Throws<InvalidOperationException>(() => input.Gamepad);

        // Assert on the distinguishing facts, not the sentence: the message must name the
        // absent device and the safe property to test, and must NOT blame initialization.
        Assert.Contains("gamepad", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(nameof(input.ConnectedGamepadCount), exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("not initialized", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("wait", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Keyboard_WhenInitializedWithNoKeyboard_ThrowsAboutTheAbsentDevice()
    {
        using var provider = new FakeWindowProvider(new FakeSilkInputContext());
        using var input = CreateLoadedContext(provider);

        var exception = Assert.Throws<InvalidOperationException>(() => input.Keyboard);

        Assert.Contains("keyboard", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(nameof(input.Keyboards), exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("not initialized", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Mouse_WhenInitializedWithNoMouse_ThrowsAboutTheAbsentDevice()
    {
        using var provider = new FakeWindowProvider(new FakeSilkInputContext());
        using var input = CreateLoadedContext(provider);

        var exception = Assert.Throws<InvalidOperationException>(() => input.Mouse);

        Assert.Contains("mouse", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(nameof(input.Mice), exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("not initialized", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Gamepad_WhenGamepadsAreDisabledInConfig_ThrowsAboutTheConfiguration()
    {
        using var provider = new FakeWindowProvider(new FakeSilkInputContext());
        using var input = new SilkInputContext(provider, new SilkInputConfig { EnableGamepads = false });
        provider.RaiseLoad();

        var exception = Assert.Throws<InvalidOperationException>(() => input.Gamepad);

        // A deliberate configuration choice must not masquerade as missing hardware.
        Assert.Contains(nameof(SilkInputConfig.EnableGamepads), exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("not initialized", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SafeQueries_WhenInitializedWithNoDevices_ReportEmptyInsteadOfThrowing()
    {
        using var provider = new FakeWindowProvider(new FakeSilkInputContext());
        using var input = CreateLoadedContext(provider);

        // The properties callers are told to check must themselves be safe to read.
        Assert.Empty(input.Gamepads);
        Assert.Empty(input.Keyboards);
        Assert.Empty(input.Mice);
        Assert.Equal(0, input.ConnectedGamepadCount);
    }

    #endregion

    #region Not initialized yet

    [Fact]
    public void Gamepad_WhenWindowHasNotLoaded_ThrowsAboutWaitingForTheWindow()
    {
        using var provider = new FakeWindowProvider(new FakeSilkInputContext());
        using var input = new SilkInputContext(provider, new SilkInputConfig());

        // No load event raised: this is the case the old message was actually written for.
        Assert.False(input.IsInitialized);

        var exception = Assert.Throws<InvalidOperationException>(() => input.Gamepad);

        Assert.Contains("not initialized", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("load", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Keyboard_WhenWindowHasNotLoaded_ThrowsAboutWaitingForTheWindow()
    {
        using var provider = new FakeWindowProvider(new FakeSilkInputContext());
        using var input = new SilkInputContext(provider, new SilkInputConfig());

        var exception = Assert.Throws<InvalidOperationException>(() => input.Keyboard);

        Assert.Contains("not initialized", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("load", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IsInitialized_WhenProviderYieldsNoInputContext_StaysFalse()
    {
        // A provider that loaded without producing an input context: the context has nothing
        // to wrap, so it must not claim to be initialized. It used to set the flag anyway,
        // which made IsInitialized report success while every device getter failed.
        using var provider = new FakeWindowProvider(silkInputContext: null);
        using var input = CreateLoadedContext(provider);

        Assert.False(input.IsInitialized);

        var exception = Assert.Throws<InvalidOperationException>(() => input.Gamepad);
        Assert.Contains("not initialized", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Helpers

    private static SilkInputContext CreateLoadedContext(FakeWindowProvider provider)
    {
        var input = new SilkInputContext(provider, new SilkInputConfig());
        provider.RaiseLoad();
        return input;
    }

    /// <summary>
    /// A window provider that never opens a window: it only hands out a Silk.NET input context
    /// and raises <see cref="ISilkWindowProvider.OnLoad"/> on demand, which is all
    /// <see cref="SilkInputContext"/> needs. That keeps these tests headless.
    /// </summary>
    private sealed class FakeWindowProvider(SilkInput.IInputContext? silkInputContext) : ISilkWindowProvider
    {
        public SilkWindowing.IWindow Window
            => throw new NotSupportedException("These tests never open a window.");

        public SilkInput.IInputContext InputContext => silkInputContext!;

        public event Action? OnLoad;

        // The rest of the lifecycle is irrelevant to input, so these accept subscribers
        // and never fire.
        public event Action<double>? OnUpdate { add { } remove { } }

        public event Action<double>? OnRender { add { } remove { } }

        public event Action<int, int>? OnResize { add { } remove { } }

        public event Action? OnClosing { add { } remove { } }

        public void RaiseLoad() => OnLoad?.Invoke();

        public void Dispose() => silkInputContext?.Dispose();
    }

    /// <summary>
    /// A Silk.NET input context for a machine with no input hardware at all: every device
    /// list is empty, which is exactly the shape the gamepad case takes in the real world.
    /// </summary>
    private sealed class FakeSilkInputContext : SilkInput.IInputContext
    {
        public nint Handle => 0;

        public IReadOnlyList<SilkInput.IGamepad> Gamepads => [];

        public IReadOnlyList<SilkInput.IJoystick> Joysticks => [];

        public IReadOnlyList<SilkInput.IKeyboard> Keyboards => [];

        public IReadOnlyList<SilkInput.IMouse> Mice => [];

        public IReadOnlyList<SilkInput.IInputDevice> OtherDevices => [];

        // Nothing ever connects or disconnects in these tests.
        public event Action<SilkInput.IInputDevice, bool>? ConnectionChanged { add { } remove { } }

        public void Dispose()
        {
            // No native resources behind this fake.
        }
    }

    #endregion
}
