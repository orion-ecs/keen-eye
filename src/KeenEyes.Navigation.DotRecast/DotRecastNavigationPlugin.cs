using KeenEyes.Navigation.Abstractions;

namespace KeenEyes.Navigation.DotRecast;

/// <summary>
/// Plugin that adds DotRecast 3D navmesh pathfinding to the ECS world.
/// </summary>
/// <remarks>
/// <para>
/// This plugin provides industry-standard navigation mesh pathfinding using DotRecast,
/// a C# port of Recast/Detour. It registers a <see cref="DotRecastProvider"/>
/// that implements <see cref="INavigationProvider"/>.
/// </para>
/// <para>
/// The plugin exposes the navigation provider as an extension that can be accessed
/// via <c>world.GetExtension&lt;DotRecastProvider&gt;()</c> or
/// <c>world.GetExtension&lt;INavigationProvider&gt;()</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using var world = new World();
///
/// // Build a navmesh from geometry
/// var builder = new DotRecastMeshBuilder(new NavMeshConfig
/// {
///     CellSize = 0.3f,
///     AgentRadius = 0.5f,
///     AgentHeight = 2.0f
/// });
///
/// var navMeshData = builder.Build(vertices, indices);
///
/// // Install the plugin with the navmesh
/// world.InstallPlugin(new DotRecastNavigationPlugin(navMeshData));
///
/// // Get the navigation provider
/// var navigation = world.GetExtension&lt;INavigationProvider&gt;();
///
/// // Find a path
/// var path = navigation.FindPath(
///     new Vector3(0, 0, 0),
///     new Vector3(50, 0, 50),
///     AgentSettings.Default);
///
/// if (path.IsValid)
/// {
///     foreach (var waypoint in path)
///     {
///         // Follow the path
///     }
/// }
/// </code>
/// </example>
public sealed class DotRecastNavigationPlugin : IWorldPlugin
{
    private readonly NavMeshConfig config;
    private readonly NavMeshData? prebuiltMesh;
    private DotRecastProvider? provider;
    private NavMeshObstacleManager? obstacleManager;

    /// <summary>
    /// Creates a new DotRecast navigation plugin with default configuration.
    /// </summary>
    /// <remarks>
    /// A navmesh must be set using <see cref="DotRecastProvider.SetNavMesh"/> before
    /// pathfinding will work.
    /// </remarks>
    public DotRecastNavigationPlugin()
        : this(NavMeshConfig.Default)
    {
    }

    /// <summary>
    /// Creates a new DotRecast navigation plugin with the specified configuration.
    /// </summary>
    /// <param name="config">The navmesh build and query configuration.</param>
    /// <exception cref="ArgumentNullException">Thrown if config is null.</exception>
    /// <exception cref="ArgumentException">Thrown if configuration is invalid.</exception>
    public DotRecastNavigationPlugin(NavMeshConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var error = config.Validate();
        if (error != null)
        {
            throw new ArgumentException($"Invalid NavMeshConfig: {error}", nameof(config));
        }

        this.config = config;
        prebuiltMesh = null;
    }

    /// <summary>
    /// Creates a new DotRecast navigation plugin with a pre-built navmesh.
    /// </summary>
    /// <param name="navMeshData">The pre-built navigation mesh to use.</param>
    /// <exception cref="ArgumentNullException">Thrown if navMeshData is null.</exception>
    public DotRecastNavigationPlugin(NavMeshData navMeshData)
        : this(navMeshData, NavMeshConfig.Default)
    {
    }

    /// <summary>
    /// Creates a new DotRecast navigation plugin with a pre-built navmesh and configuration.
    /// </summary>
    /// <param name="navMeshData">The pre-built navigation mesh to use.</param>
    /// <param name="config">The query configuration.</param>
    /// <exception cref="ArgumentNullException">Thrown if navMeshData or config is null.</exception>
    /// <exception cref="ArgumentException">Thrown if configuration is invalid.</exception>
    public DotRecastNavigationPlugin(NavMeshData navMeshData, NavMeshConfig config)
    {
        ArgumentNullException.ThrowIfNull(navMeshData);
        ArgumentNullException.ThrowIfNull(config);

        var error = config.Validate();
        if (error != null)
        {
            throw new ArgumentException($"Invalid NavMeshConfig: {error}", nameof(config));
        }

        prebuiltMesh = navMeshData;
        this.config = config;
    }

    /// <inheritdoc/>
    public string Name => "DotRecastNavigation";

    /// <inheritdoc/>
    public void Install(IPluginContext context)
    {
        // Create the navigation provider
        provider = prebuiltMesh != null
            ? new DotRecastProvider(prebuiltMesh, config)
            : new DotRecastProvider(config);

        // Create obstacle manager for dynamic carving
        obstacleManager = new NavMeshObstacleManager();

        // Register the provider as both the concrete type and the interface
        context.SetExtension(provider);
        context.SetExtension<INavigationProvider>(provider);

        // Register the obstacle manager
        context.SetExtension(obstacleManager);

        // Register the mesh builder for runtime navmesh generation
        context.SetExtension(new DotRecastMeshBuilder(config));
    }

    /// <inheritdoc/>
    public void Uninstall(IPluginContext context)
    {
        // Remove extensions
        context.RemoveExtension<DotRecastProvider>();
        context.RemoveExtension<INavigationProvider>();
        context.RemoveExtension<NavMeshObstacleManager>();
        context.RemoveExtension<DotRecastMeshBuilder>();

        // Dispose the provider
        provider?.Dispose();
        provider = null;

        // Clear obstacle manager
        obstacleManager?.Clear();
        obstacleManager = null;
    }
}
