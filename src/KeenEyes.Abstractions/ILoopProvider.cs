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
}
