using KeenEyes.Capabilities;
using KeenEyes.Debugging.Timeline;

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
/// var queryProfiler = world.GetExtension&lt;QueryProfiler&gt;();
/// var timeline = world.GetExtension&lt;TimelineRecorder&gt;();
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
    private EventSubscription? timelineHook;

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
        // Install DebugController (always available, provides debug mode toggle)
        var debugController = new DebugController(options.InitialDebugMode);
        context.SetExtension(debugController);

        // Install EntityInspector if inspection capability is available (no performance overhead)
        if (context.TryGetCapability<IInspectionCapability>(out var inspectionCapability) && inspectionCapability is not null)
        {
            context.TryGetCapability<IHierarchyCapability>(out var hierarchyCapability);
            var inspector = new EntityInspector(context.World, inspectionCapability, hierarchyCapability);
            context.SetExtension(inspector);
        }

        // Get statistics capability for MemoryTracker and QueryProfiler
        if (context.TryGetCapability<IStatisticsCapability>(out var statsCapability) && statsCapability is not null)
        {
            var memoryTracker = new MemoryTracker(statsCapability);
            context.SetExtension(memoryTracker);

            // Install QueryProfiler if enabled
            if (options.EnableQueryProfiling)
            {
                var queryProfiler = new QueryProfiler(statsCapability);
                context.SetExtension(queryProfiler);
            }
        }

        // Get the system hook capability for profiling, GC tracking, and timeline
        ISystemHookCapability? hookCapability = null;
        if (options.EnableProfiling || options.EnableGCTracking || options.EnableTimeline)
        {
            if (!context.TryGetCapability<ISystemHookCapability>(out hookCapability))
            {
                throw new InvalidOperationException(
                    "DebugPlugin requires ISystemHookCapability for profiling, GC tracking, or timeline. " +
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

        // Conditionally install timeline recording
        if (options.EnableTimeline && hookCapability is not null)
        {
            var timelineRecorder = new TimelineRecorder(options.TimelineMaxFrames);
            context.SetExtension(timelineRecorder);

            timelineHook = hookCapability.AddSystemHook(
                beforeHook: (system, dt) => timelineRecorder.BeginRecording(system.GetType().Name),
                afterHook: (system, dt) => timelineRecorder.EndRecording(system.GetType().Name, dt),
                phase: options.TimelinePhase
            );
        }
    }

    /// <inheritdoc />
    public void Uninstall(IPluginContext context)
    {
        // Dispose hooks
        profilingHook?.Dispose();
        gcTrackingHook?.Dispose();
        timelineHook?.Dispose();

        // Remove extensions
        context.RemoveExtension<DebugController>();
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

        if (options.EnableQueryProfiling)
        {
            context.RemoveExtension<QueryProfiler>();
        }

        if (options.EnableTimeline)
        {
            context.RemoveExtension<TimelineRecorder>();
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
    /// Gets or initializes the initial debug mode state.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When debug mode is enabled, debugging components may perform additional
    /// diagnostics, capture more detailed information, or enable verbose logging.
    /// </para>
    /// <para>
    /// The debug mode can be toggled at runtime via <see cref="DebugController"/>.
    /// Default is false.
    /// </para>
    /// </remarks>
    public bool InitialDebugMode { get; init; } = false;

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

    /// <summary>
    /// Gets or initializes a value indicating whether query profiling is enabled.
    /// </summary>
    /// <remarks>
    /// When enabled, the query profiler provides access to cache statistics and allows
    /// manual timing of individual queries. Unlike system profiling, query timing requires
    /// manual instrumentation. Default is true.
    /// </remarks>
    public bool EnableQueryProfiling { get; init; } = true;

    /// <summary>
    /// Gets or initializes a value indicating whether timeline recording is enabled.
    /// </summary>
    /// <remarks>
    /// When enabled, the timeline recorder captures detailed execution history for all
    /// systems, allowing frame-by-frame analysis and export for external visualization.
    /// Default is false (due to memory overhead of storing history).
    /// </remarks>
    public bool EnableTimeline { get; init; } = false;

    /// <summary>
    /// Gets or initializes the maximum number of frames to keep in timeline history.
    /// </summary>
    /// <remarks>
    /// Older frames are automatically discarded to manage memory usage. Default is 300
    /// (approximately 5 seconds at 60fps).
    /// </remarks>
    public int TimelineMaxFrames { get; init; } = 300;

    /// <summary>
    /// Gets or initializes the phase filter for timeline recording.
    /// </summary>
    /// <remarks>
    /// If specified, timeline will only record systems in the specified phase.
    /// If null, all systems in all phases are recorded. Default is null (record all phases).
    /// </remarks>
    public SystemPhase? TimelinePhase { get; init; } = null;
}
