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
        window.Load += OnWindowLoad;
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
                    "Access this property after the window's Load event has fired.");
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

    private void OnWindowLoad()
    {
        inputContext = window.CreateInput();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        inputContext?.Dispose();
        window.Dispose();
    }
}
