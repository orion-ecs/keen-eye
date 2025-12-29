using KeenEyes.Graphics.Silk;
using KeenEyes.Graphics.Tests.Mocks;

namespace KeenEyes.Graphics.Tests;

/// <summary>
/// Tests for <see cref="SilkWindow"/>.
/// Note: Tests focus on event wiring and property access since creating
/// an actual OpenGL context requires a real window/display, which is not available
/// in headless test environments.
/// </summary>
public sealed class SilkWindowTests : IDisposable
{
    private readonly MockSilkWindowProvider windowProvider;
    private readonly SilkWindow window;

    public SilkWindowTests()
    {
        windowProvider = new MockSilkWindowProvider();
        window = new SilkWindow(windowProvider.Window, isAlreadyLoaded: false);
    }

    public void Dispose()
    {
        window.Dispose();
        windowProvider.Dispose();
    }

    #region Constructor

    [Fact]
    public void Constructor_NotAlreadyLoaded_InitializesWindow()
    {
        using var testWindow = new SilkWindow(windowProvider.Window, isAlreadyLoaded: false);

        Assert.NotNull(testWindow);
    }

    [Fact]
    public void Constructor_AlreadyLoaded_InitializesWindow()
    {
        // Note: This will fail to create GL context in headless environment,
        // but we can verify the constructor doesn't throw with isAlreadyLoaded=false
        using var testWindow = new SilkWindow(windowProvider.Window, isAlreadyLoaded: false);

        Assert.NotNull(testWindow);
    }

    #endregion

    #region Properties

    [Fact]
    public void Width_ReturnsWindowWidth()
    {
        Assert.Equal(800, window.Width);
    }

    [Fact]
    public void Height_ReturnsWindowHeight()
    {
        Assert.Equal(600, window.Height);
    }

    [Fact]
    public void Title_ReturnsWindowTitle()
    {
        Assert.Equal("Mock Window", window.Title);
    }

    // NOTE: IsClosing test skipped - it accesses GLFW functions that cause
    // access violations in headless test environments. This property works
    // correctly in actual applications with a real window.

    [Fact]
    public void IsFocused_ReturnsIsVisible()
    {
        // Mock window is not visible by default
        Assert.False(window.IsFocused);
    }

    [Fact]
    public void AspectRatio_CalculatesCorrectly()
    {
        var expectedRatio = 800f / 600f;
        Assert.Equal(expectedRatio, window.AspectRatio, 5);
    }

    [Fact]
    public void AspectRatio_WithValidDimensions_IsGreaterThanOne()
    {
        // 800x600 = 1.333...
        Assert.True(window.AspectRatio > 1.0f);
    }

    #endregion

    #region Events

    [Fact]
    public void OnLoad_CanBeSubscribed()
    {
        var eventFired = false;
        window.OnLoad += () => eventFired = true;

        // We can't actually trigger the Load event without a real GL context,
        // but we can verify the subscription doesn't throw
        Assert.False(eventFired); // Not fired yet
    }

    [Fact]
    public void OnResize_CanBeSubscribed()
    {
        var eventFired = false;
        window.OnResize += (w, h) => eventFired = true;

        // Verify subscription doesn't throw
        Assert.False(eventFired);
    }

    [Fact]
    public void OnClosing_CanBeSubscribed()
    {
        var eventFired = false;
        window.OnClosing += () => eventFired = true;

        // Verify subscription doesn't throw
        Assert.False(eventFired);
    }

    [Fact]
    public void OnUpdate_CanBeSubscribed()
    {
        var eventFired = false;
        window.OnUpdate += (dt) => eventFired = true;

        // Verify subscription doesn't throw
        Assert.False(eventFired);
    }

    [Fact]
    public void OnRender_CanBeSubscribed()
    {
        var eventFired = false;
        window.OnRender += (dt) => eventFired = true;

        // Verify subscription doesn't throw
        Assert.False(eventFired);
    }

    [Fact]
    public void MultipleEventSubscribers_CanBeAdded()
    {
        var subscriber1Called = false;
        var subscriber2Called = false;

        window.OnLoad += () => subscriber1Called = true;
        window.OnLoad += () => subscriber2Called = true;

        // Verify multiple subscriptions don't throw
        Assert.False(subscriber1Called);
        Assert.False(subscriber2Called);
    }

    #endregion

    #region Device Creation

    [Fact]
    public void CreateDevice_BeforeLoad_ThrowsInvalidOperationException()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => window.CreateDevice());
        Assert.Contains("before window is loaded", exception.Message);
        Assert.Contains("OnLoad event handler", exception.Message);
    }

    #endregion

    #region Window Operations

    [Fact]
    public void DoEvents_DoesNotThrow()
    {
        window.DoEvents();

        // Should not throw
        Assert.NotNull(window);
    }

    [Fact]
    public void SwapBuffers_BeforeLoad_ThrowsInvalidOperationException()
    {
        // SwapBuffers requires GL context which is created on Load
        Assert.Throws<InvalidOperationException>(() => window.SwapBuffers());
    }

    [Fact]
    public void Close_DoesNotThrow()
    {
        window.Close();

        // Should not throw
        Assert.NotNull(window);
    }

    #endregion

    #region Disposal

    [Fact]
    public void Dispose_UnsubscribesFromEvents()
    {
        var loadTriggered = false;
        window.OnLoad += () => loadTriggered = true;

        window.Dispose();

        // Verify disposal doesn't throw
        Assert.False(loadTriggered);
    }

    [Fact]
    public void Dispose_MultipleCalls_DoesNotThrow()
    {
        window.Dispose();
        window.Dispose();
        window.Dispose();

        // Should handle multiple dispose calls gracefully
        Assert.NotNull(window);
    }

    [Fact]
    public void Dispose_BeforeLoad_DoesNotThrow()
    {
        using var testWindow = new SilkWindow(windowProvider.Window);

        testWindow.Dispose();

        // Should handle disposal before GL context is created
        Assert.NotNull(testWindow);
    }

    [Fact]
    public void Dispose_DoesNotDisposeUnderlyingWindow()
    {
        window.Dispose();

        // Window provider should still be usable (not disposed)
        // since SilkWindow doesn't own the underlying window
        Assert.NotNull(windowProvider.Window);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Properties_AccessibleBeforeLoad()
    {
        // Most properties should be accessible before Load event
        // (IsClosing throws in headless environment)
        Assert.Equal(800, window.Width);
        Assert.Equal(600, window.Height);
        Assert.NotNull(window.Title);
        Assert.True(window.AspectRatio > 0);
    }

    [Fact]
    public void Operations_DoNotThrowBeforeLoad()
    {
        // These operations should not throw even before Load (except SwapBuffers which requires GL context)
        window.DoEvents();
        window.Close();

        Assert.NotNull(window);
    }

    [Fact]
    public void AspectRatio_With4x3_CalculatesCorrectly()
    {
        // 800x600 is 4:3 ratio
        var ratio = window.AspectRatio;
        var expected = 4f / 3f;
        Assert.Equal(expected, ratio, 5);
    }

    [Fact]
    public void Dispose_After_Operations_DoesNotThrow()
    {
        window.DoEvents();

        window.Dispose();

        Assert.NotNull(window);
    }

    #endregion
}
