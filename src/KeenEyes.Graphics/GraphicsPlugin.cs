namespace KeenEyes.Graphics;

/// <summary>
/// Plugin that adds graphics and rendering capabilities to a KeenEyes world.
/// </summary>
/// <remarks>
/// <para>
/// The GraphicsPlugin integrates Silk.NET for OpenGL rendering with the KeenEyes ECS.
/// It provides components for 3D rendering (Transform3D, Renderable, Material, Camera, Light),
/// resource management (meshes, textures, shaders), and a complete render pipeline.
/// </para>
/// <para>
/// Based on research recommendations, this plugin uses:
/// <list type="bullet">
///   <item><description>Silk.NET for OpenGL/Vulkan bindings (.NET Foundation backed)</description></item>
///   <item><description>Silk.NET.Windowing for cross-platform windowing</description></item>
///   <item><description>System.Numerics for SIMD-accelerated math</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Install the graphics plugin
/// var config = new GraphicsConfig
/// {
///     WindowWidth = 1920,
///     WindowHeight = 1080,
///     WindowTitle = "My Game",
///     VSync = true
/// };
///
/// using var world = new World();
/// world.InstallPlugin(new GraphicsPlugin(config));
///
/// var graphics = world.GetExtension&lt;GraphicsContext&gt;();
/// graphics.Initialize();
///
/// // Create a cube mesh
/// var cubeMesh = graphics.CreateCube();
///
/// // Create a camera
/// world.Spawn()
///     .With(new Transform3D(new Vector3(0, 5, 10), Quaternion.Identity, Vector3.One))
///     .With(Camera.CreatePerspective(60f, 16f/9f, 0.1f, 1000f))
///     .WithTag&lt;MainCameraTag&gt;()
///     .Build();
///
/// // Create a renderable entity
/// world.Spawn()
///     .With(Transform3D.Identity)
///     .With(new Renderable(cubeMesh, 0))
///     .With(Material.Default)
///     .Build();
///
/// // Main loop
/// while (!graphics.ShouldClose)
/// {
///     graphics.ProcessEvents();
///     world.Update(0.016f);
///     graphics.SwapBuffers();
/// }
/// </code>
/// </example>
public sealed class GraphicsPlugin : IWorldPlugin
{
    private readonly GraphicsConfig config;
    private GraphicsContext? graphics;

    /// <summary>
    /// Gets the name of this plugin.
    /// </summary>
    public string Name => "Graphics";

    /// <summary>
    /// Creates a new graphics plugin with default configuration.
    /// </summary>
    public GraphicsPlugin() : this(new GraphicsConfig())
    {
    }

    /// <summary>
    /// Creates a new graphics plugin with the specified configuration.
    /// </summary>
    /// <param name="config">The graphics configuration.</param>
    public GraphicsPlugin(GraphicsConfig config)
    {
        this.config = config;
    }

    /// <inheritdoc />
    public void Install(IPluginContext context)
    {
        // Create and register the graphics context
        var world = (World)context.World;
        graphics = new GraphicsContext(world, config);
        context.SetExtension(graphics);

        // Register systems
        context.AddSystem<CameraSystem>(SystemPhase.EarlyUpdate, order: 0);
        context.AddSystem<RenderSystem>(SystemPhase.Render, order: 0);
    }

    /// <inheritdoc />
    public void Uninstall(IPluginContext context)
    {
        // Remove extension
        context.RemoveExtension<GraphicsContext>();

        // Dispose graphics resources
        graphics?.Dispose();
        graphics = null;
    }
}
