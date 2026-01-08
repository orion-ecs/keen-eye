using System.Collections.Concurrent;
using Silk.NET.Windowing;

namespace KeenEyes.Platform.Silk;

/// <summary>
/// Provides the main application loop by wrapping Silk.NET window events.
/// </summary>
/// <remarks>
/// <para>
/// This class implements <see cref="ILoopProvider"/> by forwarding events from
/// <see cref="ISilkWindowProvider"/>. It converts the window's double-precision
/// delta times to single-precision for consistency with the ECS world.
/// </para>
/// <para>
/// The <see cref="SilkWindowPlugin"/> registers this as the <see cref="ILoopProvider"/>
/// extension, allowing <c>WorldRunnerBuilder</c> to use it for the main game loop.
/// </para>
/// </remarks>
[PluginExtension("SilkWindow")]
public sealed class SilkLoopProvider : ILoopProvider
{
    private readonly ISilkWindowProvider windowProvider;
    private readonly ConcurrentQueue<Action> renderThreadQueue = new();
    private bool initialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="SilkLoopProvider"/> class.
    /// </summary>
    /// <param name="windowProvider">The window provider to wrap.</param>
    internal SilkLoopProvider(ISilkWindowProvider windowProvider)
    {
        this.windowProvider = windowProvider;

        // Subscribe to window events and forward them with type conversion
        windowProvider.OnLoad += HandleLoad;
        windowProvider.OnUpdate += HandleUpdate;
        windowProvider.OnRender += HandleRender;
        windowProvider.OnResize += HandleResize;
        windowProvider.OnClosing += HandleClosing;
    }

    /// <inheritdoc />
    public event Action? OnReady;

    /// <inheritdoc />
    public event Action<float>? OnUpdate;

    /// <inheritdoc />
    public event Action<float>? OnRender;

    /// <inheritdoc />
    public event Action<int, int>? OnResize;

    /// <inheritdoc />
    public event Action? OnClosing;

    /// <inheritdoc />
    public bool IsInitialized => initialized;

    /// <inheritdoc />
    public Task<T> InvokeOnRenderThreadAsync<T>(Func<T> action)
    {
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        renderThreadQueue.Enqueue(() =>
        {
            try
            {
                var result = action();
                tcs.SetResult(result);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        return tcs.Task;
    }

    /// <inheritdoc />
    public void InvokeOnRenderThread(Action action)
    {
        renderThreadQueue.Enqueue(action);
    }

    /// <inheritdoc />
    public void Initialize()
    {
        // Window is already created by SilkWindowProvider.
        // This method exists for interface compatibility.
        // The actual initialization happens when Run() is called and the window loads.
    }

    /// <inheritdoc />
    public void Run()
    {
        // Initialize the window first - this creates the GL context and fires Load events.
        // All subscribers to windowProvider.OnLoad (like SilkGraphicsContext) will run here.
        windowProvider.Window.Initialize();

        // Now that all Load handlers have completed, fire OnReady.
        // This ensures graphics context is available when the user's OnReady callback runs.
        initialized = true;
        OnReady?.Invoke();

        // Start the main loop - Update/Render events fire from here.
        windowProvider.Window.Run();
    }

    private void HandleLoad()
    {
        // Initialization flag and OnReady are handled in Run() after Initialize() returns.
        // This ensures all OnLoad handlers (including graphics) complete first.
    }

    private void HandleUpdate(double deltaTime)
    {
        OnUpdate?.Invoke((float)deltaTime);
    }

    private void HandleRender(double deltaTime)
    {
        // Process any pending render thread work
        while (renderThreadQueue.TryDequeue(out var action))
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                // Log but don't crash - individual queue items handle their own errors
                System.Diagnostics.Debug.WriteLine($"Render thread action failed: {ex.Message}");
            }
        }

        OnRender?.Invoke((float)deltaTime);
    }

    private void HandleResize(int width, int height)
    {
        OnResize?.Invoke(width, height);
    }

    private void HandleClosing()
    {
        OnClosing?.Invoke();
    }
}
