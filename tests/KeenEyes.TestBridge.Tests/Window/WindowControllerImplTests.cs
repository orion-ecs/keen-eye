using KeenEyes.Graphics.Abstractions;
using KeenEyes.TestBridge.Window;

namespace KeenEyes.TestBridge.Tests.Window;

public class WindowControllerImplTests
{
    #region IsAvailable

    [Fact]
    public void IsAvailable_WithWindow_ReturnsTrue()
    {
        using var world = new World();
        var mockWindow = new TestWindow();
        using var bridge = new InProcessBridge(world, window: mockWindow);

        bridge.Window.IsAvailable.ShouldBeTrue();
    }

    [Fact]
    public void IsAvailable_WithoutWindow_ReturnsFalse()
    {
        using var world = new World();
        using var bridge = new InProcessBridge(world);

        bridge.Window.IsAvailable.ShouldBeFalse();
    }

    #endregion

    #region GetStateAsync

    [Fact]
    public async Task GetStateAsync_WithWindow_ReturnsState()
    {
        using var world = new World();
        var mockWindow = new TestWindow
        {
            Width = 1920,
            Height = 1080,
            Title = "Test Window",
            IsClosing = false,
            IsFocused = true
        };
        using var bridge = new InProcessBridge(world, window: mockWindow);

        var state = await bridge.Window.GetStateAsync();

        state.Width.ShouldBe(1920);
        state.Height.ShouldBe(1080);
        state.Title.ShouldBe("Test Window");
        state.IsClosing.ShouldBeFalse();
        state.IsFocused.ShouldBeTrue();
        state.AspectRatio.ShouldBe(1920f / 1080f, 0.01f);
    }

    [Fact]
    public async Task GetStateAsync_WithoutWindow_ReturnsDefaults()
    {
        using var world = new World();
        using var bridge = new InProcessBridge(world);

        var state = await bridge.Window.GetStateAsync();

        state.Width.ShouldBe(0);
        state.Height.ShouldBe(0);
        state.Title.ShouldBe(string.Empty);
        state.IsClosing.ShouldBeFalse();
        state.IsFocused.ShouldBeFalse();
        state.AspectRatio.ShouldBe(0f);
    }

    #endregion

    #region GetSizeAsync

    [Fact]
    public async Task GetSizeAsync_WithWindow_ReturnsSize()
    {
        using var world = new World();
        var mockWindow = new TestWindow
        {
            Width = 1280,
            Height = 720
        };
        using var bridge = new InProcessBridge(world, window: mockWindow);

        var (width, height) = await bridge.Window.GetSizeAsync();

        width.ShouldBe(1280);
        height.ShouldBe(720);
    }

    [Fact]
    public async Task GetSizeAsync_WithoutWindow_ReturnsZero()
    {
        using var world = new World();
        using var bridge = new InProcessBridge(world);

        var (width, height) = await bridge.Window.GetSizeAsync();

        width.ShouldBe(0);
        height.ShouldBe(0);
    }

    #endregion

    #region GetTitleAsync

    [Fact]
    public async Task GetTitleAsync_WithWindow_ReturnsTitle()
    {
        using var world = new World();
        var mockWindow = new TestWindow { Title = "My Game Title" };
        using var bridge = new InProcessBridge(world, window: mockWindow);

        var title = await bridge.Window.GetTitleAsync();

        title.ShouldBe("My Game Title");
    }

    [Fact]
    public async Task GetTitleAsync_WithoutWindow_ReturnsEmpty()
    {
        using var world = new World();
        using var bridge = new InProcessBridge(world);

        var title = await bridge.Window.GetTitleAsync();

        title.ShouldBe(string.Empty);
    }

    #endregion

    #region IsClosingAsync

    [Fact]
    public async Task IsClosingAsync_WithWindow_NotClosing_ReturnsFalse()
    {
        using var world = new World();
        var mockWindow = new TestWindow { IsClosing = false };
        using var bridge = new InProcessBridge(world, window: mockWindow);

        var isClosing = await bridge.Window.IsClosingAsync();

        isClosing.ShouldBeFalse();
    }

