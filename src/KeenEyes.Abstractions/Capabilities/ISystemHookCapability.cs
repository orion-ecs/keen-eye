namespace KeenEyes.Capabilities;

/// <summary>
/// Capability interface for adding system hooks to an ECS world.
/// </summary>
/// <remarks>
/// <para>
/// System hooks enable cross-cutting concerns such as profiling, logging,
/// conditional execution, and metrics collection without modifying individual systems.
/// </para>
/// <para>
/// Plugins that need to hook into system execution should request this capability
/// via <see cref="IPluginContext.GetCapability{T}"/> rather than casting
/// <see cref="IPluginContext.World"/> to the concrete World type.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public void Install(IPluginContext context)
/// {
///     if (!context.TryGetCapability&lt;ISystemHookCapability&gt;(out var hookCapability))
///     {
///         throw new InvalidOperationException("Plugin requires system hook capability");
///     }
///
///     hookSubscription = hookCapability.AddSystemHook(
///         beforeHook: (system, dt) => profiler.BeginSystem(system.GetType().Name),
///         afterHook: (system, dt) => profiler.EndSystem()
///     );
/// }
/// </code>
/// </example>
public interface ISystemHookCapability
{
    /// <summary>
    /// Adds global hooks that execute before and/or after every system in the world.
    /// </summary>
    /// <param name="beforeHook">Optional hook to execute before each system runs.</param>
    /// <param name="afterHook">Optional hook to execute after each system completes.</param>
    /// <param name="phase">Optional phase filter. If specified, hooks only execute for systems in this phase.</param>
    /// <returns>A subscription that can be disposed to remove the hooks.</returns>
    /// <exception cref="ArgumentException">Thrown when both beforeHook and afterHook are null.</exception>
    EventSubscription AddSystemHook(
        SystemHook? beforeHook = null,
        SystemHook? afterHook = null,
        SystemPhase? phase = null);
}
