namespace KeenEyes;

/// <summary>
/// Interface for world plugins that provide modular functionality to an ECS world.
/// </summary>
/// <remarks>
/// <para>
/// Plugins encapsulate related systems, components, and functionality that can be
/// installed into a world. This enables modular architecture where features like
/// physics, rendering, or networking can be added independently.
/// </para>
/// <para>
/// Plugins are installed per-world, maintaining the isolation principle where each
/// world has its own independent state. A plugin can be installed on multiple worlds
/// simultaneously if it maintains no shared mutable state.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class PhysicsPlugin : IWorldPlugin
/// {
///     public string Name => "Physics";
///
///     public void Install(PluginContext context)
///     {
///         // Register physics systems
///         context.AddSystem&lt;BroadphaseSystem&gt;(SystemPhase.FixedUpdate, order: 0);
///         context.AddSystem&lt;NarrowphaseSystem&gt;(SystemPhase.FixedUpdate, order: 10);
///         context.AddSystem&lt;SolverSystem&gt;(SystemPhase.FixedUpdate, order: 20);
///
///         // Expose physics API
///         context.SetExtension(new PhysicsWorld());
///     }
///
///     public void Uninstall(PluginContext context)
///     {
///         // Cleanup is handled automatically
///     }
/// }
/// </code>
/// </example>
public interface IWorldPlugin
{
    /// <summary>
    /// Gets the unique name of this plugin.
    /// </summary>
    /// <remarks>
    /// The name should be unique within a world. Installing a plugin with the same
    /// name as an already-installed plugin will throw an exception.
    /// </remarks>
    string Name { get; }

    /// <summary>
    /// Called when the plugin is installed into a world.
    /// </summary>
    /// <param name="context">
    /// The plugin context providing access to system registration and extension APIs.
    /// </param>
    /// <remarks>
    /// <para>
    /// Use this method to register systems, set up extensions, and perform any
    /// initialization the plugin requires. Systems registered through the context
    /// are tracked and will be automatically cleaned up on uninstall.
    /// </para>
    /// </remarks>
    void Install(PluginContext context);

    /// <summary>
    /// Called when the plugin is uninstalled from a world.
    /// </summary>
    /// <param name="context">
    /// The plugin context providing access to cleanup operations.
    /// </param>
    /// <remarks>
    /// <para>
    /// Use this method to perform any custom cleanup beyond what is automatically
    /// handled. Systems registered during installation are automatically removed
    /// and disposed.
    /// </para>
    /// </remarks>
    void Uninstall(PluginContext context);
}
