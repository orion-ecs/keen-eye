using KeenEyes.Capabilities;

namespace KeenEyes.Debugging;

/// <summary>
/// Plugin that adds debugging and profiling capabilities to a KeenEyes world.
/// </summary>
/// <remarks>
/// <para>
/// The DebugPlugin provides comprehensive debugging tools including system profiling,
/// entity inspection, memory tracking, and GC allocation monitoring. These tools use
/// SystemHooks for automatic integration without modifying individual systems.
/// </para>
/// <para>
/// All debugging features are exposed through extension APIs that can be retrieved
/// using <see cref="IWorld.GetExtension{T}"/>. Features can be enabled or disabled
/// individually through the configuration options.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Install with default options (all features enabled)
/// using var world = new World();
/// world.InstallPlugin(new DebugPlugin());
///
/// // Access debugging tools
/// var profiler = world.GetExtension&lt;Profiler&gt;();
/// var inspector = world.GetExtension&lt;EntityInspector&gt;();
/// var memoryTracker = world.GetExtension&lt;MemoryTracker&gt;();
/// var gcTracker = world.GetExtension&lt;GCTracker&gt;();
///
/// // Run your simulation
/// world.Update(0.016f);
///
/// // Print profiling report
/// foreach (var profile in profiler.GetAllSystemProfiles())
/// {
///     Console.WriteLine($"{profile.Name}: {profile.AverageTime.TotalMilliseconds:F2}ms");
/// }
/// </code>
/// </example>
/// <param name="options">Configuration options for the debug plugin.</param>
public sealed class DebugPlugin(DebugOptions? options = null) : IWorldPlugin
{
    private readonly DebugOptions options = options ?? new DebugOptions();
    private EventSubscription? profilingHook;
    private EventSubscription? gcTrackingHook;

    /// <summary>
    /// Creates a new debug plugin with default options.
    /// </summary>
    public DebugPlugin() : this(null)
    {
    }

    /// <summary>
    /// Gets the name of this plugin.
    /// </summary>
    public string Name => "Debug";

    /// <inheritdoc />
    public void Install(IPluginContext context)
    {
        // Install EntityInspector if inspection capability is available (no performance overhead)
        if (context.TryGetCapability<IInspectionCapability>(out var inspectionCapability) && inspectionCapability is not null)
        {
            context.TryGetCapability<IHierarchyCapability>(out var hierarchyCapability);
            var inspector = new EntityInspector(context.World, inspectionCapability, hierarchyCapability);
            context.SetExtension(inspector);
        }

        // Get statistics capability for MemoryTracker
        if (context.TryGetCapability<IStatisticsCapability>(out var statsCapability) && statsCapability is not null)
        {
            var memoryTracker = new MemoryTracker(statsCapability);
            context.SetExtension(memoryTracker);
        }

        // Get the system hook capability for profiling and GC tracking
        ISystemHookCapability? hookCapability = null;
        if (options.EnableProfiling || options.EnableGCTracking)
        {
            if (!context.TryGetCapability<ISystemHookCapability>(out hookCapability))
            {
                throw new InvalidOperationException(
                    "DebugPlugin requires ISystemHookCapability for profiling or GC tracking. " +
                    "Disable these options or provide a world that supports system hooks.");
            }
        }

        // Conditionally install profiling
        if (options.EnableProfiling && hookCapability is not null)
        {
            var profiler = new Profiler();
            context.SetExtension(profiler);

            profilingHook = hookCapability.AddSystemHook(
                beforeHook: (system, dt) => profiler.BeginSample(system.GetType().Name),
                afterHook: (system, dt) => profiler.EndSample(system.GetType().Name),
                phase: options.ProfilingPhase
            );
        }

        // Conditionally install GC tracking
        if (options.EnableGCTracking && hookCapability is not null)
        {
            var gcTracker = new GCTracker();
            context.SetExtension(gcTracker);

            gcTrackingHook = hookCapability.AddSystemHook(
                beforeHook: (system, dt) => gcTracker.BeginTracking(system.GetType().Name),
                afterHook: (system, dt) => gcTracker.EndTracking(system.GetType().Name),
                phase: options.GCTrackingPhase
            );
        }
    }

    /// <inheritdoc />
    public void Uninstall(IPluginContext context)
    {
        // Dispose hooks
        profilingHook?.Dispose();
        gcTrackingHook?.Dispose();

        // Remove extensions
        context.RemoveExtension<EntityInspector>();
        context.RemoveExtension<MemoryTracker>();

        if (options.EnableProfiling)
        {
            context.RemoveExtension<Profiler>();
        }

        if (options.EnableGCTracking)
        {
            context.RemoveExtension<GCTracker>();
        }
    }
}

/// <summary>
/// Configuration options for the debug plugin.
/// </summary>
/// <remarks>
/// These options control which debugging features are enabled and their behavior.
/// Features that are disabled have zero performance overhead.
/// </remarks>
public sealed record DebugOptions
{
    /// <summary>
    /// Gets or initializes a value indicating whether system profiling is enabled.
    /// </summary>
    /// <remarks>
    /// When enabled, the profiler tracks execution time for all systems and provides
    /// detailed timing metrics. Default is true.
    /// </remarks>
    public bool EnableProfiling { get; init; } = true;

    /// <summary>
    /// Gets or initializes a value indicating whether GC allocation tracking is enabled.
    /// </summary>
    /// <remarks>
    /// When enabled, tracks memory allocations per system to identify allocation hotspots.
    /// This has minimal overhead but may not capture all allocations in multi-threaded scenarios.
    /// Default is true.
    /// </remarks>
    public bool EnableGCTracking { get; init; } = true;

    /// <summary>
    /// Gets or initializes the phase filter for profiling hooks.
    /// </summary>
    /// <remarks>
    /// If specified, profiling will only track systems in the specified phase.
    /// If null, all systems in all phases are profiled. Default is null (profile all phases).
    /// </remarks>
    public SystemPhase? ProfilingPhase { get; init; } = null;

    /// <summary>
    /// Gets or initializes the phase filter for GC tracking hooks.
    /// </summary>
    /// <remarks>
    /// If specified, GC tracking will only monitor systems in the specified phase.
    /// If null, all systems in all phases are tracked. Default is null (track all phases).
    /// </remarks>
    public SystemPhase? GCTrackingPhase { get; init; } = null;
}
