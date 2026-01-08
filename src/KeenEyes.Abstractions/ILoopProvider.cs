namespace KeenEyes;

/// <summary>
/// Interface for plugins that provide a main loop (window, game loop, etc.).
/// </summary>
/// <remarks>
/// <para>
/// This interface enables backend-agnostic main loop configuration. Plugins that
/// create windows or game loops implement this interface to provide a standardized
/// way to hook into the application lifecycle.
/// </para>
/// <para>
/// The WorldRunnerBuilder (from KeenEyes.Runtime) uses this interface to wire
/// up the world's update loop without coupling to specific backends like Silk.NET or SDL.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Plugins implement ILoopProvider to provide main loops
/// public class MyGraphicsPlugin : IWorldPlugin
/// {
///     public void Install(IPluginContext context)
///     {
///         var loopProvider = new MyLoopProvider();
///         context.SetExtension&lt;ILoopProvider&gt;(loopProvider);
///     }
/// }
/// </code>
/// </example>
public interface ILoopProvider
{
    /// <summary>
    /// Raised once when the loop is ready (e.g., window created, context available).
    /// </summary>
    event Action? OnReady;

    /// <summary>
    /// Raised each frame for update logic.
    /// </summary>
    event Action<float>? OnUpdate;

    /// <summary>
    /// Raised each frame for rendering.
    /// </summary>
    event Action<float>? OnRender;

    /// <summary>
    /// Raised when the window or viewport is resized.
    /// </summary>
    event Action<int, int>? OnResize;

    /// <summary>
    /// Raised when the loop is closing.
    /// </summary>
    event Action? OnClosing;

    /// <summary>
    /// Initializes the loop provider (creates window, etc.).
    /// </summary>
    void Initialize();

    /// <summary>
    /// Runs the main loop. Blocks until closed.
    /// </summary>
    void Run();

    /// <summary>
    /// Gets whether the loop provider is initialized.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Queues an action to run on the render thread and waits for completion.
    /// </summary>
    /// <typeparam name="T">The return type of the action.</typeparam>
    /// <param name="action">The action to execute on the render thread.</param>
    /// <returns>A task that completes with the result when the action has executed.</returns>
    /// <remarks>
    /// Use this method when you need to perform operations that require the render thread's
    /// OpenGL context (e.g., reading framebuffers, creating textures). The action will be
    /// executed during the next render frame.
    /// </remarks>
    Task<T> InvokeOnRenderThreadAsync<T>(Func<T> action);

    /// <summary>
    /// Queues an action to run on the render thread (fire-and-forget).
    /// </summary>
    /// <param name="action">The action to execute on the render thread.</param>
    /// <remarks>
    /// Use this for render thread operations where you don't need to wait for completion.
    /// </remarks>
    void InvokeOnRenderThread(Action action);
}
