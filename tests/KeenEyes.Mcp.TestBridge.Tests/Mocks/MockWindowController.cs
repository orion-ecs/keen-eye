using KeenEyes.TestBridge.Window;

namespace KeenEyes.Mcp.TestBridge.Tests.Mocks;

/// <summary>
/// Mock implementation of IWindowController for testing MCP tools.
/// </summary>
internal sealed class MockWindowController : IWindowController
{
    public bool IsAvailable { get; set; } = true;
    public (int Width, int Height) Size { get; set; } = (1920, 1080);
    public string Title { get; set; } = "MockWindow";
    public bool IsClosing { get; set; }
    public bool IsFocused { get; set; } = true;

    public Task<WindowStateSnapshot> GetStateAsync()
    {
        return Task.FromResult(new WindowStateSnapshot
        {
            Width = Size.Width,
            Height = Size.Height,
            Title = Title,
            IsClosing = IsClosing,
            IsFocused = IsFocused,
            AspectRatio = (float)Size.Width / Size.Height
        });
    }

    public Task<(int Width, int Height)> GetSizeAsync() => Task.FromResult(Size);

    public Task<string> GetTitleAsync() => Task.FromResult(Title);

    public Task<bool> IsClosingAsync() => Task.FromResult(IsClosing);

    public Task<bool> IsFocusedAsync() => Task.FromResult(IsFocused);

    public Task<float> GetAspectRatioAsync() => Task.FromResult((float)Size.Width / Size.Height);
}
