using KeenEyes.Spatial.Partitioning;
using KeenEyes.Spatial.Systems;

namespace KeenEyes.Spatial;

/// <summary>
/// Plugin that adds spatial partitioning capabilities to the ECS world.
/// </summary>
/// <remarks>
/// <para>
/// This plugin provides efficient spatial queries for collision detection, proximity
/// searches, rendering culling, and AI perception systems. It automatically maintains
/// a spatial index of entities with the <see cref="KeenEyes.Spatial.SpatialIndexed"/> tag component.
/// </para>
/// <para>
/// The plugin exposes a <see cref="SpatialQueryApi"/> extension that can be accessed
/// via <c>world.GetExtension&lt;SpatialQueryApi&gt;()</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using var world = new World();
///
/// // Install the plugin with default configuration
/// world.InstallPlugin(new SpatialPlugin());
///
/// // Or with custom configuration
/// world.InstallPlugin(new SpatialPlugin(new SpatialConfig
/// {
///     Strategy = SpatialStrategy.Grid,
///     Grid = new GridConfig
///     {
///         CellSize = 50f,
///         WorldMin = new Vector3(-1000, -1000, -1000),
///         WorldMax = new Vector3(1000, 1000, 1000)
///     }
/// }));
///
/// // Create spatially indexed entities
/// var entity = world.Spawn()
///     .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
///     .WithTag&lt;SpatialIndexed&gt;()
///     .Build();
///
/// // Query nearby entities
/// var spatial = world.GetExtension&lt;SpatialQueryApi&gt;();
/// foreach (var nearby in spatial.QueryRadius(playerPos, 100f))
/// {
///     // Process nearby entities
/// }
/// </code>
/// </example>
public sealed class SpatialPlugin : IWorldPlugin
{
    private readonly SpatialConfig config;
    private SpatialQueryApi? spatialApi;

    /// <summary>
    /// Creates a new spatial plugin with default configuration.
    /// </summary>
    public SpatialPlugin()
        : this(new SpatialConfig())
    {
    }

    /// <summary>
    /// Creates a new spatial plugin with the specified configuration.
    /// </summary>
    /// <param name="config">The spatial partitioning configuration.</param>
    /// <exception cref="ArgumentException">Thrown if configuration is invalid.</exception>
    public SpatialPlugin(SpatialConfig config)
    {
        var error = config.Validate();
        if (error != null)
        {
            throw new ArgumentException($"Invalid SpatialConfig: {error}", nameof(config));
        }

        this.config = config;
    }

    /// <inheritdoc/>
    public string Name => "Spatial";

    /// <inheritdoc/>
    public void Install(IPluginContext context)
    {
        // Register the SpatialIndexed component type so it can be used dynamically
        context.RegisterComponent<SpatialIndexed>(isTag: true);

        // Create the appropriate spatial partitioner based on strategy
        ISpatialPartitioner partitioner = config.Strategy switch
        {
            SpatialStrategy.Grid => new GridPartitioner(config.Grid),
            SpatialStrategy.Quadtree => throw new NotImplementedException(
                "Quadtree strategy coming in Phase 2"),
            SpatialStrategy.Octree => throw new NotImplementedException(
                "Octree strategy coming in Phase 2"),
            _ => throw new ArgumentException($"Unknown spatial strategy: {config.Strategy}")
        };

        // Create and expose the query API
        spatialApi = new SpatialQueryApi(context.World, partitioner);
        context.SetExtension(spatialApi);

        // Register the update system (runs in LateUpdate after movement)
        var updateSystem = new SpatialUpdateSystem();
        context.AddSystem(
            updateSystem,
            SystemPhase.LateUpdate,
            order: -100, // Run early in LateUpdate phase
            runsBefore: Array.Empty<Type>(),
            runsAfter: Array.Empty<Type>());
    }

    /// <inheritdoc/>
    public void Uninstall(IPluginContext context)
    {
        // Remove the extension
        context.RemoveExtension<SpatialQueryApi>();

        // Dispose the API (which disposes the partitioner)
        spatialApi?.Dispose();
        spatialApi = null;

        // Systems are automatically cleaned up by PluginManager
    }
}
