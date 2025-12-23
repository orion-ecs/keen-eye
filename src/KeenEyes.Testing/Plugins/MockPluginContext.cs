namespace KeenEyes.Testing.Plugins;

/// <summary>
/// Records information about a system that was registered.
/// </summary>
/// <param name="SystemType">The type of the registered system.</param>
/// <param name="Phase">The phase the system was registered for.</param>
/// <param name="Order">The execution order within the phase.</param>
/// <param name="Instance">The system instance if one was provided.</param>
public readonly record struct RegisteredSystemInfo(
    Type SystemType,
    SystemPhase Phase,
    int Order,
    ISystem? Instance = null);

/// <summary>
/// Records information about an extension that was set.
/// </summary>
/// <param name="ExtensionType">The type of the extension.</param>
/// <param name="Instance">The extension instance.</param>
public readonly record struct RegisteredExtensionInfo(Type ExtensionType, object Instance);

/// <summary>
/// Records information about a component type that was registered.
/// </summary>
/// <param name="ComponentType">The type of the component.</param>
/// <param name="IsTag">Whether the component is a tag component.</param>
public readonly record struct RegisteredComponentInfo(Type ComponentType, bool IsTag);

/// <summary>
/// A mock plugin context that tracks all registrations for verification in tests.
/// </summary>
/// <remarks>
/// <para>
/// MockPluginContext implements <see cref="IPluginContext"/> and records all operations
/// performed by plugins during installation. This allows tests to verify that plugins
/// register the expected systems, extensions, and components.
/// </para>
/// <para>
/// Unlike a real plugin context, MockPluginContext does not actually register systems
/// with a world. It only records the registration attempts for later verification.
/// </para>
/// <para>
/// Mock capabilities can be registered via <see cref="SetCapability{T}"/> to provide
/// test implementations of capability interfaces that plugins may request.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var mockContext = new MockPluginContext(mockPlugin);
/// myPlugin.Install(mockContext);
///
/// // Verify systems were registered
/// Assert.True(mockContext.WasSystemRegistered&lt;PhysicsSystem&gt;());
///
/// // Verify extensions were set
/// Assert.True(mockContext.WasExtensionSet&lt;PhysicsWorld&gt;());
///
/// // Or use fluent assertions
/// mockContext
///     .ShouldHaveRegisteredSystem&lt;PhysicsSystem&gt;()
///     .ShouldHaveSetExtension&lt;PhysicsWorld&gt;();
///
/// // With mock capabilities
/// var mockHooks = new MockSystemHookCapability();
/// mockContext.SetCapability&lt;ISystemHookCapability&gt;(mockHooks);
/// plugin.Install(mockContext);
/// Assert.True(mockHooks.WasHookAdded);
/// </code>
/// </example>
public sealed class MockPluginContext : IPluginContext
{
    private readonly List<RegisteredSystemInfo> registeredSystems = [];
    private readonly List<RegisteredExtensionInfo> registeredExtensions = [];
    private readonly List<RegisteredComponentInfo> registeredComponents = [];
    private readonly Dictionary<Type, object> extensions = [];
    private readonly Dictionary<Type, object> capabilities = [];
    private readonly IWorld? world;

