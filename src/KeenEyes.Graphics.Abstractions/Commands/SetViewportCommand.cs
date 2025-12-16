namespace KeenEyes.Graphics.Abstractions;

/// <summary>
/// Command to set the rendering viewport.
/// </summary>
/// <param name="X">The left edge of the viewport in pixels.</param>
/// <param name="Y">The bottom edge of the viewport in pixels.</param>
/// <param name="Width">The width of the viewport in pixels.</param>
/// <param name="Height">The height of the viewport in pixels.</param>
/// <remarks>
/// <para>
/// The viewport defines the rectangular region of the window where rendering occurs.
/// Coordinates are typically in pixels with (0,0) at the bottom-left corner.
/// </para>
/// <para>
/// Viewport commands have a low sort key to ensure they execute before draw commands.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Set full-window viewport
/// var viewport = new SetViewportCommand(0, 0, window.Width, window.Height);
///
/// // Set split-screen viewport (left half)
/// var leftViewport = new SetViewportCommand(0, 0, window.Width / 2, window.Height);
/// </code>
/// </example>
public readonly record struct SetViewportCommand(
    int X,
    int Y,
    int Width,
    int Height) : IRenderCommand
{
    /// <summary>
    /// Sort key for viewport commands. Viewport commands execute early (after clear).
    /// </summary>
    public ulong SortKey => 1;

    /// <summary>
    /// Creates a viewport command for the entire window.
    /// </summary>
    /// <param name="width">The window width in pixels.</param>
    /// <param name="height">The window height in pixels.</param>
    /// <returns>A viewport command covering the entire window.</returns>
    public static SetViewportCommand FullWindow(int width, int height) =>
        new(0, 0, width, height);
}
