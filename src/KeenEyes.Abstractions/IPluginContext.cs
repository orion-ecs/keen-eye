namespace KeenEyes;

/// <summary>
/// Interface for plugin context that provides access to system registration and extension APIs.
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
/// <para>
/// Plugins that need advanced functionality should use capabilities via
/// <see cref="GetCapability{T}"/> or <see cref="TryGetCapability{T}"/>
/// instead of casting <see cref="World"/> to concrete types. This enables
/// better testability with mock implementations.
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
///     // Request capabilities for advanced functionality
///     if (context.TryGetCapability&lt;ISystemHookCapability&gt;(out var hooks))
///     {
///         hookSubscription = hooks.AddSystemHook(
///             beforeHook: (system, dt) => /* ... */,
///             afterHook: (system, dt) => /* ... */
///         );
///     }
/// }
/// </code>
/// </example>
public interface IPluginContext
{
    /// <summary>
    /// Gets the world that the plugin is being installed into or uninstalled from.
    /// </summary>
    IWorld World { get; }

    /// <summary>
    /// Gets the plugin that this context is for.
    /// </summary>
    IWorldPlugin Plugin { get; }

    /// <summary>
    /// Registers a system with the world at the specified phase and order.
    /// </summary>
    /// <typeparam name="T">The system type to register.</typeparam>
    /// <param name="phase">The execution phase for this system. Defaults to <see cref="SystemPhase.Update"/>.</param>
    /// <param name="order">The execution order within the phase. Lower values execute first. Defaults to 0.</param>
    /// <returns>The created system instance.</returns>
    /// <remarks>
    /// Systems registered through this method are tracked and will be automatically
    /// removed and disposed when the plugin is uninstalled.
    /// </remarks>
    T AddSystem<T>(SystemPhase phase = SystemPhase.Update, int order = 0)
        where T : ISystem, new();

    /// <summary>
    /// Registers a system with the world at the specified phase, order, and dependency constraints.
    /// </summary>
    /// <typeparam name="T">The system type to register.</typeparam>
    /// <param name="phase">The execution phase for this system.</param>
    /// <param name="order">The execution order within the phase. Lower values execute first.</param>
    /// <param name="runsBefore">Types of systems that this system must run before.</param>
    /// <param name="runsAfter">Types of systems that this system must run after.</param>
    /// <returns>The created system instance.</returns>
    T AddSystem<T>(
        SystemPhase phase,
        int order,
        Type[] runsBefore,
        Type[] runsAfter)
        where T : ISystem, new();

    /// <summary>
    /// Registers a system instance with the world at the specified phase and order.
    /// </summary>
    /// <param name="system">The system instance to register.</param>
    /// <param name="phase">The execution phase for this system. Defaults to <see cref="SystemPhase.Update"/>.</param>
    /// <param name="order">The execution order within the phase. Lower values execute first. Defaults to 0.</param>
    /// <returns>The system instance for chaining.</returns>
    ISystem AddSystem(ISystem system, SystemPhase phase = SystemPhase.Update, int order = 0);

    /// <summary>
    /// Registers a system instance with the world at the specified phase, order, and dependency constraints.
    /// </summary>
    /// <param name="system">The system instance to register.</param>
    /// <param name="phase">The execution phase for this system.</param>
    /// <param name="order">The execution order within the phase. Lower values execute first.</param>
    /// <param name="runsBefore">Types of systems that this system must run before.</param>
    /// <param name="runsAfter">Types of systems that this system must run after.</param>
    /// <returns>The system instance for chaining.</returns>
    ISystem AddSystem(
        ISystem system,
        SystemPhase phase,
        int order,
        Type[] runsBefore,
        Type[] runsAfter);

