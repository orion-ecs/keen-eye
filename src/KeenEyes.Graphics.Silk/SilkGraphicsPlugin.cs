using KeenEyes;
using KeenEyes.Platform.Silk;

namespace KeenEyes.Graphics.Silk;

/// <summary>
/// Plugin that provides Silk.NET OpenGL graphics capabilities to a KeenEyes world.
/// </summary>
/// <remarks>
/// <para>
/// This plugin requires SilkWindowPlugin from KeenEyes.Platform.Silk to be installed first.
/// It will throw an <see cref="InvalidOperationException"/> if the window plugin
/// is not available.
/// </para>
/// <para>
/// The plugin creates a <see cref="SilkGraphicsContext"/> extension that provides
/// access to rendering operations, resource creation, and the OpenGL device.
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
/// // Then install graphics plugin
/// world.InstallPlugin(new SilkGraphicsPlugin(new SilkGraphicsConfig
/// {
///     ClearColor = new Vector4(0.2f, 0.3f, 0.4f, 1f)
/// }));
///
/// // Access graphics context
/// var graphics = world.GetExtension&lt;SilkGraphicsContext&gt;();
/// </code>
/// </example>
/// <param name="config">The graphics configuration.</param>
public sealed class SilkGraphicsPlugin(SilkGraphicsConfig config) : IWorldPlugin
{
    private SilkGraphicsContext? graphicsContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="SilkGraphicsPlugin"/> class with default configuration.
    /// </summary>
    public SilkGraphicsPlugin()
        : this(new SilkGraphicsConfig())
    {
    }

    /// <inheritdoc />
    public string Name => "SilkGraphics";

    /// <inheritdoc />
    public void Install(IPluginContext context)
    {
        // Explicit dependency - fail loudly if window plugin not installed
        if (!context.World.TryGetExtension<ISilkWindowProvider>(out var windowProvider) || windowProvider is null)
        {
            throw new InvalidOperationException(
                $"{nameof(SilkGraphicsPlugin)} requires SilkWindowPlugin to be installed first. " +
                $"Install SilkWindowPlugin before installing {nameof(SilkGraphicsPlugin)}.");
        }

        // Create graphics context using the shared window
        graphicsContext = new SilkGraphicsContext(windowProvider, config);
        context.SetExtension(graphicsContext);

        // Register systems
        context.AddSystem<SilkCameraSystem>(SystemPhase.EarlyUpdate, order: 0);
        context.AddSystem<SilkRenderSystem>(SystemPhase.Render, order: 0);
    }

    /// <inheritdoc />
    public void Uninstall(IPluginContext context)
    {
        context.RemoveExtension<SilkGraphicsContext>();
        graphicsContext?.Dispose();
        graphicsContext = null;
    }
}
