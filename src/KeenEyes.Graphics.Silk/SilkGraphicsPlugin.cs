using KeenEyes;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graphics.Silk;

/// <summary>
/// Plugin that provides Silk.NET OpenGL graphics capabilities to a KeenEyes world.
/// </summary>
/// <remarks>
/// <para>
/// This plugin creates a window and OpenGL context, providing a complete graphics
/// solution through the <see cref="IGraphicsContext"/> interface.
/// </para>
/// <para>
/// The plugin only registers the graphics context extension. Graphics systems
/// (such as CameraSystem and RenderSystem from KeenEyes.Graphics) must be added
/// explicitly by the user to allow customization of system order and selection.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Step 1: Install the rendering backend
/// var config = new SilkGraphicsConfig
/// {
///     WindowTitle = "My Game",
///     WindowWidth = 1920,
///     WindowHeight = 1080,
///     ClearColor = new Vector4(0.2f, 0.3f, 0.4f, 1f)
/// };
/// world.InstallPlugin(new SilkGraphicsPlugin(config));
///
/// // Step 2: Add graphics systems (from KeenEyes.Graphics)
/// world.AddSystem&lt;CameraSystem&gt;(SystemPhase.EarlyUpdate, order: 0);
/// world.AddSystem&lt;RenderSystem&gt;(SystemPhase.Render, order: 0);
///
/// // Get the graphics context (backend-agnostic interface)
/// var graphics = world.GetExtension&lt;IGraphicsContext&gt;();
///
/// // Set up event handlers
/// graphics.OnLoad += () =&gt; CreateScene();
/// graphics.OnUpdate += deltaTime =&gt; world.Update((float)deltaTime);
///
/// // Run the graphics loop
/// graphics.Initialize();
/// graphics.Run();
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
        // Create graphics context
        graphicsContext = new SilkGraphicsContext(config);

        // Set extension as the interface type for backend-agnostic access
        context.SetExtension<IGraphicsContext>(graphicsContext);

        // Register as ILoopProvider for WorldRunnerBuilder
        context.SetExtension<ILoopProvider>(graphicsContext);

        // Also set as concrete type for advanced usage
        context.SetExtension(graphicsContext);

        // NOTE: Systems are NOT registered by the plugin.
        // Users should add systems explicitly from KeenEyes.Graphics:
        //   world.AddSystem<CameraSystem>(SystemPhase.EarlyUpdate, order: 0);
        //   world.AddSystem<RenderSystem>(SystemPhase.Render, order: 0);
    }

    /// <inheritdoc />
    public void Uninstall(IPluginContext context)
    {
        context.RemoveExtension<IGraphicsContext>();
        context.RemoveExtension<ILoopProvider>();
        context.RemoveExtension<SilkGraphicsContext>();
        graphicsContext?.Dispose();
        graphicsContext = null;
    }
}
