using System.ComponentModel;
using KeenEyes.Mcp.TestBridge.Connection;
using KeenEyes.TestBridge.Window;
using ModelContextProtocol.Server;

namespace KeenEyes.Mcp.TestBridge.Tools;

/// <summary>
/// MCP tools for window state queries.
/// </summary>
[McpServerToolType]
public sealed class WindowTools(BridgeConnectionManager connection)
{
    [McpServerTool(Name = "window_is_available")]
    [Description("Check if window state queries are available. Returns false if the game is running headless.")]
    public WindowAvailableResult WindowIsAvailable()
    {
        var bridge = connection.GetBridge();
        return new WindowAvailableResult
        {
            Available = bridge.Window.IsAvailable
        };
    }

    [McpServerTool(Name = "window_get_state")]
    [Description("Get complete window state including size, title, focus state, and closing status.")]
    public async Task<WindowStateResult> WindowGetState()
    {
        var bridge = connection.GetBridge();

        if (!bridge.Window.IsAvailable)
        {
            return new WindowStateResult
            {
                Success = false,
                Error = "Window state queries are not available (game may be running headless)"
            };
        }

        var state = await bridge.Window.GetStateAsync();
        return new WindowStateResult
        {
            Success = true,
            Width = state.Width,
            Height = state.Height,
            Title = state.Title,
            IsClosing = state.IsClosing,
            IsFocused = state.IsFocused,
            AspectRatio = state.AspectRatio
        };
    }

    [McpServerTool(Name = "window_get_size")]
    [Description("Get the current window dimensions in pixels.")]
    public async Task<WindowSizeResultMcp> WindowGetSize()
    {
        var bridge = connection.GetBridge();

        if (!bridge.Window.IsAvailable)
        {
            return new WindowSizeResultMcp
            {
                Success = false,
                Error = "Window state queries are not available (game may be running headless)"
            };
        }

        var (width, height) = await bridge.Window.GetSizeAsync();
        return new WindowSizeResultMcp
        {
            Success = true,
            Width = width,
            Height = height
        };
    }

    [McpServerTool(Name = "window_get_title")]
    [Description("Get the current window title.")]
    public async Task<WindowTitleResult> WindowGetTitle()
    {
        var bridge = connection.GetBridge();

        if (!bridge.Window.IsAvailable)
        {
            return new WindowTitleResult
            {
                Success = false,
                Error = "Window state queries are not available (game may be running headless)"
            };
        }

        var title = await bridge.Window.GetTitleAsync();
        return new WindowTitleResult
        {
            Success = true,
            Title = title
        };
    }

    [McpServerTool(Name = "window_is_closing")]
    [Description("Check if the window is in the process of closing.")]
    public async Task<WindowClosingResult> WindowIsClosing()
    {
        var bridge = connection.GetBridge();

        if (!bridge.Window.IsAvailable)
        {
            return new WindowClosingResult
            {
                Success = false,
                Error = "Window state queries are not available (game may be running headless)"
            };
        }

        var isClosing = await bridge.Window.IsClosingAsync();
        return new WindowClosingResult
        {
            Success = true,
            IsClosing = isClosing
        };
    }

    [McpServerTool(Name = "window_is_focused")]
    [Description("Check if the window currently has focus.")]
    public async Task<WindowFocusedResult> WindowIsFocused()
    {
        var bridge = connection.GetBridge();

        if (!bridge.Window.IsAvailable)
        {
            return new WindowFocusedResult
            {
                Success = false,
                Error = "Window state queries are not available (game may be running headless)"
            };
        }

        var isFocused = await bridge.Window.IsFocusedAsync();
        return new WindowFocusedResult
        {
            Success = true,
            IsFocused = isFocused
        };
    }

    [McpServerTool(Name = "window_get_aspect_ratio")]
    [Description("Get the current window aspect ratio (width / height).")]
    public async Task<WindowAspectRatioResult> WindowGetAspectRatio()
    {
        var bridge = connection.GetBridge();

        if (!bridge.Window.IsAvailable)
        {
            return new WindowAspectRatioResult
            {
                Success = false,
                Error = "Window state queries are not available (game may be running headless)"
            };
        }

        var aspectRatio = await bridge.Window.GetAspectRatioAsync();
        return new WindowAspectRatioResult
        {
            Success = true,
            AspectRatio = aspectRatio
        };
    }
}

/// <summary>
/// Result indicating if window queries are available.
/// </summary>
public sealed record WindowAvailableResult
{
    public required bool Available { get; init; }
}

/// <summary>
/// Result containing complete window state.
/// </summary>
public sealed record WindowStateResult
{
    public required bool Success { get; init; }
    public string? Error { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public string? Title { get; init; }
    public bool IsClosing { get; init; }
    public bool IsFocused { get; init; }
    public float AspectRatio { get; init; }
}

/// <summary>
/// Result containing window size.
/// </summary>
public sealed record WindowSizeResultMcp
{
    public required bool Success { get; init; }
    public string? Error { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
}

/// <summary>
/// Result containing window title.
/// </summary>
public sealed record WindowTitleResult
{
    public required bool Success { get; init; }
    public string? Error { get; init; }
    public string? Title { get; init; }
}

/// <summary>
/// Result indicating if window is closing.
/// </summary>
public sealed record WindowClosingResult
{
    public required bool Success { get; init; }
    public string? Error { get; init; }
    public bool IsClosing { get; init; }
}

/// <summary>
/// Result indicating if window is focused.
/// </summary>
public sealed record WindowFocusedResult
{
    public required bool Success { get; init; }
    public string? Error { get; init; }
    public bool IsFocused { get; init; }
}

/// <summary>
/// Result containing window aspect ratio.
/// </summary>
public sealed record WindowAspectRatioResult
{
    public required bool Success { get; init; }
    public string? Error { get; init; }
    public float AspectRatio { get; init; }
}
