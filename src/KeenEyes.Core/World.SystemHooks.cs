namespace KeenEyes;

public sealed partial class World
{
    #region System Hooks

    /// <summary>
    /// Adds a global system hook with optional before and after callbacks.
    /// </summary>
    /// <param name="beforeHook">Optional callback to invoke before each system execution.</param>
    /// <param name="afterHook">Optional callback to invoke after each system execution.</param>
    /// <param name="phase">Optional phase filter - hook only executes for systems in this phase.</param>
    /// <returns>A subscription that can be disposed to unregister the hook.</returns>
    /// <remarks>
    /// <para>
    /// System hooks enable cross-cutting concerns such as profiling, logging, conditional execution,
    /// and metrics collection without modifying individual systems. Hooks are registered globally
    /// per-world and execute for all systems (or systems in the specified phase).
    /// </para>
    /// <para>
    /// Multiple independent hooks can be registered and will execute in registration order.
    /// At least one of <paramref name="beforeHook"/> or <paramref name="afterHook"/> must be non-null.
    /// </para>
    /// <para>
    /// When no hooks are registered, there is minimal performance overhead (empty check only).
    /// With hooks registered, overhead scales linearly with the number of hooks.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Profiling hook
    /// var profilingHook = world.AddSystemHook(
    ///     beforeHook: (system, dt) => profiler.BeginSystem(system.GetType().Name),
    ///     afterHook: (system, dt) => profiler.EndSystem()
    /// );
    ///
    /// // Logging hook (only for Update phase)
    /// var loggingHook = world.AddSystemHook(
    ///     beforeHook: (system, dt) => logger.Debug($"Starting {system.GetType().Name}"),
    ///     afterHook: (system, dt) => logger.Debug($"Finished {system.GetType().Name}"),
    ///     phase: SystemPhase.Update
    /// );
    ///
    /// // Conditional execution hook
    /// var debugHook = world.AddSystemHook(
    ///     beforeHook: (system, dt) =>
    ///     {
    ///         if (system is IDebugSystem &amp;&amp; !debugMode)
    ///             system.Enabled = false;
    ///     },
    ///     afterHook: null
    /// );
    ///
    /// // Later, unregister by disposing
    /// profilingHook.Dispose();
    /// loggingHook.Dispose();
    /// debugHook.Dispose();
    /// </code>
    /// </example>
    /// <exception cref="ArgumentException">Thrown when both beforeHook and afterHook are null.</exception>
    public EventSubscription AddSystemHook(
        SystemHook? beforeHook = null,
        SystemHook? afterHook = null,
        SystemPhase? phase = null)
    {
        return systemHookManager.AddHook(beforeHook, afterHook, phase);
    }

    #endregion
}
