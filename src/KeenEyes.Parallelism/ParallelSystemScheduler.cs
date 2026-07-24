namespace KeenEyes.Parallelism;

/// <summary>
/// Schedules and executes systems in parallel batches based on component dependencies.
/// </summary>
/// <remarks>
/// <para>
/// The scheduler analyzes system component dependencies to determine which systems
/// can run concurrently. Systems within a batch have no conflicting dependencies
/// and can be executed in parallel. Batches are executed sequentially.
/// </para>
/// <para>
/// Each system is assigned a CommandBuffer from a pool. After batch execution,
/// all CommandBuffers are merged and flushed to maintain deterministic ordering.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var scheduler = world.GetExtension&lt;ParallelSystemScheduler&gt;();
/// scheduler.UpdateParallel(deltaTime);
/// </code>
/// </example>
public sealed class ParallelSystemScheduler
{
    private readonly World world;
    private readonly SystemDependencyTracker dependencyTracker;
    private readonly ParallelSystemBatcher batcher;
    private readonly CommandBufferPool commandBufferPool;
    private readonly ParallelOptions parallelOptions;
    private readonly int minBatchSizeForParallel;
    private readonly List<ISystem> registeredSystems = [];

    // Stable, monotonically-increasing command-buffer id per registered system instance.
    // Keyed by reference so systems that override GetHashCode()/Equals cannot collide on
    // a single CommandBufferPool key (which would throw on Rent and break deterministic
    // flush ordering). See issue #1155.
    private readonly Dictionary<ISystem, int> systemIds;
    private int nextSystemId;
    private IReadOnlyList<SystemBatch>? cachedBatches;
    private bool batchesDirty = true;

    /// <summary>
    /// Gets the dependency tracker used by this scheduler.
    /// </summary>
    public SystemDependencyTracker DependencyTracker => dependencyTracker;

    /// <summary>
    /// Gets the command buffer pool used by this scheduler.
    /// </summary>
    public CommandBufferPool CommandBufferPool => commandBufferPool;

    /// <summary>
    /// Gets the number of registered systems.
    /// </summary>
    public int SystemCount => registeredSystems.Count;

    /// <summary>
    /// Gets the number of execution batches.
    /// </summary>
    public int BatchCount => GetBatches().Count;

    /// <summary>
    /// Creates a new parallel system scheduler for the specified world.
    /// </summary>
    /// <param name="world">The world to schedule systems for.</param>
    /// <param name="options">Optional parallel execution options.</param>
    /// <param name="minBatchSizeForParallel">
    /// Batches with fewer systems than this threshold execute sequentially rather than in parallel.
    /// </param>
    internal ParallelSystemScheduler(World world, ParallelOptions? options = null, int minBatchSizeForParallel = 2)
    {
        this.world = world;
        this.minBatchSizeForParallel = minBatchSizeForParallel;

        // IDE0028: a collection expression cannot carry the reference-equality comparer,
        // which is required so systems overriding GetHashCode()/Equals do not collide on a
        // single id (issue #1155). The explicit constructor is intentional.
#pragma warning disable IDE0028
        systemIds = new Dictionary<ISystem, int>(ReferenceEqualityComparer.Instance);
#pragma warning restore IDE0028
        dependencyTracker = new SystemDependencyTracker();
        batcher = new ParallelSystemBatcher(dependencyTracker);
        commandBufferPool = new CommandBufferPool();
        parallelOptions = options ?? new ParallelOptions();
    }

    /// <summary>
    /// Registers a system for parallel execution with automatic dependency detection.
    /// </summary>
    /// <param name="system">The system to register.</param>
    /// <remarks>
    /// If the system implements <see cref="ISystemDependencyProvider"/>, its dependencies
    /// are automatically extracted. Otherwise, empty dependencies are assumed.
    /// </remarks>
    public void RegisterSystem(ISystem system)
    {
        registeredSystems.Add(system);
        AssignSystemId(system);
        dependencyTracker.RegisterSystem(system);
        batchesDirty = true;
    }

    /// <summary>
    /// Registers a system with explicit component dependencies.
    /// </summary>
    /// <param name="system">The system to register.</param>
    /// <param name="dependencies">The component dependencies for this system.</param>
    public void RegisterSystem(ISystem system, ComponentDependencies dependencies)
    {
        registeredSystems.Add(system);
        AssignSystemId(system);
        dependencyTracker.RegisterDependencies(system.GetType(), dependencies);
        batchesDirty = true;
    }

    private void AssignSystemId(ISystem system)
    {
        if (!systemIds.ContainsKey(system))
        {
            systemIds[system] = ++nextSystemId;
        }
    }

