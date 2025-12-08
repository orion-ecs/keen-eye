namespace KeenEyes.Graphics.Backend;

/// <summary>
/// Abstraction for a graphics window that can create a graphics device.
/// </summary>
/// <remarks>
/// This interface abstracts platform-specific windowing APIs (Silk.NET, SDL, etc.)
/// and enables testing without a real window or GPU.
/// </remarks>
public interface IGraphicsWindow : IDisposable
{
    /// <summary>
    /// Gets the current window width in pixels.
    /// </summary>
    int Width { get; }

    /// <summary>
    /// Gets the current window height in pixels.
    /// </summary>
    int Height { get; }

    /// <summary>
    /// Gets whether the window is closing or has been closed.
    /// </summary>
    bool IsClosing { get; }

    /// <summary>
    /// Event raised when the window has loaded and is ready for rendering.
    /// </summary>
    event Action? OnLoad;

    /// <summary>
    /// Event raised when the window is resized.
    /// </summary>
    event Action<int, int>? OnResize;

    /// <summary>
    /// Event raised when the window is closing.
    /// </summary>
    event Action? OnClosing;

    /// <summary>
    /// Event raised each frame for updates.
    /// </summary>
    event Action<double>? OnUpdate;

    /// <summary>
    /// Event raised each frame for rendering.
    /// </summary>
    event Action<double>? OnRender;

    /// <summary>
    /// Creates the graphics device for this window.
    /// </summary>
    /// <returns>The graphics device.</returns>
    IGraphicsDevice CreateDevice();

    /// <summary>
    /// Runs the main window loop. Blocks until the window is closed.
    /// </summary>
    void Run();

    /// <summary>
    /// Processes pending window events without blocking.
    /// </summary>
    void DoEvents();

    /// <summary>
    /// Swaps the front and back buffers.
    /// </summary>
    void SwapBuffers();

    /// <summary>
    /// Closes the window.
    /// </summary>
    void Close();
}
