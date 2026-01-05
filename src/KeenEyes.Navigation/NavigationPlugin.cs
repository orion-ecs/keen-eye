using KeenEyes.Navigation.Abstractions;
using KeenEyes.Navigation.Abstractions.Components;
using KeenEyes.Navigation.Systems;

namespace KeenEyes.Navigation;

/// <summary>
/// Plugin that adds navigation and pathfinding capabilities to the ECS world.
/// </summary>
/// <remarks>
/// <para>
/// This plugin provides comprehensive navigation support including:
/// </para>
/// <list type="bullet">
/// <item><description>Asynchronous pathfinding via <see cref="PathRequestSystem"/></description></item>
/// <item><description>Agent movement along paths via <see cref="NavMeshAgentSystem"/></description></item>
/// <item><description>Dynamic obstacle handling via <see cref="ObstacleUpdateSystem"/></description></item>
/// </list>
/// <para>
/// The plugin exposes a <see cref="NavigationContext"/> extension that can be accessed
/// via <c>world.GetExtension&lt;NavigationContext&gt;()</c> or <c>world.Navigation</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using var world = new World();
///
/// // Install with default configuration
/// world.InstallPlugin(new NavigationPlugin());
///
/// // Or with custom configuration
/// world.InstallPlugin(new NavigationPlugin(new NavigationConfig
/// {
///     Strategy = NavigationStrategy.Grid,
///     MaxPathRequestsPerFrame = 10
/// }));
///
/// // Create a navigating entity
/// var agent = world.Spawn()
///     .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
///     .With(NavMeshAgent.Create())
///     .Build();
///
/// // Set destination
/// var nav = world.GetExtension&lt;NavigationContext&gt;();
/// nav.SetDestination(agent, new Vector3(10, 0, 10));
/// </code>
/// </example>
public sealed class NavigationPlugin : IWorldPlugin
{
    private readonly NavigationConfig config;
    private NavigationContext? navigationContext;
    private INavigationProvider? provider;
    private EventSubscription? agentAddedSubscription;
    private EventSubscription? agentRemovedSubscription;
    private EventSubscription? obstacleAddedSubscription;
    private EventSubscription? obstacleRemovedSubscription;
    private EventSubscription? entityDestroyedSubscription;

    /// <summary>
    /// Creates a new navigation plugin with default configuration.
    /// </summary>
    public NavigationPlugin()
        : this(NavigationConfig.Default)
    {
    }

    /// <summary>
    /// Creates a new navigation plugin with the specified configuration.
    /// </summary>
    /// <param name="config">The navigation configuration.</param>
    /// <exception cref="ArgumentException">Thrown if configuration is invalid.</exception>
    public NavigationPlugin(NavigationConfig config)
    {
        var error = config.Validate();
        if (error != null)
        {
            throw new ArgumentException($"Invalid NavigationConfig: {error}", nameof(config));
        }

        this.config = config;
    }

    /// <inheritdoc/>
    public string Name => "Navigation";

    /// <inheritdoc/>
    public void Install(IPluginContext context)
    {
        // Register components
        context.RegisterComponent<NavMeshAgent>();
        context.RegisterComponent<NavMeshObstacle>();

        // Get or create navigation provider
        provider = GetNavigationProvider(context);

        // Create and expose the navigation context API
        navigationContext = new NavigationContext(context.World, config);
        navigationContext.SetProvider(provider);
        context.SetExtension(navigationContext);

        // Also register the INavigationProvider for direct access
        context.SetExtension(provider);

        // Register systems
        RegisterSystems(context);

        // Subscribe to component lifecycle events
        SubscribeToEvents(context);
    }

