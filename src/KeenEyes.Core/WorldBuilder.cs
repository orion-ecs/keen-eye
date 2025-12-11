namespace KeenEyes;

/// <summary>
/// Fluent builder for configuring and creating independent <see cref="World"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// WorldBuilder enables creating multiple worlds with the same configuration.
/// Each call to <see cref="Build"/> creates a completely independent world instance
/// with its own component registry, entity pool, and system instances.
/// </para>
/// <para>
/// This is useful for:
/// <list type="bullet">
/// <item>Client-server simulations (separate worlds for client and server)</item>
/// <item>Test isolation (each test gets a fresh world)</item>
/// <item>Multi-scene games (separate worlds for menu, gameplay, etc.)</item>
/// </list>
/// </para>
/// <para>
/// The builder pattern allows for fluent chaining of configuration options,
/// making the setup code readable and self-documenting.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create a reusable builder
/// var builder = new WorldBuilder()
///     .WithSystem&lt;MovementSystem&gt;(SystemPhase.Update)
///     .WithSystem&lt;PhysicsSystem&gt;(SystemPhase.FixedUpdate);
///
/// // Create independent worlds
/// var clientWorld = builder.Build();
/// clientWorld.Name = "Client";
///
/// var serverWorld = builder.Build();
/// serverWorld.Name = "Server";
///
/// // Completely isolated - different component IDs, entity IDs, system instances
/// </code>
/// </example>
public sealed class WorldBuilder
{
    private readonly List<PluginRegistration> plugins = [];
    private readonly List<SystemRegistration> systems = [];

    /// <summary>
    /// Adds a plugin to be installed when the world is built.
    /// </summary>
    /// <typeparam name="T">The plugin type to install.</typeparam>
    /// <returns>This builder for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// Plugins are installed in the order they are added. This allows plugins
    /// to depend on other plugins that were added earlier.
    /// </para>
    /// <para>
    /// A new plugin instance is created for each call to <see cref="Build"/>,
    /// allowing the builder to be reused for multiple worlds.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var world = new WorldBuilder()
    ///     .WithPlugin&lt;CorePlugin&gt;()
    ///     .WithPlugin&lt;PhysicsPlugin&gt;()
    ///     .Build();
    /// </code>
    /// </example>
    public WorldBuilder WithPlugin<T>() where T : IWorldPlugin, new()
    {
        // Capture factory delegate at registration time for AOT compatibility (no Activator.CreateInstance)
        plugins.Add(new PluginRegistration(static () => new T(), null));
        return this;
    }

    /// <summary>
    /// Adds a plugin instance to be installed when the world is built.
    /// </summary>
    /// <param name="plugin">The plugin instance to install.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// Use this overload when you need to pass a pre-configured plugin instance.
    /// </para>
    /// <para>
    /// Note: When using a plugin instance, the same instance will be used
    /// if <see cref="Build"/> is called multiple times. For independent worlds,
    /// use the generic <see cref="WithPlugin{T}"/> method or create new
    /// WorldBuilder instances.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var physics = new PhysicsPlugin(gravity: -9.81f);
    /// var world = new WorldBuilder()
    ///     .WithPlugin(physics)
    ///     .Build();
    /// </code>
    /// </example>
    public WorldBuilder WithPlugin(IWorldPlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        plugins.Add(new PluginRegistration(null, Instance: plugin));
        return this;
    }

    /// <summary>
    /// Adds a system to be registered when the world is built.
    /// </summary>
    /// <typeparam name="T">The system type to register.</typeparam>
    /// <param name="phase">The execution phase for this system. Defaults to <see cref="SystemPhase.Update"/>.</param>
    /// <param name="order">The execution order within the phase. Lower values execute first. Defaults to 0.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// Systems added through the builder are registered after all plugins are installed.
    /// This allows systems to depend on plugin-provided functionality.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var world = new WorldBuilder()
    ///     .WithPlugin&lt;PhysicsPlugin&gt;()
    ///     .WithSystem&lt;MovementSystem&gt;(SystemPhase.Update, order: 0)
    ///     .WithSystem&lt;RenderSystem&gt;(SystemPhase.Render)
    ///     .Build();
    /// </code>
    /// </example>
    public WorldBuilder WithSystem<T>(SystemPhase phase = SystemPhase.Update, int order = 0)
        where T : ISystem, new()
    {
        // Capture factory delegate at registration time for AOT compatibility (no Activator.CreateInstance)
        systems.Add(new SystemRegistration(static () => new T(), null, phase, order, [], []));
        return this;
    }

