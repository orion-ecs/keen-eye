namespace KeenEyes.TestBridge.Window;

/// <summary>
/// Controller for querying window state.
/// </summary>
/// <remarks>
/// <para>
/// The window controller provides methods for querying the state of the application
/// window, including dimensions, title, focus state, and more.
/// </para>
/// <para>
/// Window queries may be unavailable in headless mode or when no window provider
/// is registered. Check <see cref="IsAvailable"/> before making queries.
/// </para>
/// </remarks>
public interface IWindowController
{
    /// <summary>
    /// Gets whether window queries are available.
    /// </summary>
    /// <remarks>
    /// Window queries may be unavailable in headless mode or when
    /// no window provider is registered.
    /// </remarks>
    bool IsAvailable { get; }

    /// <summary>
    /// Gets all window state as a snapshot.
    /// </summary>
    /// <returns>A snapshot containing all window state.</returns>
    /// <remarks>
    /// Use this method when you need multiple properties at once to minimize
    /// IPC round-trips.
    /// </remarks>
    Task<WindowStateSnapshot> GetStateAsync();

    /// <summary>
    /// Gets the window dimensions in pixels.
    /// </summary>
    /// <returns>The window dimensions as (Width, Height).</returns>
    Task<(int Width, int Height)> GetSizeAsync();

    /// <summary>
    /// Gets the window title.
    /// </summary>
    /// <returns>The window title string.</returns>
    Task<string> GetTitleAsync();

    /// <summary>
    /// Gets whether the window is closing or has been closed.
    /// </summary>
    /// <returns>True if the window is closing; otherwise false.</returns>
    Task<bool> IsClosingAsync();

    /// <summary>
    /// Gets whether the window currently has input focus.
    /// </summary>
    /// <returns>True if the window has focus; otherwise false.</returns>
    Task<bool> IsFocusedAsync();

    /// <summary>
    /// Gets the aspect ratio (width / height) of the window.
    /// </summary>
    /// <returns>The aspect ratio as a floating-point value.</returns>
    Task<float> GetAspectRatioAsync();
}
