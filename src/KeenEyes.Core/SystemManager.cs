using System.Buffers;

namespace KeenEyes;

/// <summary>
/// Manages system registration, ordering, and execution.
/// </summary>
/// <remarks>
/// <para>
/// This is an internal manager class that handles all system operations.
/// The public API is exposed through <see cref="World"/>.
/// </para>
/// <para>
/// Systems are sorted by phase then by order, using topological sorting
/// when RunBefore/RunAfter constraints exist.
/// </para>
/// <para>
/// This class is thread-safe: system registration and queries can be called
/// concurrently from multiple threads. Update/FixedUpdate use a snapshot pattern
/// for iteration.
/// </para>
/// </remarks>
internal sealed class SystemManager
{
    private readonly Lock syncRoot = new();
    private readonly List<SystemEntry> systems = [];
    private readonly World world;
    private readonly SystemHookManager hookManager;
    private bool systemsSorted = true;

    /// <summary>
    /// Creates a new system manager for the specified world.
    /// </summary>
    /// <param name="world">The world that owns this system manager.</param>
    /// <param name="hookManager">The hook manager for invoking system hooks.</param>
    internal SystemManager(World world, SystemHookManager hookManager)
    {
        this.world = world;
        this.hookManager = hookManager;
    }

    /// <summary>
    /// Adds a system to this world with specified execution phase, order, and dependency constraints.
    /// </summary>
    /// <typeparam name="T">The system type to add.</typeparam>
    /// <param name="phase">The execution phase for this system.</param>
    /// <param name="order">The execution order within the phase.</param>
    /// <param name="runsBefore">Types of systems that this system must run before.</param>
    /// <param name="runsAfter">Types of systems that this system must run after.</param>
    internal void AddSystem<T>(
        SystemPhase phase,
        int order,
        Type[] runsBefore,
        Type[] runsAfter) where T : ISystem, new()
    {
        var system = new T();
        system.Initialize(world);
        lock (syncRoot)
        {
            systems.Add(new SystemEntry(system, phase, order, runsBefore, runsAfter));
            systemsSorted = false;
        }
    }

    /// <summary>
    /// Adds a system instance with specified execution phase, order, and dependency constraints.
    /// </summary>
    /// <param name="system">The system instance to add.</param>
    /// <param name="phase">The execution phase for this system.</param>
    /// <param name="order">The execution order within the phase.</param>
    /// <param name="runsBefore">Types of systems that this system must run before.</param>
    /// <param name="runsAfter">Types of systems that this system must run after.</param>
    internal void AddSystem(
        ISystem system,
        SystemPhase phase,
        int order,
        Type[] runsBefore,
        Type[] runsAfter)
    {
        system.Initialize(world);
        lock (syncRoot)
        {
            systems.Add(new SystemEntry(system, phase, order, runsBefore, runsAfter));
            systemsSorted = false;
        }
    }

    /// <summary>
    /// Updates all enabled systems with the given delta time.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update.</param>
    internal void Update(float deltaTime)
    {
        // Get a snapshot of systems under lock using pooled array
        int count;
        SystemEntry[] rentedArray;
        lock (syncRoot)
        {
            EnsureSystemsSortedNoLock();
            count = systems.Count;
            if (count == 0)
            {
                return;
            }
            rentedArray = ArrayPool<SystemEntry>.Shared.Rent(count);
            systems.CopyTo(rentedArray);
        }

        try
        {
            for (int i = 0; i < count; i++)
            {
                var entry = rentedArray[i];
                var system = entry.System;

                if (!system.Enabled)
                {
                    continue;
                }

                // Invoke before hooks
                hookManager.InvokeBeforeHooks(system, deltaTime, entry.Phase);

                if (system is SystemBase systemBase)
                {
                    systemBase.InvokeBeforeUpdate(deltaTime);
                    systemBase.Update(deltaTime);
                    systemBase.InvokeAfterUpdate(deltaTime);
                }
                else
                {
                    system.Update(deltaTime);
                }

                // Invoke after hooks
                hookManager.InvokeAfterHooks(system, deltaTime, entry.Phase);
            }
        }
        finally
        {
            ArrayPool<SystemEntry>.Shared.Return(rentedArray);
        }
    }

