// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

namespace KeenEyes.Editor.Plugins.Dependencies;

/// <summary>
/// Represents a directed dependency graph for plugins.
/// </summary>
/// <remarks>
/// <para>
/// The graph stores edges from dependencies to dependents.
/// For example, if plugin A depends on plugin B, an edge B → A is created
/// (B must be loaded before A).
/// </para>
/// <para>
/// Uses Kahn's algorithm for topological sorting and cycle detection.
/// </para>
/// </remarks>
internal sealed class DependencyGraph
{
    private readonly Dictionary<string, HashSet<string>> adjacencyList = [];
    private readonly Dictionary<string, int> inDegree = [];

    /// <summary>
    /// Gets the number of plugins in the graph.
    /// </summary>
    public int PluginCount => adjacencyList.Count;

    /// <summary>
    /// Adds a plugin to the graph.
    /// </summary>
    /// <param name="pluginId">The plugin ID to add.</param>
    public void AddPlugin(string pluginId)
    {
        if (!adjacencyList.ContainsKey(pluginId))
        {
            adjacencyList[pluginId] = [];
            inDegree[pluginId] = 0;
        }
    }

    /// <summary>
    /// Adds a dependency edge to the graph.
    /// </summary>
    /// <param name="dependencyId">The ID of the dependency (loaded first).</param>
    /// <param name="dependentId">The ID of the dependent (loaded after).</param>
    /// <remarks>
    /// Creates an edge from dependency → dependent, meaning the dependency
    /// must be loaded before the dependent.
    /// </remarks>
    public void AddDependency(string dependencyId, string dependentId)
    {
        // Ensure both plugins are in the graph
        AddPlugin(dependencyId);
        AddPlugin(dependentId);

        // Add edge: dependency → dependent
        if (adjacencyList[dependencyId].Add(dependentId))
        {
            inDegree[dependentId]++;
        }
    }

    /// <summary>
    /// Gets the direct dependents of a plugin (plugins that depend on it).
    /// </summary>
    /// <param name="pluginId">The plugin ID.</param>
    /// <returns>The IDs of plugins that directly depend on this plugin.</returns>
    public IReadOnlySet<string> GetDependents(string pluginId)
    {
        if (adjacencyList.TryGetValue(pluginId, out var dependents))
        {
            return dependents;
        }

        return new HashSet<string>();
    }

    /// <summary>
    /// Performs topological sort using Kahn's algorithm.
    /// </summary>
    /// <returns>
    /// A tuple containing the sorted order (or partial order if cycle exists)
    /// and the IDs of plugins involved in any cycle.
    /// </returns>
    public (IReadOnlyList<string> Order, IReadOnlyList<string> CycleParticipants) TopologicalSort()
    {
        if (adjacencyList.Count == 0)
        {
            return ([], []);
        }

        // Create working copy of in-degrees
        var workingInDegree = new Dictionary<string, int>(inDegree);

        var result = new List<string>(adjacencyList.Count);
        var available = new List<string>();

        // Find all plugins with no dependencies
        foreach (var (pluginId, degree) in workingInDegree)
        {
            if (degree == 0)
            {
                available.Add(pluginId);
            }
        }

        while (available.Count > 0)
        {
            // Sort for deterministic results
            available.Sort(StringComparer.Ordinal);

            var current = available[0];
            available.RemoveAt(0);
            result.Add(current);

            foreach (var neighbor in adjacencyList[current])
            {
                workingInDegree[neighbor]--;
                if (workingInDegree[neighbor] == 0)
                {
                    available.Add(neighbor);
                }
            }
        }

        // If we didn't process all plugins, there's a cycle
        if (result.Count != adjacencyList.Count)
        {
            var cycleParticipants = workingInDegree
                .Where(kvp => kvp.Value > 0)
                .Select(kvp => kvp.Key)
                .OrderBy(id => id)
                .ToList();

            return (result, cycleParticipants);
        }

        return (result, []);
    }

    /// <summary>
    /// Finds a cycle path starting from a given plugin.
    /// </summary>
    /// <param name="startId">The plugin ID to start from.</param>
    /// <returns>The cycle path, or empty if no cycle is found.</returns>
    public IReadOnlyList<string> FindCyclePath(string startId)
    {
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();
        var path = new List<string>();

        if (FindCyclePathDfs(startId, visited, recursionStack, path))
        {
            // Add the start node again to show the cycle
            path.Add(startId);
            return path;
        }

        return [];
    }

    /// <summary>
    /// Gets all plugins that transitively depend on the specified plugin.
    /// </summary>
    /// <param name="pluginId">The plugin ID.</param>
    /// <returns>All plugins that directly or indirectly depend on this plugin.</returns>
    public IReadOnlySet<string> GetAllDependents(string pluginId)
    {
        var result = new HashSet<string>();
        var queue = new Queue<string>();
        queue.Enqueue(pluginId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (!adjacencyList.TryGetValue(current, out var dependents))
            {
                continue;
            }

            foreach (var dependent in dependents)
            {
                if (result.Add(dependent))
                {
                    queue.Enqueue(dependent);
                }
            }
        }

        return result;
    }

    private bool FindCyclePathDfs(
        string current,
        HashSet<string> visited,
        HashSet<string> recursionStack,
        List<string> path)
    {
        visited.Add(current);
        recursionStack.Add(current);

        if (!adjacencyList.TryGetValue(current, out var neighbors))
        {
            recursionStack.Remove(current);
            return false;
        }

        foreach (var neighbor in neighbors)
        {
            if (!visited.Contains(neighbor))
            {
                if (FindCyclePathDfs(neighbor, visited, recursionStack, path))
                {
                    path.Insert(0, current);
                    return true;
                }
            }
            else if (recursionStack.Contains(neighbor))
            {
                // Found a cycle
                path.Insert(0, neighbor);
                path.Insert(0, current);
                return true;
            }
        }

        recursionStack.Remove(current);
        return false;
    }
}
