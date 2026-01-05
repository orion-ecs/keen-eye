using KeenEyes.Navigation.Abstractions;

namespace KeenEyes.Navigation.Grid;

/// <summary>
/// Plugin that adds grid-based A* pathfinding to the ECS world.
/// </summary>
/// <remarks>
/// <para>
/// This plugin provides efficient grid-based navigation for 2D games or games
/// with tile-based movement. It registers a <see cref="GridNavigationProvider"/>
/// that implements <see cref="INavigationProvider"/>.
/// </para>
/// <para>
/// The plugin exposes the navigation provider as an extension that can be accessed
/// via <c>world.GetExtension&lt;GridNavigationProvider&gt;()</c> or
/// <c>world.GetExtension&lt;INavigationProvider&gt;()</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using var world = new World();
///
/// // Install the plugin with configuration
/// world.InstallPlugin(new GridNavigationPlugin(new GridConfig
/// {
///     Width = 100,
///     Height = 100,
///     CellSize = 1f,
///     AllowDiagonal = true
/// }));
///
/// // Get the navigation provider
/// var navigation = world.GetExtension&lt;GridNavigationProvider&gt;();
///
/// // Set up obstacles
/// navigation.Grid[10, 10] = GridCell.Blocked;
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
public sealed class GridNavigationPlugin : IWorldPlugin
{
    private readonly GridConfig config;
    private readonly NavigationGrid? existingGrid;
    private GridNavigationProvider? provider;

    /// <summary>
    /// Creates a new grid navigation plugin with default configuration.
    /// </summary>
    public GridNavigationPlugin()
        : this(GridConfig.Default)
    {
    }

    /// <summary>
    /// Creates a new grid navigation plugin with the specified configuration.
    /// </summary>
    /// <param name="config">The grid navigation configuration.</param>
    /// <exception cref="ArgumentException">Thrown if configuration is invalid.</exception>
    public GridNavigationPlugin(GridConfig config)
    {
        var error = config.Validate();
        if (error != null)
        {
            throw new ArgumentException($"Invalid GridConfig: {error}", nameof(config));
        }

        this.config = config;
        existingGrid = null;
    }

    /// <summary>
    /// Creates a new grid navigation plugin with an existing grid.
    /// </summary>
    /// <param name="grid">The pre-configured navigation grid to use.</param>
    /// <param name="config">The grid navigation configuration.</param>
    /// <exception cref="ArgumentNullException">Thrown if grid is null.</exception>
    /// <exception cref="ArgumentException">Thrown if configuration is invalid.</exception>
    public GridNavigationPlugin(NavigationGrid grid, GridConfig config)
    {
        ArgumentNullException.ThrowIfNull(grid);

        var error = config.Validate();
        if (error != null)
        {
            throw new ArgumentException($"Invalid GridConfig: {error}", nameof(config));
        }

        existingGrid = grid;
        this.config = config;
    }

    /// <inheritdoc/>
    public string Name => "GridNavigation";

    /// <inheritdoc/>
    public void Install(IPluginContext context)
    {
        // Create the navigation provider
        provider = existingGrid != null
            ? new GridNavigationProvider(existingGrid, config)
            : new GridNavigationProvider(config);

        // Register the provider as both the concrete type and the interface
        context.SetExtension(provider);
        context.SetExtension<INavigationProvider>(provider);
    }

    /// <inheritdoc/>
    public void Uninstall(IPluginContext context)
    {
        // Remove extensions
        context.RemoveExtension<GridNavigationProvider>();
        context.RemoveExtension<INavigationProvider>();

        // Dispose the provider
        provider?.Dispose();
        provider = null;
    }
}
