namespace KeenEyes;

/// <summary>
/// Represents a callback that is invoked before or after a system executes.
/// </summary>
/// <param name="system">The system being executed.</param>
/// <param name="deltaTime">The time elapsed since the last update in seconds.</param>
/// <remarks>
/// <para>
/// System hooks enable cross-cutting concerns such as profiling, logging, conditional execution,
/// and metrics collection without modifying individual systems. Hooks are registered globally
/// per-world and execute for all systems in that world.
/// </para>
/// <para>
/// Use World.AddSystemHook to register hooks. Multiple independent hooks can be
/// registered and will execute in registration order.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Profiling hook
/// world.AddSystemHook(
///     beforeHook: (system, dt) => profiler.BeginSystem(system.GetType().Name),
///     afterHook: (system, dt) => profiler.EndSystem()
/// );
///
/// // Logging hook
/// world.AddSystemHook(
///     beforeHook: (system, dt) => logger.Debug($"Starting {system.GetType().Name}"),
///     afterHook: (system, dt) => logger.Debug($"Finished {system.GetType().Name}")
/// );
/// </code>
/// </example>
public delegate void SystemHook(ISystem system, float deltaTime);
