namespace KeenEyes.Parallelism;

/// <summary>
/// Plugin that enables parallel system execution based on component dependencies.
/// </summary>
/// <remarks>
/// <para>
/// The ParallelSystemPlugin provides infrastructure for executing ECS systems in parallel.
/// It analyzes component read/write dependencies to determine which systems can run
/// concurrently, then groups them into batches for parallel execution.
/// </para>
/// <para>
/// After installation, use <see cref="ParallelSystemScheduler"/> to register systems
/// and execute them in parallel. Systems within a batch have no conflicting dependencies
/// and can safely run concurrently.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Install the plugin
/// using var world = new World();
/// world.InstallPlugin(new ParallelSystemPlugin());
///
/// // Get the scheduler
/// var scheduler = world.GetExtension&lt;ParallelSystemScheduler&gt;();
///
/// // Register systems
/// scheduler.RegisterSystem(new MovementSystem());
/// scheduler.RegisterSystem(new PhysicsSystem());
/// scheduler.RegisterSystem(new DamageSystem());
///
/// // Execute systems in parallel batches
/// while (running)
/// {
///     scheduler.UpdateParallel(deltaTime);
/// }
/// </code>
/// </example>
/// <param name="options">Configuration options for parallel execution.</param>
public sealed class ParallelSystemPlugin(ParallelSystemOptions? options = null) : IWorldPlugin
{
    private readonly ParallelSystemOptions options = options ?? new ParallelSystemOptions();
    private ParallelSystemScheduler? scheduler;

    /// <summary>
    /// Creates a new parallel system plugin with default options.
    /// </summary>
    public ParallelSystemPlugin() : this(null)
    {
    }

    /// <summary>
    /// Gets the name of this plugin.
    /// </summary>
    public string Name => "ParallelSystem";

    /// <inheritdoc />
    public void Install(IPluginContext context)
    {
        if (context.World is not World world)
        {
            throw new InvalidOperationException("ParallelSystemPlugin requires a concrete World instance");
        }

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = options.MaxDegreeOfParallelism
        };

        scheduler = new ParallelSystemScheduler(world, parallelOptions);
        context.SetExtension(scheduler);
    }

    /// <inheritdoc />
    public void Uninstall(IPluginContext context)
    {
        if (scheduler != null)
        {
            scheduler.Clear();
            context.RemoveExtension<ParallelSystemScheduler>();
        }
    }
}

/// <summary>
/// Configuration options for the parallel system plugin.
/// </summary>
public sealed record ParallelSystemOptions
{
    /// <summary>
    /// Gets or initializes the maximum degree of parallelism.
    /// </summary>
    /// <remarks>
    /// Controls the maximum number of concurrent system executions.
    /// Default is -1 (use all available processors).
    /// </remarks>
    public int MaxDegreeOfParallelism { get; init; } = -1;

    /// <summary>
    /// Gets or initializes the minimum batch size for parallel execution.
    /// </summary>
    /// <remarks>
    /// Batches with fewer systems than this threshold are executed sequentially.
    /// Default is 2 (parallelize any batch with 2+ systems).
    /// </remarks>
    public int MinBatchSizeForParallel { get; init; } = 2;
}