    /// <summary>
    /// Creates a new mock plugin context.
    /// </summary>
    /// <param name="plugin">The plugin this context is for.</param>
    /// <param name="world">Optional world reference. If null, World property throws.</param>
    public MockPluginContext(IWorldPlugin plugin, IWorld? world = null)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        Plugin = plugin;
        this.world = world;
    }

    /// <summary>
    /// Gets all systems that were registered through this context.
    /// </summary>
    public IReadOnlyList<RegisteredSystemInfo> RegisteredSystems => registeredSystems;

    /// <summary>
    /// Gets all extensions that were set through this context.
    /// </summary>
    public IReadOnlyList<RegisteredExtensionInfo> RegisteredExtensions => registeredExtensions;

    /// <summary>
    /// Gets all component types that were registered through this context.
    /// </summary>
    public IReadOnlyList<RegisteredComponentInfo> RegisteredComponents => registeredComponents;

    /// <summary>
    /// Checks if a system of the specified type was registered.
    /// </summary>
    /// <typeparam name="T">The system type to check.</typeparam>
    /// <returns>True if a system of this type was registered; otherwise, false.</returns>
    public bool WasSystemRegistered<T>() where T : ISystem
    {
        return registeredSystems.Any(s => s.SystemType == typeof(T));
    }

    /// <summary>
    /// Checks if a system of the specified type was registered at the given phase.
    /// </summary>
    /// <typeparam name="T">The system type to check.</typeparam>
    /// <param name="phase">The expected phase.</param>
    /// <returns>True if the system was registered at the specified phase; otherwise, false.</returns>
    public bool WasSystemRegisteredAtPhase<T>(SystemPhase phase) where T : ISystem
    {
        return registeredSystems.Any(s => s.SystemType == typeof(T) && s.Phase == phase);
    }

    /// <summary>
    /// Gets information about a registered system.
    /// </summary>
    /// <typeparam name="T">The system type.</typeparam>
    /// <returns>The registration info, or null if not registered.</returns>
    public RegisteredSystemInfo? GetSystemRegistration<T>() where T : ISystem
    {
        var match = registeredSystems.FirstOrDefault(s => s.SystemType == typeof(T));
        // FirstOrDefault on structs returns default, not null. Check SystemType to determine if found.
        return match.SystemType == typeof(T) ? match : null;
    }

    /// <summary>
    /// Checks if an extension of the specified type was set.
    /// </summary>
    /// <typeparam name="T">The extension type to check.</typeparam>
    /// <returns>True if an extension of this type was set; otherwise, false.</returns>
    public bool WasExtensionSet<T>() where T : class
    {
        return registeredExtensions.Any(e => e.ExtensionType == typeof(T));
    }

    /// <summary>
    /// Gets the extension instance that was set for the specified type.
    /// </summary>
    /// <typeparam name="T">The extension type.</typeparam>
    /// <returns>The extension instance, or null if not set.</returns>
    public T? GetSetExtension<T>() where T : class
    {
        var info = registeredExtensions.FirstOrDefault(e => e.ExtensionType == typeof(T));
        return info.Instance as T;
    }

    /// <summary>
    /// Checks if a component of the specified type was registered.
    /// </summary>
    /// <typeparam name="T">The component type to check.</typeparam>
    /// <returns>True if a component of this type was registered; otherwise, false.</returns>
    public bool WasComponentRegistered<T>() where T : struct, IComponent
    {
        return registeredComponents.Any(c => c.ComponentType == typeof(T));
    }

    /// <summary>
    /// Resets all tracked registrations.
    /// </summary>
    public void Reset()
    {
        registeredSystems.Clear();
        registeredExtensions.Clear();
        registeredComponents.Clear();
        extensions.Clear();
        capabilities.Clear();
    }

    /// <summary>
    /// Sets a mock capability that can be requested by plugins.
    /// </summary>
    /// <typeparam name="T">The capability interface type.</typeparam>
    /// <param name="capability">The mock capability implementation.</param>
    /// <returns>This context for fluent chaining.</returns>
    /// <remarks>
    /// <para>
    /// Use this method to provide mock implementations of capability interfaces
    /// that plugins may request via <see cref="GetCapability{T}"/> or
    /// <see cref="TryGetCapability{T}"/>.
    /// </para>
    /// </remarks>
    public MockPluginContext SetCapability<T>(T capability) where T : class
    {
        ArgumentNullException.ThrowIfNull(capability);
        capabilities[typeof(T)] = capability;
        return this;
    }

    /// <summary>
    /// Gets a mock capability that was set.
    /// </summary>
    /// <typeparam name="T">The capability interface type.</typeparam>
    /// <returns>The mock capability, or null if not set.</returns>
    public T? GetSetCapability<T>() where T : class
    {
        if (capabilities.TryGetValue(typeof(T), out var cap))
        {
            return (T)cap;
        }

        return null;
    }

    /// <summary>
    /// Checks if a capability was set.
    /// </summary>
    /// <typeparam name="T">The capability interface type.</typeparam>
    /// <returns>True if the capability was set; false otherwise.</returns>
    public bool WasCapabilitySet<T>() where T : class
    {
        return capabilities.ContainsKey(typeof(T));
    }

    #region IPluginContext Implementation

    /// <inheritdoc />
    public IWorld World => world ?? throw new InvalidOperationException(
        "No world was provided to MockPluginContext. Provide a world in the constructor if World access is needed.");

    /// <inheritdoc />
    public IWorldPlugin Plugin { get; }

    /// <inheritdoc />
    public T AddSystem<T>(SystemPhase phase = SystemPhase.Update, int order = 0)
        where T : ISystem, new()
    {
        var system = new T();
        registeredSystems.Add(new RegisteredSystemInfo(typeof(T), phase, order, system));
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
        registeredSystems.Add(new RegisteredSystemInfo(typeof(T), phase, order, system));
        return system;
    }

    /// <inheritdoc />
    public ISystem AddSystem(ISystem system, SystemPhase phase = SystemPhase.Update, int order = 0)
    {
        ArgumentNullException.ThrowIfNull(system);
        registeredSystems.Add(new RegisteredSystemInfo(system.GetType(), phase, order, system));
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
        ArgumentNullException.ThrowIfNull(system);
        registeredSystems.Add(new RegisteredSystemInfo(system.GetType(), phase, order, system));
        return system;
    }

    /// <inheritdoc />
    public SystemGroup AddSystemGroup(SystemGroup group, SystemPhase phase = SystemPhase.Update, int order = 0)
    {
        ArgumentNullException.ThrowIfNull(group);
        registeredSystems.Add(new RegisteredSystemInfo(group.GetType(), phase, order, group));
        return group;
    }

    /// <inheritdoc />
    public T GetExtension<T>() where T : class
    {
        if (extensions.TryGetValue(typeof(T), out var extension))
        {
            return (T)extension;
        }

        throw new InvalidOperationException($"Extension of type {typeof(T).Name} is not registered.");
    }

    /// <inheritdoc />
    public bool TryGetExtension<T>(out T? extension) where T : class
    {
        if (extensions.TryGetValue(typeof(T), out var ext))
        {
            extension = (T)ext;
            return true;
        }

        extension = null;
        return false;
    }

    /// <inheritdoc />
    public void SetExtension<T>(T extension) where T : class
    {
        ArgumentNullException.ThrowIfNull(extension);
        extensions[typeof(T)] = extension;
        registeredExtensions.Add(new RegisteredExtensionInfo(typeof(T), extension));
    }

    /// <inheritdoc />
    public bool RemoveExtension<T>() where T : class
    {
        return extensions.Remove(typeof(T));
    }

    /// <inheritdoc />
    public void RegisterComponent<T>(bool isTag = false) where T : struct, IComponent
    {
        registeredComponents.Add(new RegisteredComponentInfo(typeof(T), isTag));
    }

    /// <inheritdoc />
    public T GetCapability<T>() where T : class
    {
        if (TryGetCapability<T>(out var capability))
        {
            return capability!;
        }

        throw new InvalidOperationException(
            $"Capability of type {typeof(T).Name} is not available. " +
            $"Use SetCapability<{typeof(T).Name}>() to provide a mock implementation.");
    }

    /// <inheritdoc />
    public bool TryGetCapability<T>(out T? capability) where T : class
    {
        // First check mock capabilities
        if (capabilities.TryGetValue(typeof(T), out var cap))
        {
            capability = (T)cap;
            return true;
        }

        // Fall back to checking if World implements it
        if (world is T worldCapability)
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
        return capabilities.ContainsKey(typeof(T)) || world is T;
    }

    #endregion
}