    /// <inheritdoc/>
    public void Uninstall(IPluginContext context)
    {
        // Unsubscribe from events
        agentAddedSubscription?.Dispose();
        agentRemovedSubscription?.Dispose();
        obstacleAddedSubscription?.Dispose();
        obstacleRemovedSubscription?.Dispose();
        entityDestroyedSubscription?.Dispose();

        agentAddedSubscription = null;
        agentRemovedSubscription = null;
        obstacleAddedSubscription = null;
        obstacleRemovedSubscription = null;
        entityDestroyedSubscription = null;

        // Remove extensions
        context.RemoveExtension<NavigationContext>();
        context.RemoveExtension<INavigationProvider>();

        // Dispose the context
        navigationContext?.Dispose();
        navigationContext = null;

        // Dispose provider only if we own it (not custom provided)
        if (config.Strategy != NavigationStrategy.Custom)
        {
            provider?.Dispose();
        }

        provider = null;

        // Systems are automatically cleaned up by PluginManager
    }

    private INavigationProvider GetNavigationProvider(IPluginContext context)
    {
        // If custom provider is specified, use it
        if (config.Strategy == NavigationStrategy.Custom && config.CustomProvider != null)
        {
            return config.CustomProvider;
        }

        // Check if a provider is already registered (e.g., GridNavigationPlugin was installed first)
        if (context.TryGetExtension<INavigationProvider>(out var existingProvider) && existingProvider != null)
        {
            return existingProvider;
        }

        // For now, throw if no provider is available
        // In a full implementation, we could create a default provider based on strategy
        throw new InvalidOperationException(
            $"No navigation provider available for strategy {config.Strategy}. " +
            "Install a navigation provider plugin (e.g., GridNavigationPlugin) first, " +
            "or provide a custom provider via NavigationConfig.CustomProvider.");
    }

    private void RegisterSystems(IPluginContext context)
    {
        // PathRequestSystem - processes async path requests
        // Runs early in Update phase to process completed requests before agent movement
        context.AddSystem<PathRequestSystem>(
            SystemPhase.Update,
            order: -100);

        // NavMeshAgentSystem - moves agents along their paths
        // Runs in Update phase after path requests are processed
        if (config.AgentSteeringEnabled)
        {
            context.AddSystem<NavMeshAgentSystem>(
                SystemPhase.Update,
                order: 0);
        }

        // ObstacleUpdateSystem - syncs dynamic obstacles
        // Runs in LateUpdate phase after all movement is complete
        if (config.DynamicObstaclesEnabled)
        {
            context.AddSystem<ObstacleUpdateSystem>(
                SystemPhase.LateUpdate,
                order: 0);
        }
    }

    private void SubscribeToEvents(IPluginContext context)
    {
        // Subscribe to NavMeshAgent component added/removed
        agentAddedSubscription = context.World.OnComponentAdded<NavMeshAgent>(OnAgentAdded);
        agentRemovedSubscription = context.World.OnComponentRemoved<NavMeshAgent>(OnAgentRemoved);

        // Subscribe to NavMeshObstacle component added/removed
        obstacleAddedSubscription = context.World.OnComponentAdded<NavMeshObstacle>(OnObstacleAdded);
        obstacleRemovedSubscription = context.World.OnComponentRemoved<NavMeshObstacle>(OnObstacleRemoved);

        // Subscribe to entity destroyed for cleanup
        entityDestroyedSubscription = context.World.OnEntityDestroyed(OnEntityDestroyed);
    }

    private void OnAgentAdded(Entity entity, NavMeshAgent agent)
    {
        // Agent added - nothing to do initially
        // Path will be requested when SetDestination is called
    }

    private void OnAgentRemoved(Entity entity)
    {
        // Clean up navigation state
        navigationContext?.RemoveAgent(entity);
    }

    private void OnObstacleAdded(Entity entity, NavMeshObstacle obstacle)
    {
        // Obstacle added - obstacle system will handle updates
        // Could mark navigation data as dirty here
    }

    private void OnObstacleRemoved(Entity entity)
    {
        // Obstacle removed - obstacle system will handle updates
    }

    private void OnEntityDestroyed(Entity entity)
    {
        // Clean up any navigation state for destroyed entity
        navigationContext?.RemoveAgent(entity);
    }
}