    [Fact]
    public async Task IsClosingAsync_WithWindow_Closing_ReturnsTrue()
    {
        using var world = new World();
        var mockWindow = new TestWindow { IsClosing = true };
        using var bridge = new InProcessBridge(world, window: mockWindow);

        var isClosing = await bridge.Window.IsClosingAsync();

        isClosing.ShouldBeTrue();
    }

    [Fact]
    public async Task IsClosingAsync_WithoutWindow_ReturnsFalse()
    {
        using var world = new World();
        using var bridge = new InProcessBridge(world);

        var isClosing = await bridge.Window.IsClosingAsync();

        isClosing.ShouldBeFalse();
    }

    #endregion

    #region IsFocusedAsync

    [Fact]
    public async Task IsFocusedAsync_WithWindow_Focused_ReturnsTrue()
    {
        using var world = new World();
        var mockWindow = new TestWindow { IsFocused = true };
        using var bridge = new InProcessBridge(world, window: mockWindow);

        var isFocused = await bridge.Window.IsFocusedAsync();

        isFocused.ShouldBeTrue();
    }

    [Fact]
    public async Task IsFocusedAsync_WithWindow_NotFocused_ReturnsFalse()
    {
        using var world = new World();
        var mockWindow = new TestWindow { IsFocused = false };
        using var bridge = new InProcessBridge(world, window: mockWindow);

        var isFocused = await bridge.Window.IsFocusedAsync();

        isFocused.ShouldBeFalse();
    }

    [Fact]
    public async Task IsFocusedAsync_WithoutWindow_ReturnsFalse()
    {
        using var world = new World();
        using var bridge = new InProcessBridge(world);

        var isFocused = await bridge.Window.IsFocusedAsync();

        isFocused.ShouldBeFalse();
    }

    #endregion

    #region GetAspectRatioAsync

    [Fact]
    public async Task GetAspectRatioAsync_WithWindow_ReturnsRatio()
    {
        using var world = new World();
        var mockWindow = new TestWindow
        {
            Width = 1600,
            Height = 900
        };
        using var bridge = new InProcessBridge(world, window: mockWindow);

        var ratio = await bridge.Window.GetAspectRatioAsync();

        ratio.ShouldBe(1600f / 900f, 0.01f);
    }

    [Fact]
    public async Task GetAspectRatioAsync_WithWindow_ZeroHeight_ReturnsZero()
    {
        using var world = new World();
        var mockWindow = new TestWindow
        {
            Width = 1920,
            Height = 0
        };
        using var bridge = new InProcessBridge(world, window: mockWindow);

        var ratio = await bridge.Window.GetAspectRatioAsync();

        ratio.ShouldBe(0f);
    }

    [Fact]
    public async Task GetAspectRatioAsync_WithoutWindow_ReturnsZero()
    {
        using var world = new World();
        using var bridge = new InProcessBridge(world);

        var ratio = await bridge.Window.GetAspectRatioAsync();

        ratio.ShouldBe(0f);
    }

    #endregion

    /// <summary>
    /// Test implementation of IWindow for unit testing.
    /// </summary>
#pragma warning disable CS0067 // Events are never used - required by interface
    private sealed class TestWindow : IWindow
    {
        public int Width { get; set; } = 800;
        public int Height { get; set; } = 600;
        public string Title { get; set; } = "Test Window";
        public bool IsClosing { get; set; }
        public bool IsFocused { get; set; } = true;
        public float AspectRatio => Height > 0 ? (float)Width / Height : 0f;

        public event Action? OnLoad;
        public event Action<int, int>? OnResize;
        public event Action? OnClosing;
        public event Action<double>? OnUpdate;
        public event Action<double>? OnRender;

        public IGraphicsDevice CreateDevice() => throw new NotSupportedException();
        public void Run() { }
        public void DoEvents() { }
        public void SwapBuffers() { }
        public void Close() => IsClosing = true;
        public void Dispose() { }
    }
#pragma warning restore CS0067
}
