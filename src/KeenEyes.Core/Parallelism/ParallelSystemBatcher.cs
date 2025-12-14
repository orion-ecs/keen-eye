namespace KeenEyes;

/// <summary>
/// Analyzes system dependencies and groups systems into parallel execution batches.
/// </summary>
/// <remarks>
/// <para>
/// The batcher respects both explicit ordering constraints (RunBefore/RunAfter)
/// and component dependencies (read/write conflicts). Systems within a batch
/// can safely execute in parallel, while batches execute sequentially.
/// </para>
/// <para>
/// The algorithm works in two phases:
/// 1. Topologically sort systems respecting explicit ordering constraints
/// 2. Greedily batch compatible systems that have no component conflicts
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var batcher = new ParallelSystemBatcher(dependencyTracker);
/// var batches = batcher.CreateBatches(sortedSystems);
///
/// foreach (var batch in batches)
/// {
///     // Execute all systems in this batch in parallel
///     Parallel.ForEach(batch, system => system.Update(deltaTime));
///     // Wait for batch completion before next batch
/// }
/// </code>
/// </example>
/// <param name="dependencyTracker">The tracker containing component dependencies for systems.</param>
public sealed class ParallelSystemBatcher(SystemDependencyTracker dependencyTracker)
{

    /// <summary>
    /// Groups systems into parallel execution batches based on their dependencies.
    /// </summary>
    /// <param name="sortedSystems">Systems already sorted by topological order (respecting RunBefore/RunAfter).</param>
    /// <returns>A list of batches, where each batch contains systems that can run in parallel.</returns>
    /// <remarks>
    /// <para>
    /// The input systems must already be topologically sorted to respect explicit ordering constraints.
    /// This method only considers component dependencies for parallel grouping.
    /// </para>
    /// <para>
    /// Systems are greedily added to the current batch if they don't conflict with any system
    /// already in that batch. A new batch is started when a conflict is detected.
    /// </para>
    /// </remarks>
    public IReadOnlyList<SystemBatch> CreateBatches(IEnumerable<ISystem> sortedSystems)
    {
        var systems = sortedSystems.ToList();
        if (systems.Count == 0)
        {
            return [];
        }

        var batches = new List<SystemBatch>();
        var currentBatch = new List<ISystem>();
        var currentBatchDependencies = new List<ComponentDependencies>();

        foreach (var system in systems)
        {
            var systemType = system.GetType();
            var deps = dependencyTracker.TryGetDependencies(systemType, out var d)
                ? d
                : ComponentDependencies.Empty;

            // Check if this system conflicts with any system in the current batch
            var hasConflict = false;
            foreach (var existingDeps in currentBatchDependencies)
            {
                if (deps.ConflictsWith(existingDeps))
                {
                    hasConflict = true;
                    break;
                }
            }

            if (hasConflict && currentBatch.Count > 0)
            {
                // Start a new batch
                batches.Add(new SystemBatch(currentBatch.ToArray()));
                currentBatch.Clear();
                currentBatchDependencies.Clear();
            }

            currentBatch.Add(system);
            currentBatchDependencies.Add(deps);
        }

        // Add the final batch
        if (currentBatch.Count > 0)
        {
            batches.Add(new SystemBatch(currentBatch.ToArray()));
        }

        return batches;
    }

    /// <summary>
    /// Groups system types into parallel execution batches based on their dependencies.
    /// </summary>
    /// <param name="sortedSystemTypes">System types already sorted by topological order.</param>
    /// <returns>A list of batches, where each batch contains system types that can run in parallel.</returns>
    public IReadOnlyList<TypeBatch> CreateTypeBatches(IEnumerable<Type> sortedSystemTypes)
    {
        var systemTypes = sortedSystemTypes.ToList();
        if (systemTypes.Count == 0)
        {
            return [];
        }

        var batches = new List<TypeBatch>();
        var currentBatch = new List<Type>();
        var currentBatchDependencies = new List<ComponentDependencies>();

        foreach (var systemType in systemTypes)
        {
            var deps = dependencyTracker.TryGetDependencies(systemType, out var d)
                ? d
                : ComponentDependencies.Empty;

            // Check if this system conflicts with any system in the current batch
            var hasConflict = false;
            foreach (var existingDeps in currentBatchDependencies)
            {
                if (deps.ConflictsWith(existingDeps))
                {
                    hasConflict = true;
                    break;
                }
            }

            if (hasConflict && currentBatch.Count > 0)
            {
                // Start a new batch
                batches.Add(new TypeBatch(currentBatch.ToArray()));
                currentBatch.Clear();
                currentBatchDependencies.Clear();
            }

            currentBatch.Add(systemType);
            currentBatchDependencies.Add(deps);
        }

        // Add the final batch
        if (currentBatch.Count > 0)
        {
            batches.Add(new TypeBatch(currentBatch.ToArray()));
        }

        return batches;
    }

