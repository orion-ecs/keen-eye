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
/// public void Install(IPluginContext context)
/// {
///     // Register systems - these are tracked for automatic cleanup
///     context.AddSystem&lt;PhysicsSystem&gt;(SystemPhase.FixedUpdate);
///
///     // Expose a custom API through extensions
///     context.SetExtension(new PhysicsWorld());
///
///     // Access the world directly if needed (cast if concrete World is required)
///     context.World.SetSingleton(new PhysicsSettings { Gravity = -9.81f });
/// }
/// </code>
/// </example>
public sealed class PluginContext : IPluginContext
{
    private readonly List<ISystem> registeredSystems = [];

    /// <summary>
    /// Gets the world that the plugin is being installed into or uninstalled from.
    /// </summary>
    /// <remarks>
    /// This property returns the concrete <see cref="World"/> type for full access
    /// to all world operations.
    /// </remarks>
    public World World { get; }

    /// <inheritdoc />
    IWorld IPluginContext.World => World;

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

    /// <inheritdoc />
    public T AddSystem<T>(SystemPhase phase = SystemPhase.Update, int order = 0)
        where T : ISystem, new()
    {
        var system = new T();
        AddSystemInternal(system, phase, order, [], []);
        return system;
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public ISystem AddSystem(ISystem system, SystemPhase phase = SystemPhase.Update, int order = 0)
    {
        AddSystemInternal(system, phase, order, [], []);
        return system;
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public SystemGroup AddSystemGroup(SystemGroup group, SystemPhase phase = SystemPhase.Update, int order = 0)
    {
        AddSystemInternal(group, phase, order, [], []);
        return group;
    }

    /// <inheritdoc />
    public T GetExtension<T>() where T : class
    {
        return World.GetExtension<T>();
    }

    /// <inheritdoc />
    public bool TryGetExtension<T>(out T? extension) where T : class
    {
        var result = World.TryGetExtension<T>(out var ext);
        extension = ext;
        return result;
    }

    /// <inheritdoc />
    public void SetExtension<T>(T extension) where T : class
    {
        World.SetExtension(extension);
    }

    /// <inheritdoc />
    public bool RemoveExtension<T>() where T : class
    {
        return World.RemoveExtension<T>();
    }

    /// <inheritdoc />
    public void RegisterComponent<T>(bool isTag = false) where T : struct, IComponent
    {
        World.Components.GetOrRegister<T>(isTag);
    }

    /// <inheritdoc />
    public T GetCapability<T>() where T : class
    {
        if (TryGetCapability<T>(out var capability))
        {
            return capability!;
        }

        throw new InvalidOperationException(
            $"Capability of type {typeof(T).Name} is not available in this context.");
    }

    /// <inheritdoc />
    public bool TryGetCapability<T>(out T? capability) where T : class
    {
        // Check if World directly implements the capability
        if (World is T worldCapability)
        {
            capability = worldCapability;
            return true;
        }

        capability = null;
        return false;
    }

    /// <inheritdoc />
    public bool HasCapability<T>() where T : class
    {
        return World is T;
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
