namespace KeenEyes.TestBridge.Window;

/// <summary>
/// Immutable snapshot of window state.
/// </summary>
/// <remarks>
/// This record captures all window state in a single object, enabling efficient
/// transfer over IPC with a single round-trip.
/// </remarks>
public sealed record WindowStateSnapshot
{
    /// <summary>
    /// Gets the window width in pixels.
    /// </summary>
    public required int Width { get; init; }

    /// <summary>
    /// Gets the window height in pixels.
    /// </summary>
    public required int Height { get; init; }

    /// <summary>
    /// Gets the window title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets whether the window is closing or has been closed.
    /// </summary>
    public required bool IsClosing { get; init; }

    /// <summary>
    /// Gets whether the window currently has input focus.
    /// </summary>
    public required bool IsFocused { get; init; }

    /// <summary>
    /// Gets the aspect ratio (width / height) of the window.
    /// </summary>
    public required float AspectRatio { get; init; }
}