    /// <summary>
    /// Analyzes systems and returns detailed conflict information.
    /// </summary>
    /// <param name="systems">The systems to analyze.</param>
    /// <returns>Analysis result with conflict details.</returns>
    public BatchAnalysis Analyze(IEnumerable<ISystem> systems)
    {
        var systemList = systems.ToList();
        var conflicts = new List<SystemConflict>();

        for (int i = 0; i < systemList.Count; i++)
        {
            for (int j = i + 1; j < systemList.Count; j++)
            {
                var typeA = systemList[i].GetType();
                var typeB = systemList[j].GetType();

                var depsA = dependencyTracker.TryGetDependencies(typeA, out var a)
                    ? a
                    : ComponentDependencies.Empty;
                var depsB = dependencyTracker.TryGetDependencies(typeB, out var b)
                    ? b
                    : ComponentDependencies.Empty;

                if (depsA.ConflictsWith(depsB))
                {
                    var conflictingComponents = depsA.GetConflictingComponents(depsB);
                    conflicts.Add(new SystemConflict(typeA, typeB, conflictingComponents));
                }
            }
        }

        var batches = CreateBatches(systemList);
        return new BatchAnalysis(batches, conflicts);
    }
}

/// <summary>
/// Represents a batch of systems that can execute in parallel.
/// </summary>
/// <param name="Systems">The systems in this batch.</param>
public readonly record struct SystemBatch(IReadOnlyList<ISystem> Systems)
{
    /// <summary>
    /// Gets the number of systems in this batch.
    /// </summary>
    public int Count => Systems.Count;

    /// <summary>
    /// Gets whether this batch can be parallelized (has more than one system).
    /// </summary>
    public bool IsParallelizable => Systems.Count > 1;
}

/// <summary>
/// Represents a batch of system types that can execute in parallel.
/// </summary>
/// <param name="SystemTypes">The system types in this batch.</param>
public readonly record struct TypeBatch(IReadOnlyList<Type> SystemTypes)
{
    /// <summary>
    /// Gets the number of system types in this batch.
    /// </summary>
    public int Count => SystemTypes.Count;

    /// <summary>
    /// Gets whether this batch can be parallelized (has more than one system type).
    /// </summary>
    public bool IsParallelizable => SystemTypes.Count > 1;
}

/// <summary>
/// Represents a conflict between two systems.
/// </summary>
/// <param name="SystemA">The first system type involved in the conflict.</param>
/// <param name="SystemB">The second system type involved in the conflict.</param>
/// <param name="ConflictingComponents">The component types that cause the conflict.</param>
public readonly record struct SystemConflict(
    Type SystemA,
    Type SystemB,
    IReadOnlyCollection<Type> ConflictingComponents);

/// <summary>
/// Contains the results of batch analysis.
/// </summary>
/// <param name="Batches">The generated execution batches.</param>
/// <param name="Conflicts">All detected conflicts between systems.</param>
public readonly record struct BatchAnalysis(
    IReadOnlyList<SystemBatch> Batches,
    IReadOnlyList<SystemConflict> Conflicts)
{
    /// <summary>
    /// Gets the total number of batches.
    /// </summary>
    public int BatchCount => Batches.Count;

    /// <summary>
    /// Gets the total number of conflicts detected.
    /// </summary>
    public int ConflictCount => Conflicts.Count;

    /// <summary>
    /// Gets the maximum parallelism achieved (size of the largest batch).
    /// </summary>
    public int MaxParallelism => Batches.Count > 0
        ? Batches.Max(b => b.Count)
        : 0;
}