    /// <summary>
    /// Unregisters a system from parallel execution.
    /// </summary>
    /// <param name="system">The system to unregister.</param>
    /// <returns>True if the system was found and removed.</returns>
    public bool UnregisterSystem(ISystem system)
    {
        var removed = registeredSystems.Remove(system);
        if (removed)
        {
            systemIds.Remove(system);

            // The dependency tracker keys by Type, but multiple instances of the same
            // system type may be registered. Only drop the type's shared dependency
            // registration when no other live instance of that type remains; otherwise
            // a surviving sibling would silently fall back to empty dependencies and be
            // batched to run concurrently with conflicting systems. See issue #1158.
            var systemType = system.GetType();
            var hasOtherInstance = false;
            foreach (var other in registeredSystems)
            {
                if (other.GetType() == systemType)
                {
                    hasOtherInstance = true;
                    break;
                }
            }

            if (!hasOtherInstance)
            {
                dependencyTracker.Unregister(systemType);
            }

            batchesDirty = true;
        }
        return removed;
    }

    /// <summary>
    /// Clears all registered systems.
    /// </summary>
    public void Clear()
    {
        registeredSystems.Clear();
        systemIds.Clear();
        nextSystemId = 0;
        dependencyTracker.Clear();
        commandBufferPool.Clear();
        cachedBatches = null;
        batchesDirty = true;
    }

    /// <summary>
    /// Updates all registered systems in parallel batches.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update.</param>
    /// <remarks>
    /// <para>
    /// Systems are grouped into batches based on component dependencies. Systems within
    /// a batch execute concurrently, while batches execute sequentially.
    /// </para>
    /// <para>
    /// Each system receives its own CommandBuffer for entity operations. After each batch,
    /// CommandBuffers are flushed to maintain deterministic ordering.
    /// </para>
    /// </remarks>
    public void UpdateParallel(float deltaTime)
    {
        var batches = GetBatches();

        foreach (var batch in batches)
        {
            ExecuteBatch(batch, deltaTime);
        }
    }

    /// <summary>
    /// Gets an analysis of the current system batching.
    /// </summary>
    /// <returns>A batch analysis with conflict details.</returns>
    public BatchAnalysis GetAnalysis()
    {
        return batcher.Analyze(registeredSystems);
    }

    /// <summary>
    /// Gets the current execution batches.
    /// </summary>
    /// <returns>The list of system batches.</returns>
    public IReadOnlyList<SystemBatch> GetBatches()
    {
        if (batchesDirty || cachedBatches == null)
        {
            cachedBatches = batcher.CreateBatches(registeredSystems);
            batchesDirty = false;
        }
        return cachedBatches;
    }

    private void ExecuteBatch(SystemBatch batch, float deltaTime)
    {
        var systems = batch.Systems;

        if (systems.Count == 0)
        {
            return;
        }

        // Collect the stable command-buffer ids for this batch upfront for buffer management.
        var bufferIds = new List<int>(systems.Count);
        for (int i = 0; i < systems.Count; i++)
        {
            bufferIds.Add(GetSystemId(systems[i]));
        }

        try
        {
            if (systems.Count < minBatchSizeForParallel)
            {
                // Below the parallel threshold - execute sequentially.
                for (int i = 0; i < systems.Count; i++)
                {
                    ExecuteSystem(systems[i], deltaTime);
                }
            }
            else
            {
                // At or above the threshold - execute in parallel.
                Parallel.ForEach(systems, parallelOptions, system =>
                {
                    ExecuteSystem(system, deltaTime);
                });
            }

            // Flush all command buffers from this batch (also returns them to pool)
            commandBufferPool.FlushBatches(world, [bufferIds]);
        }
        catch
        {
            // On exception, clear buffers without flushing to avoid partial state
            // and return them to the pool for reuse
            foreach (var bufferId in bufferIds)
            {
                commandBufferPool.Return(bufferId);
            }
            throw;
        }
    }

    private int GetSystemId(ISystem system)
    {
        return systemIds.TryGetValue(system, out var id)
            ? id
            : throw new InvalidOperationException("System is not registered with this scheduler.");
    }

    private void ExecuteSystem(ISystem system, float deltaTime)
    {
        if (!system.Enabled)
        {
            return;
        }

        // Get a CommandBuffer for this system
        // Note: Buffer is NOT returned here - ExecuteBatch handles flushing and returning
        // after all systems in the batch have completed, ensuring commands are not lost.
        var systemId = GetSystemId(system);
        var commandBuffer = commandBufferPool.Rent(systemId);

        if (system is SystemBase systemBase)
        {
            // Inject command buffer if system supports it
            if (systemBase is ICommandBufferConsumer consumer)
            {
                consumer.SetCommandBuffer(commandBuffer);
            }

            systemBase.InvokeBeforeUpdate(deltaTime);
            systemBase.Update(deltaTime);
            systemBase.InvokeAfterUpdate(deltaTime);
        }
        else
        {
            system.Update(deltaTime);
        }
    }
}

/// <summary>
/// Interface for systems that can receive a command buffer for deferred entity operations.
/// </summary>
public interface ICommandBufferConsumer
{
    /// <summary>
    /// Sets the command buffer for this system to use during update.
    /// </summary>
    /// <param name="commandBuffer">The command buffer for deferred operations.</param>
    void SetCommandBuffer(ICommandBuffer commandBuffer);
}
