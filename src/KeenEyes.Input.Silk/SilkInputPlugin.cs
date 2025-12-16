using KeenEyes.Platform.Silk;

namespace KeenEyes.Input.Silk;

/// <summary>
/// Plugin that provides Silk.NET input handling capabilities to a KeenEyes world.
/// </summary>
/// <remarks>
/// <para>
/// This plugin requires SilkWindowPlugin from KeenEyes.Platform.Silk to be installed first.
/// It will throw an <see cref="InvalidOperationException"/> if the window plugin
/// is not available.
/// </para>
/// <para>
/// The plugin creates a <see cref="SilkInputContext"/> extension that provides
/// access to keyboard, mouse, and gamepad input.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Install window plugin first (required)
/// world.InstallPlugin(new SilkWindowPlugin(new WindowConfig
/// {
///     Title = "My Game",
///     Width = 1920,
///     Height = 1080
/// }));
///
/// // Then install input plugin
/// world.InstallPlugin(new SilkInputPlugin(new SilkInputConfig
/// {
///     EnableGamepads = true,
///     GamepadDeadzone = 0.2f
/// }));
///
/// // Access input context
/// var input = world.GetExtension&lt;SilkInputContext&gt;();
///
/// // Polling-based input
/// if (input.Keyboard.IsKeyDown(Key.W))
///     MoveForward();
///
/// // Event-based input
/// input.Keyboard.OnKeyDown += args =&gt; { /* handle key */ };
/// </code>
/// </example>
/// <param name="config">The input configuration.</param>
public sealed class SilkInputPlugin(SilkInputConfig config) : IWorldPlugin
{
    private SilkInputContext? inputContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="SilkInputPlugin"/> class with default configuration.
    /// </summary>
    public SilkInputPlugin()
        : this(new SilkInputConfig())
    {
    }

    /// <inheritdoc />
    public string Name => "SilkInput";

    /// <inheritdoc />
    public void Install(IPluginContext context)
    {
        // Explicit dependency - fail loudly if window plugin not installed
        if (!context.World.TryGetExtension<ISilkWindowProvider>(out var windowProvider) || windowProvider is null)
        {
            throw new InvalidOperationException(
                $"{nameof(SilkInputPlugin)} requires SilkWindowPlugin to be installed first. " +
                $"Install SilkWindowPlugin before installing {nameof(SilkInputPlugin)}.");
        }

        // Create input context using the shared window's input context
        inputContext = new SilkInputContext(windowProvider, config);
        context.SetExtension(inputContext);

        // Register input capture system (runs early to capture input before other systems)
        context.AddSystem<SilkInputCaptureSystem>(SystemPhase.EarlyUpdate, order: -1000);
    }

    /// <inheritdoc />
    public void Uninstall(IPluginContext context)
    {
        context.RemoveExtension<SilkInputContext>();
        inputContext?.Dispose();
        inputContext = null;
    }
}
