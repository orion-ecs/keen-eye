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
    public void Initialize()
    {
        // Window is already created by SilkWindowProvider.
        // This method exists for interface compatibility.
        // The actual initialization happens when Run() is called and the window loads.
    }

    /// <inheritdoc />
    public void Run()
    {
        // Silk.NET IView.Run() requires an onFrame callback.
        // We use an empty callback since we're using window events instead.
        windowProvider.Window.Run(() => { });
    }

    private void HandleLoad()
    {
        initialized = true;
        OnReady?.Invoke();
    }

    private void HandleUpdate(double deltaTime)
    {
        OnUpdate?.Invoke((float)deltaTime);
    }

    private void HandleRender(double deltaTime)
    {
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
