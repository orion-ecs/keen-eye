namespace KeenEyes;

/// <summary>
/// Provides context for plugin installation and uninstallation operations.
/// </summary>
/// <remarks>
/// <para>
/// The plugin context is passed to <see cref="IWorldPlugin.Install"/> and
/// <see cref="IWorldPlugin.Uninstall"/> methods, providing access to the world
/// and APIs for registering systems and extensions.
/// </para>
/// <para>
/// Systems registered through the context are tracked and automatically cleaned
/// up when the plugin is uninstalled. Extensions set through the context are
/// stored in the world and can be retrieved by other code.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public void Install(PluginContext context)
/// {
///     // Register systems - these are tracked for automatic cleanup
///     context.AddSystem&lt;PhysicsSystem&gt;(SystemPhase.FixedUpdate);
///
///     // Expose a custom API through extensions
///     context.SetExtension(new PhysicsWorld(context.World));
///
///     // Access the world directly if needed
///     context.World.SetSingleton(new PhysicsSettings { Gravity = -9.81f });
/// }
/// </code>
/// </example>
public sealed class PluginContext
{
    private readonly List<ISystem> registeredSystems = [];

    /// <summary>
    /// Gets the world that the plugin is being installed into or uninstalled from.
    /// </summary>
    public World World { get; }

    /// <summary>
    /// Gets the plugin that this context is for.
    /// </summary>
    public IWorldPlugin Plugin { get; }

    /// <summary>
    /// Gets the systems registered by the plugin through this context.
    /// </summary>
    internal IReadOnlyList<ISystem> RegisteredSystems => registeredSystems;

    /// <summary>
    /// Creates a new plugin context for the specified world and plugin.
    /// </summary>
    /// <param name="world">The world for plugin operations.</param>
    /// <param name="plugin">The plugin this context is for.</param>
    internal PluginContext(World world, IWorldPlugin plugin)
    {
        World = world;
        Plugin = plugin;
    }

    /// <summary>
    /// Registers a system with the world at the specified phase and order.
    /// </summary>
    /// <typeparam name="T">The system type to register.</typeparam>
    /// <param name="phase">The execution phase for this system. Defaults to <see cref="SystemPhase.Update"/>.</param>
    /// <param name="order">The execution order within the phase. Lower values execute first. Defaults to 0.</param>
    /// <returns>The created system instance.</returns>
    /// <remarks>
    /// <para>
    /// Systems registered through this method are tracked and will be automatically
    /// removed and disposed when the plugin is uninstalled.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// context.AddSystem&lt;PhysicsSystem&gt;(SystemPhase.FixedUpdate, order: 0);
    /// context.AddSystem&lt;CollisionSystem&gt;(SystemPhase.FixedUpdate, order: 10);
    /// </code>
    /// </example>
    public T AddSystem<T>(SystemPhase phase = SystemPhase.Update, int order = 0)
        where T : ISystem, new()
    {
        var system = new T();
        AddSystemInternal(system, phase, order, [], []);
        return system;
    }

    /// <summary>
    /// Registers a system with the world at the specified phase, order, and dependency constraints.
    /// </summary>
    /// <typeparam name="T">The system type to register.</typeparam>
    /// <param name="phase">The execution phase for this system.</param>
    /// <param name="order">The execution order within the phase. Lower values execute first.</param>
    /// <param name="runsBefore">Types of systems that this system must run before.</param>
    /// <param name="runsAfter">Types of systems that this system must run after.</param>
    /// <returns>The created system instance.</returns>
    /// <remarks>
    /// <para>
    /// Systems registered through this method are tracked and will be automatically
    /// removed and disposed when the plugin is uninstalled.
    /// </para>
    /// </remarks>
    public T AddSystem<T>(
        SystemPhase phase,
        int order,
        Type[] runsBefore,
        Type[] runsAfter)
        where T : ISystem, new()
    {
        var system = new T();
        AddSystemInternal(system, phase, order, runsBefore, runsAfter);
        return system;
    }