    /// <summary>
    /// Updates only systems in the <see cref="SystemPhase.FixedUpdate"/> phase.
    /// </summary>
    /// <param name="fixedDeltaTime">The fixed timestep interval.</param>
    internal void FixedUpdate(float fixedDeltaTime)
    {
        // Get a snapshot of systems under lock using pooled array
        int count;
        SystemEntry[] rentedArray;
        lock (syncRoot)
        {
            EnsureSystemsSortedNoLock();
            count = systems.Count;
            if (count == 0)
            {
                return;
            }
            rentedArray = ArrayPool<SystemEntry>.Shared.Rent(count);
            systems.CopyTo(rentedArray);
        }

        try
        {
            for (int i = 0; i < count; i++)
            {
                var entry = rentedArray[i];
                if (entry.Phase != SystemPhase.FixedUpdate)
                {
                    continue;
                }

                var system = entry.System;

                if (!system.Enabled)
                {
                    continue;
                }

                // Invoke before hooks
                hookManager.InvokeBeforeHooks(system, fixedDeltaTime, entry.Phase);

                if (system is SystemBase systemBase)
                {
                    systemBase.InvokeBeforeUpdate(fixedDeltaTime);
                    systemBase.Update(fixedDeltaTime);
                    systemBase.InvokeAfterUpdate(fixedDeltaTime);
                }
                else
                {
                    system.Update(fixedDeltaTime);
                }

                // Invoke after hooks
                hookManager.InvokeAfterHooks(system, fixedDeltaTime, entry.Phase);
            }
        }
        finally
        {
            ArrayPool<SystemEntry>.Shared.Return(rentedArray);
        }
    }

    /// <summary>
    /// Gets a system of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of system to retrieve.</typeparam>
    /// <returns>The system instance, or null if not found.</returns>
    internal T? GetSystem<T>() where T : class, ISystem
    {
        lock (syncRoot)
        {
            foreach (var entry in systems)
            {
                if (entry.System is T typedSystem)
                {
                    return typedSystem;
                }
                if (entry.System is SystemGroup group)
                {
                    var found = group.GetSystem<T>();
                    if (found is not null)
                    {
                        return found;
                    }
                }
            }
            return null;
        }
    }

    /// <summary>
    /// Enables a system of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of system to enable.</typeparam>
    /// <returns>True if the system was found and enabled; false otherwise.</returns>
    internal bool EnableSystem<T>() where T : class, ISystem
    {
        var system = GetSystem<T>();
        if (system is null)
        {
            return false;
        }
        system.Enabled = true;
        return true;
    }

    /// <summary>
    /// Disables a system of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of system to disable.</typeparam>
    /// <returns>True if the system was found and disabled; false otherwise.</returns>
    internal bool DisableSystem<T>() where T : class, ISystem
    {
        var system = GetSystem<T>();
        if (system is null)
        {
            return false;
        }
        system.Enabled = false;
        return true;
    }

