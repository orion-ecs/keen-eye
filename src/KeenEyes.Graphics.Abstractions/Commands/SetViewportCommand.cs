namespace KeenEyes.Graphics.Abstractions;

/// <summary>
/// Command to set the rendering viewport.
/// </summary>
/// <param name="X">The left edge of the viewport in device pixels.</param>
/// <param name="Y">The bottom edge of the viewport in device pixels.</param>
/// <param name="Width">The width of the viewport in device pixels.</param>
/// <param name="Height">The height of the viewport in device pixels.</param>
/// <remarks>
/// <para>
/// The viewport defines the rectangular region of the window where rendering occurs.
/// Coordinates are in device pixels - not logical points - with (0,0) at the bottom-left
/// corner. On a HiDPI display the two differ, so size a full-window viewport from
/// <see cref="IGraphicsContext.FramebufferWidth"/>, never from the window's logical size.
/// </para>
/// <para>
/// Viewport commands have a low sort key to ensure they execute before draw commands.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Set full-window viewport
/// var viewport = new SetViewportCommand(0, 0, graphics.FramebufferWidth, graphics.FramebufferHeight);
///
/// // Set split-screen viewport (left half)
/// var leftViewport = new SetViewportCommand(0, 0, graphics.FramebufferWidth / 2, graphics.FramebufferHeight);
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
    /// <param name="width">The framebuffer width in device pixels.</param>
    /// <param name="height">The framebuffer height in device pixels.</param>
    /// <returns>A viewport command covering the entire window.</returns>
    public static SetViewportCommand FullWindow(int width, int height) =>
        new(0, 0, width, height);
}