    /// <summary>
    /// Registers a system instance with the world at the specified phase and order.
    /// </summary>
    /// <param name="system">The system instance to register.</param>
    /// <param name="phase">The execution phase for this system. Defaults to <see cref="SystemPhase.Update"/>.</param>
    /// <param name="order">The execution order within the phase. Lower values execute first. Defaults to 0.</param>
    /// <returns>The system instance for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Use this overload when you need to pass a pre-configured system instance or a system
    /// that requires constructor parameters.
    /// </para>
    /// <para>
    /// Systems registered through this method are tracked and will be automatically
    /// removed and disposed when the plugin is uninstalled.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var physics = new PhysicsSystem(gravity: -9.81f);
    /// context.AddSystem(physics, SystemPhase.FixedUpdate);
    /// </code>
    /// </example>
    public ISystem AddSystem(ISystem system, SystemPhase phase = SystemPhase.Update, int order = 0)
    {
        AddSystemInternal(system, phase, order, [], []);
        return system;
    }

    /// <summary>
    /// Registers a system instance with the world at the specified phase, order, and dependency constraints.
    /// </summary>
    /// <param name="system">The system instance to register.</param>
    /// <param name="phase">The execution phase for this system.</param>
    /// <param name="order">The execution order within the phase. Lower values execute first.</param>
    /// <param name="runsBefore">Types of systems that this system must run before.</param>
    /// <param name="runsAfter">Types of systems that this system must run after.</param>
    /// <returns>The system instance for chaining.</returns>
    public ISystem AddSystem(
        ISystem system,
        SystemPhase phase,
        int order,
        Type[] runsBefore,
        Type[] runsAfter)
    {
        AddSystemInternal(system, phase, order, runsBefore, runsAfter);
        return system;
    }

    /// <summary>
    /// Registers a system group with the world at the specified phase and order.
    /// </summary>
    /// <param name="group">The system group to register.</param>
    /// <param name="phase">The execution phase for this group. Defaults to <see cref="SystemPhase.Update"/>.</param>
    /// <param name="order">The execution order within the phase. Lower values execute first. Defaults to 0.</param>
    /// <returns>The system group for chaining.</returns>
    /// <remarks>
    /// <para>
    /// System groups allow organizing multiple systems that execute together.
    /// The group and all its contained systems are tracked for automatic cleanup.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var physicsGroup = new SystemGroup("Physics")
    ///     .Add&lt;BroadphaseSystem&gt;(order: 0)
    ///     .Add&lt;NarrowphaseSystem&gt;(order: 10);
    /// context.AddSystemGroup(physicsGroup, SystemPhase.FixedUpdate);
    /// </code>
    /// </example>
    public SystemGroup AddSystemGroup(SystemGroup group, SystemPhase phase = SystemPhase.Update, int order = 0)
    {
        AddSystemInternal(group, phase, order, [], []);
        return group;
    }

    /// <summary>
    /// Sets an extension value that can be retrieved by other code.
    /// </summary>
    /// <typeparam name="T">The extension type.</typeparam>
    /// <param name="extension">The extension instance to store.</param>
    /// <remarks>
    /// <para>
    /// Extensions allow plugins to expose custom APIs to application code.
    /// For example, a physics plugin might expose a <c>PhysicsWorld</c> extension
    /// that provides raycast and collision query methods.
    /// </para>
    /// <para>
    /// Extensions are stored in the world and can be retrieved using
    /// <see cref="World.GetExtension{T}"/> or <see cref="World.TryGetExtension{T}"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In plugin Install method:
    /// context.SetExtension(new PhysicsWorld(context.World));
    ///
    /// // In application code:
    /// var physics = world.GetExtension&lt;PhysicsWorld&gt;();
    /// var hit = physics.Raycast(origin, direction);
    /// </code>
    /// </example>
    public void SetExtension<T>(T extension) where T : class
    {
        World.SetExtension(extension);
    }

    /// <summary>
    /// Removes an extension from the world.
    /// </summary>
    /// <typeparam name="T">The extension type to remove.</typeparam>
    /// <returns>True if the extension was found and removed; false otherwise.</returns>
    /// <remarks>
    /// <para>
    /// Call this during <see cref="IWorldPlugin.Uninstall"/> if your extension
    /// requires explicit cleanup. Extensions are automatically removed when
    /// <see cref="World.RemoveExtension{T}"/> is called.
    /// </para>
    /// </remarks>
    public bool RemoveExtension<T>() where T : class
    {
        return World.RemoveExtension<T>();
    }

    /// <summary>
    /// Registers a system internally and tracks it for cleanup.
    /// </summary>
    private void AddSystemInternal(
        ISystem system,
        SystemPhase phase,
        int order,
        Type[] runsBefore,
        Type[] runsAfter)
    {
        World.AddSystem(system, phase, order, runsBefore, runsAfter);
        registeredSystems.Add(system);
    }
}