    /// <summary>
    /// Removes a system from this manager.
    /// </summary>
    /// <param name="system">The system to remove.</param>
    /// <returns>True if the system was found and removed; false otherwise.</returns>
    internal bool RemoveSystem(ISystem system)
    {
        lock (syncRoot)
        {
            for (int i = systems.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(systems[i].System, system))
                {
                    systems.RemoveAt(i);
                    systemsSorted = false;
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Gets the number of systems in this manager.
    /// </summary>
    internal int Count
    {
        get
        {
            lock (syncRoot)
            {
                return systems.Count;
            }
        }
    }

    /// <summary>
    /// Disposes all systems and clears the list.
    /// </summary>
    internal void DisposeAll()
    {
        SystemEntry[] snapshot;
        lock (syncRoot)
        {
            snapshot = [.. systems];
            systems.Clear();
            systemsSorted = true;
        }

        foreach (var entry in snapshot)
        {
            entry.System.Dispose();
        }
    }

    /// <summary>
    /// Clears all systems without disposing them.
    /// Used when systems are disposed by plugins.
    /// </summary>
    internal void Clear()
    {
        lock (syncRoot)
        {
            systems.Clear();
            systemsSorted = true;
        }
    }

    /// <summary>
    /// Ensures systems are sorted by phase and order before iteration.
    /// Uses topological sorting when RunBefore/RunAfter constraints exist.
    /// Must be called while holding syncRoot.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a cycle is detected in system dependencies.
    /// </exception>
    private void EnsureSystemsSortedNoLock()
    {
        if (systemsSorted)
        {
            return;
        }

        // Group systems by phase (allocation-free grouping)
        var phaseGroups = new Dictionary<SystemPhase, List<SystemEntry>>();
        foreach (var entry in systems)
        {
            if (!phaseGroups.TryGetValue(entry.Phase, out var list))
            {
                list = [];
                phaseGroups[entry.Phase] = list;
            }
            list.Add(entry);
        }

        // Sort phases for deterministic ordering
        var sortedPhases = phaseGroups.Keys.ToList();
        sortedPhases.Sort();

        var sortedSystems = new List<SystemEntry>(systems.Count);

        foreach (var phase in sortedPhases)
        {
            var phaseSystems = phaseGroups[phase];
            var sorted = TopologicalSortWithCycleDetection(phaseSystems, phase);
            sortedSystems.AddRange(sorted);
        }

        systems.Clear();
        systems.AddRange(sortedSystems);
        systemsSorted = true;
    }

    /// <summary>
    /// Performs topological sort on systems within a phase, respecting RunBefore/RunAfter constraints.
    /// Falls back to Order-based sorting when no constraints exist.
    /// </summary>
    private static List<SystemEntry> TopologicalSortWithCycleDetection(List<SystemEntry> phaseSystems, SystemPhase phase)
    {
        // Check for self-cycles (system referencing itself)
        foreach (var entry in phaseSystems)
        {
            var systemType = entry.System.GetType();
            if (entry.RunsBefore.Contains(systemType) || entry.RunsAfter.Contains(systemType))
            {
                throw new InvalidOperationException(
                    $"Cycle detected in system dependencies for phase {phase}. " +
                    $"Systems involved: {systemType.Name}");
            }
        }

        if (phaseSystems.Count == 1)
        {
            return phaseSystems;
        }

        // Build type-to-entry mapping for systems in this phase
        var typeToEntry = new Dictionary<Type, SystemEntry>();
        foreach (var entry in phaseSystems)
        {
            var systemType = entry.System.GetType();
            typeToEntry[systemType] = entry;
        }

        // Build adjacency list: key must run before all values
        var graph = new Dictionary<SystemEntry, HashSet<SystemEntry>>();
        var inDegree = new Dictionary<SystemEntry, int>();

        foreach (var entry in phaseSystems)
        {
            graph[entry] = [];
            inDegree[entry] = 0;
        }

        // Process RunsBefore constraints: if A.RunsBefore contains B, then A → B (A before B)
        foreach (var entry in phaseSystems)
        {
            foreach (var targetType in entry.RunsBefore)
            {
                if (typeToEntry.TryGetValue(targetType, out var targetEntry) &&
                    graph[entry].Add(targetEntry))
                {
                    inDegree[targetEntry]++;
                }
            }
        }

        // Process RunsAfter constraints: if A.RunsAfter contains B, then B → A (B before A)
        foreach (var entry in phaseSystems)
        {
            foreach (var targetType in entry.RunsAfter)
            {
                if (typeToEntry.TryGetValue(targetType, out var targetEntry) &&
                    graph[targetEntry].Add(entry))
                {
                    inDegree[entry]++;
                }
            }
        }

        // Check if any constraints exist
        var hasConstraints = phaseSystems.Any(e => e.RunsBefore.Length > 0 || e.RunsAfter.Length > 0);
        if (!hasConstraints)
        {
            // No constraints - use simple Order-based sorting
            return [.. phaseSystems.OrderBy(e => e.Order)];
        }

        // Kahn's algorithm with Order-based tiebreaking
        var result = new List<SystemEntry>(phaseSystems.Count);
        var available = new List<SystemEntry>();

        // Find all entries with no dependencies
        foreach (var entry in phaseSystems)
        {
            if (inDegree[entry] == 0)
            {
                available.Add(entry);
            }
        }

        while (available.Count > 0)
        {
            // Sort available by Order for stable, deterministic results
            available.Sort((a, b) => a.Order.CompareTo(b.Order));

            var current = available[0];
            available.RemoveAt(0);
            result.Add(current);

            foreach (var neighbor in graph[current])
            {
                inDegree[neighbor]--;
                if (inDegree[neighbor] == 0)
                {
                    available.Add(neighbor);
                }
            }
        }

        // If we didn't process all systems, there's a cycle
        if (result.Count != phaseSystems.Count)
        {
            // Find systems involved in the cycle for better error message
            var cycleParticipants = phaseSystems
                .Where(e => inDegree[e] > 0)
                .Select(e => e.System.GetType().Name);
            throw new InvalidOperationException(
                $"Cycle detected in system dependencies for phase {phase}. " +
                $"Systems involved: {string.Join(", ", cycleParticipants)}");
        }

        return result;
    }

    /// <summary>
    /// Internal record for storing system with its execution metadata.
    /// </summary>
    private sealed record SystemEntry(
        ISystem System,
        SystemPhase Phase,
        int Order,
        Type[] RunsBefore,
        Type[] RunsAfter);
}