    /// <summary>
    /// Adds a system to be registered when the world is built with dependency constraints.
    /// </summary>
    /// <typeparam name="T">The system type to register.</typeparam>
    /// <param name="phase">The execution phase for this system.</param>
    /// <param name="order">The execution order within the phase. Lower values execute first.</param>
    /// <param name="runsBefore">Types of systems that this system must run before.</param>
    /// <param name="runsAfter">Types of systems that this system must run after.</param>
    /// <returns>This builder for method chaining.</returns>
    public WorldBuilder WithSystem<T>(
        SystemPhase phase,
        int order,
        Type[] runsBefore,
        Type[] runsAfter)
        where T : ISystem, new()
    {
        // Capture factory delegate at registration time for AOT compatibility (no Activator.CreateInstance)
        systems.Add(new SystemRegistration(static () => new T(), null, phase, order, runsBefore, runsAfter));
        return this;
    }

    /// <summary>
    /// Adds a system instance to be registered when the world is built.
    /// </summary>
    /// <param name="system">The system instance to register.</param>
    /// <param name="phase">The execution phase for this system. Defaults to <see cref="SystemPhase.Update"/>.</param>
    /// <param name="order">The execution order within the phase. Lower values execute first. Defaults to 0.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// Use this overload when you need to pass a pre-configured system instance.
    /// </para>
    /// </remarks>
    public WorldBuilder WithSystem(ISystem system, SystemPhase phase = SystemPhase.Update, int order = 0)
    {
        ArgumentNullException.ThrowIfNull(system);
        systems.Add(new SystemRegistration(null, Instance: system, phase, order, [], []));
        return this;
    }

    /// <summary>
    /// Adds a system instance to be registered when the world is built with dependency constraints.
    /// </summary>
    /// <param name="system">The system instance to register.</param>
    /// <param name="phase">The execution phase for this system.</param>
    /// <param name="order">The execution order within the phase. Lower values execute first.</param>
    /// <param name="runsBefore">Types of systems that this system must run before.</param>
    /// <param name="runsAfter">Types of systems that this system must run after.</param>
    /// <returns>This builder for method chaining.</returns>
    public WorldBuilder WithSystem(
        ISystem system,
        SystemPhase phase,
        int order,
        Type[] runsBefore,
        Type[] runsAfter)
    {
        ArgumentNullException.ThrowIfNull(system);
        systems.Add(new SystemRegistration(null, Instance: system, phase, order, runsBefore, runsAfter));
        return this;
    }

    /// <summary>
    /// Adds a system group to be registered when the world is built.
    /// </summary>
    /// <param name="group">The system group to register.</param>
    /// <param name="phase">The execution phase for this group. Defaults to <see cref="SystemPhase.Update"/>.</param>
    /// <param name="order">The execution order within the phase. Lower values execute first. Defaults to 0.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <example>
    /// <code>
    /// var gameplayGroup = new SystemGroup("Gameplay")
    ///     .Add&lt;InputSystem&gt;(order: 0)
    ///     .Add&lt;MovementSystem&gt;(order: 10);
    ///
    /// var world = new WorldBuilder()
    ///     .WithSystemGroup(gameplayGroup, SystemPhase.Update)
    ///     .Build();
    /// </code>
    /// </example>
    public WorldBuilder WithSystemGroup(SystemGroup group, SystemPhase phase = SystemPhase.Update, int order = 0)
    {
        ArgumentNullException.ThrowIfNull(group);
        systems.Add(new SystemRegistration(null, Instance: group, phase, order, [], []));
        return this;
    }

    /// <summary>
    /// Builds and returns a new configured <see cref="World"/> instance.
    /// </summary>
    /// <returns>A new world with all configured plugins and systems.</returns>
    /// <remarks>
    /// <para>
    /// The build process:
    /// <list type="number">
    /// <item>Creates a new World instance</item>
    /// <item>Installs all plugins in the order they were added</item>
    /// <item>Registers all systems in the order they were added</item>
    /// </list>
    /// </para>
    /// <para>
    /// After calling Build(), the builder can be reused to create additional
    /// world instances with the same configuration.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var world = new WorldBuilder()
    ///     .WithPlugin&lt;PhysicsPlugin&gt;()
    ///     .WithSystem&lt;GameplaySystem&gt;()
    ///     .Build();
    ///
    /// // Use the world
    /// world.Update(deltaTime);
    /// </code>
    /// </example>
    public World Build()
    {
        var world = new World();

        // Install all plugins first
        foreach (var registration in plugins)
        {
            var plugin = registration.Instance ?? registration.Factory!();
            world.InstallPlugin(plugin);
        }

        // Register all systems
        foreach (var registration in systems)
        {
            var system = registration.Instance ?? registration.Factory!();
            world.AddSystem(
                system,
                registration.Phase,
                registration.Order,
                registration.RunsBefore,
                registration.RunsAfter);
        }

        return world;
    }

    /// <summary>
    /// Internal record for tracking plugin registrations.
    /// </summary>
    private sealed record PluginRegistration(Func<IWorldPlugin>? Factory, IWorldPlugin? Instance);

    /// <summary>
    /// Internal record for tracking system registrations.
    /// </summary>
    private sealed record SystemRegistration(
        Func<ISystem>? Factory,
        ISystem? Instance,
        SystemPhase Phase,
        int Order,
        Type[] RunsBefore,
        Type[] RunsAfter);
}
