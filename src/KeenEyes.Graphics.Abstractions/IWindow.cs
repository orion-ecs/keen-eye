namespace KeenEyes.Graphics.Abstractions;

/// <summary>
/// Abstraction for a graphics window that manages the application lifecycle.
/// </summary>
/// <remarks>
/// <para>
/// This interface abstracts platform-specific windowing APIs (Silk.NET, SDL, GLFW, etc.)
/// and enables testing without a real window or GPU.
/// </para>
/// <para>
/// The window manages the main application loop and provides events for the key
/// lifecycle stages: initialization, updates, rendering, resizing, and cleanup.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var window = CreateWindow(config);
/// window.OnLoad += () => InitializeScene();
/// window.OnUpdate += deltaTime => UpdateSystems(deltaTime);
/// window.OnRender += deltaTime => RenderFrame(deltaTime);
/// window.OnResize += (w, h) => UpdateViewport(w, h);
/// window.Run();
/// </code>
/// </example>
public interface IWindow : IDisposable
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
    /// Gets the window title.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Gets whether the window is closing or has been closed.
    /// </summary>
    bool IsClosing { get; }

    /// <summary>
    /// Gets whether the window currently has input focus.
    /// </summary>
    bool IsFocused { get; }

    /// <summary>
    /// Gets the aspect ratio (width / height) of the window.
    /// </summary>
    float AspectRatio { get; }

    /// <summary>
    /// Event raised when the window has loaded and is ready for rendering.
    /// </summary>
    /// <remarks>
    /// This is the appropriate place to initialize graphics resources,
    /// load assets, and set up the initial scene.
    /// </remarks>
    event Action? OnLoad;

    /// <summary>
    /// Event raised when the window is resized.
    /// </summary>
    /// <remarks>
    /// Parameters are the new width and height in pixels.
    /// Use this to update viewport dimensions and projection matrices.
    /// </remarks>
    event Action<int, int>? OnResize;

    /// <summary>
    /// Event raised when the window is closing.
    /// </summary>
    /// <remarks>
    /// This is the appropriate place to save state, release resources,
    /// and perform cleanup before the window is destroyed.
    /// </remarks>
    event Action? OnClosing;

    /// <summary>
    /// Event raised each frame for game logic updates.
    /// </summary>
    /// <remarks>
    /// The parameter is the delta time in seconds since the last update.
    /// Use this for physics, AI, input processing, and other game logic.
    /// </remarks>
    event Action<double>? OnUpdate;

    /// <summary>
    /// Event raised each frame for rendering.
    /// </summary>
    /// <remarks>
    /// The parameter is the delta time in seconds since the last render.
    /// Use this for all drawing operations.
    /// </remarks>
    event Action<double>? OnRender;

    /// <summary>
    /// Creates the graphics device for this window.
    /// </summary>
    /// <returns>The graphics device instance.</returns>
    /// <remarks>
    /// The device should only be created after the window is initialized
    /// (typically in the <see cref="OnLoad"/> event handler).
    /// </remarks>
    IGraphicsDevice CreateDevice();

    /// <summary>
    /// Runs the main window loop. Blocks until the window is closed.
    /// </summary>
    /// <remarks>
    /// This method starts the event loop and will not return until
    /// <see cref="Close"/> is called or the window is closed by the user.
    /// </remarks>
    void Run();

    /// <summary>
    /// Processes pending window events without blocking.
    /// </summary>
    /// <remarks>
    /// Useful for integration with external event loops or
    /// when manual control over event processing is needed.
    /// </remarks>
    void DoEvents();

    /// <summary>
    /// Swaps the front and back buffers.
    /// </summary>
    /// <remarks>
    /// Call this at the end of each frame to present the rendered content.
    /// </remarks>
    void SwapBuffers();

    /// <summary>
    /// Requests the window to close.
    /// </summary>
    /// <remarks>
    /// This triggers the <see cref="OnClosing"/> event and exits the main loop.
    /// </remarks>
    void Close();
}
