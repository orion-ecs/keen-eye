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
        // Initialize the window before running the main loop.
        // This creates the native window and OpenGL context.
        // The window's Load event fires synchronously during Initialize(),
        // which triggers windowProvider.OnLoad for plugins to initialize.
        windowProvider.Window.Initialize();

        // Fire OnReady AFTER window initialization is complete.
        // This ensures all plugins (graphics, input) have initialized from OnLoad
        // before user code runs in OnReady.
        initialized = true;
        OnReady?.Invoke();

        // Track frame timing for delta time calculation
        var lastTime = DateTime.UtcNow;

        // Silk.NET IView.Run() requires an onFrame callback.
        // When using Run(Action), Update/Render events do NOT fire automatically.
        // We need to manually calculate delta time and fire our events.
        windowProvider.Window.Run(() =>
        {
            var currentTime = DateTime.UtcNow;
            var deltaTime = (currentTime - lastTime).TotalSeconds;
            lastTime = currentTime;

            // Fire update event
            OnUpdate?.Invoke((float)deltaTime);

            // Fire render event
            OnRender?.Invoke((float)deltaTime);
        });
    }

    private void HandleLoad()
    {
        // Don't fire OnReady here - we fire it in Run() after Initialize() completes.
        // This ensures all plugin OnLoad handlers have run before OnReady fires.
    }

    private void HandleUpdate(double deltaTime)
    {
        // Not used when using Run(Action) - we fire OnUpdate directly in the callback
        OnUpdate?.Invoke((float)deltaTime);
    }

    private void HandleRender(double deltaTime)
    {
        // Not used when using Run(Action) - we fire OnRender directly in the callback
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
