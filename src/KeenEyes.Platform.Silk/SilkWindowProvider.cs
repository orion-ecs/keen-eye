using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace KeenEyes.Platform.Silk;

/// <summary>
/// Default implementation of <see cref="ISilkWindowProvider"/> using Silk.NET windowing.
/// </summary>
internal sealed class SilkWindowProvider : ISilkWindowProvider
{
    private readonly IWindow window;
    private IInputContext? inputContext;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SilkWindowProvider"/> class.
    /// </summary>
    /// <param name="config">The window configuration.</param>
    public SilkWindowProvider(WindowConfig config)
    {
        var options = WindowOptions.Default with
        {
            Title = config.Title,
            Size = new Vector2D<int>(config.Width, config.Height),
            VSync = config.VSync,
            WindowBorder = config.Resizable ? WindowBorder.Resizable : WindowBorder.Fixed,
            FramesPerSecond = config.TargetFramerate,
            UpdatesPerSecond = config.TargetUpdateFrequency,
        };

        window = global::Silk.NET.Windowing.Window.Create(options);

        // Subscribe to window events and forward to our events
        window.Load += HandleWindowLoad;
        window.Update += HandleWindowUpdate;
        window.Render += HandleWindowRender;
        window.Resize += HandleWindowResize;
        window.Closing += HandleWindowClosing;
    }

    /// <inheritdoc />
    public IWindow Window => window;

    /// <inheritdoc />
    public IInputContext InputContext
    {
        get
        {
            if (inputContext is null)
            {
                throw new InvalidOperationException(
                    "InputContext is not available until the window has loaded. " +
                    "Access this property after the OnLoad event has fired.");
            }

            return inputContext;
        }
    }

    /// <summary>
    /// Gets whether the input context is available.
    /// </summary>
    /// <remarks>
    /// The input context becomes available after the window loads.
    /// </remarks>
    public bool IsInputContextAvailable => inputContext is not null;

    /// <inheritdoc />
    public event Action? OnLoad;

    /// <inheritdoc />
    public event Action<double>? OnUpdate;

    /// <inheritdoc />
    public event Action<double>? OnRender;

    /// <inheritdoc />
    public event Action<int, int>? OnResize;

    /// <inheritdoc />
    public event Action? OnClosing;

    private void HandleWindowLoad()
    {
        inputContext = window.CreateInput();
        OnLoad?.Invoke();
    }

    private void HandleWindowUpdate(double deltaTime)
    {
        OnUpdate?.Invoke(deltaTime);
    }

    private void HandleWindowRender(double deltaTime)
    {
        OnRender?.Invoke(deltaTime);
    }

    private void HandleWindowResize(Vector2D<int> size)
    {
        OnResize?.Invoke(size.X, size.Y);
    }

    private void HandleWindowClosing()
    {
        OnClosing?.Invoke();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        // Unsubscribe from window events
        window.Load -= HandleWindowLoad;
        window.Update -= HandleWindowUpdate;
        window.Render -= HandleWindowRender;
        window.Resize -= HandleWindowResize;
        window.Closing -= HandleWindowClosing;

        inputContext?.Dispose();
        window.Dispose();
    }
}