    /// <summary>
    /// Registers a system group with the world at the specified phase and order.
    /// </summary>
    /// <param name="group">The system group to register.</param>
    /// <param name="phase">The execution phase for this group. Defaults to <see cref="SystemPhase.Update"/>.</param>
    /// <param name="order">The execution order within the phase. Lower values execute first. Defaults to 0.</param>
    /// <returns>The system group for chaining.</returns>
    SystemGroup AddSystemGroup(SystemGroup group, SystemPhase phase = SystemPhase.Update, int order = 0);


    /// <summary>
    /// Gets an extension registered with the world.
    /// </summary>
    /// <typeparam name="T">The extension type.</typeparam>
    /// <returns>The extension instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the extension is not registered.</exception>
    T GetExtension<T>() where T : class;

    /// <summary>
    /// Tries to get an extension registered with the world.
    /// </summary>
    /// <typeparam name="T">The extension type.</typeparam>
    /// <param name="extension">When this method returns, contains the extension if found.</param>
    /// <returns>True if the extension is registered; false otherwise.</returns>
    bool TryGetExtension<T>(out T? extension) where T : class;

    /// <summary>
    /// Sets an extension value that can be retrieved by other code.
    /// </summary>
    /// <typeparam name="T">The extension type.</typeparam>
    /// <param name="extension">The extension instance to store.</param>
    /// <remarks>
    /// Extensions allow plugins to expose custom APIs to application code.
    /// For example, a physics plugin might expose a <c>PhysicsWorld</c> extension
    /// that provides raycast and collision query methods.
    /// </remarks>
    void SetExtension<T>(T extension) where T : class;

    /// <summary>
    /// Removes an extension from the world.
    /// </summary>
    /// <typeparam name="T">The extension type to remove.</typeparam>
    /// <returns>True if the extension was found and removed; false otherwise.</returns>
    bool RemoveExtension<T>() where T : class;

    /// <summary>
    /// Registers a component type with the world.
    /// </summary>
    /// <typeparam name="T">The component type to register.</typeparam>
    /// <param name="isTag">Whether this component is a tag (zero-size) component.</param>
    /// <remarks>
    /// <para>
    /// Component types are typically registered automatically when first used, but plugins
    /// may need to register components explicitly if they use dynamic component access or
    /// need to ensure a component type is available.
    /// </para>
    /// </remarks>
    void RegisterComponent<T>(bool isTag = false) where T : struct, IComponent;

    /// <summary>
    /// Gets a capability from the plugin context.
    /// </summary>
    /// <typeparam name="T">The capability interface type.</typeparam>
    /// <returns>The capability implementation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the capability is not available.</exception>
    /// <remarks>
    /// <para>
    /// Capabilities provide access to advanced functionality without requiring plugins
    /// to cast to concrete types. Common capabilities include:
    /// </para>
    /// <list type="bullet">
    /// <item><description><c>ISystemHookCapability</c> - Add hooks to system execution</description></item>
    /// <item><description><c>IPersistenceCapability</c> - Configure persistence settings</description></item>
    /// </list>
    /// <para>
    /// Use <see cref="TryGetCapability{T}"/> if the capability is optional for your plugin.
    /// </para>
    /// </remarks>
    T GetCapability<T>() where T : class;

    /// <summary>
    /// Tries to get a capability from the plugin context.
    /// </summary>
    /// <typeparam name="T">The capability interface type.</typeparam>
    /// <param name="capability">When this method returns, contains the capability if available.</param>
    /// <returns>True if the capability is available; false otherwise.</returns>
    /// <remarks>
    /// <para>
    /// Use this method when a capability is optional for your plugin's functionality.
    /// For required capabilities, use <see cref="GetCapability{T}"/> which throws if unavailable.
    /// </para>
    /// </remarks>
    bool TryGetCapability<T>(out T? capability) where T : class;

    /// <summary>
    /// Checks if a capability is available in this context.
    /// </summary>
    /// <typeparam name="T">The capability interface type.</typeparam>
    /// <returns>True if the capability is available; false otherwise.</returns>
    bool HasCapability<T>() where T : class;
}
