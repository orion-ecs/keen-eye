using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace KeenEyes.Serialization;

/// <summary>
/// Represents a migration graph for a single component type.
/// </summary>
/// <remarks>
/// <para>
/// A migration graph models the available version transitions for a component.
/// Each edge represents a migration from one version to another (e.g., v1 → v2).
/// </para>
/// <para>
/// The graph provides:
/// <list type="bullet">
/// <item><description>Path validation with caching for efficient repeated lookups</description></item>
/// <item><description>Topological chain resolution for multi-step migrations</description></item>
/// <item><description>Cycle detection (though cycles are structurally impossible with version-incrementing migrations)</description></item>
/// <item><description>Diagnostic visualization of available migration paths</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var graph = new MigrationGraph("MyComponent");
/// graph.AddEdge(1, 2); // v1 → v2 migration exists
/// graph.AddEdge(2, 3); // v2 → v3 migration exists
///
/// // Check if we can migrate from v1 to v3
/// if (graph.HasPath(1, 3))
/// {
///     var chain = graph.GetMigrationChain(1, 3);
///     // chain = [(1, 2), (2, 3)]
/// }
/// </code>
/// </example>
/// <summary>
/// Initializes a new instance of the <see cref="MigrationGraph"/> class.
/// </summary>
/// <param name="componentTypeName">The fully-qualified name of the component type.</param>
/// <param name="currentVersion">The current version of the component (default is 1).</param>
public sealed class MigrationGraph(string componentTypeName, int currentVersion = 1)
{
    private readonly Dictionary<int, HashSet<int>> adjacencyList = [];
    private readonly ConcurrentDictionary<(int, int), bool> pathCache = [];
    private readonly ConcurrentDictionary<(int, int), IReadOnlyList<MigrationStep>> chainCache = [];
    private int version = currentVersion;

    /// <summary>
    /// Gets the component type name this graph represents.
    /// </summary>
    public string ComponentTypeName => componentTypeName;

    /// <summary>
    /// Gets the current version of the component.
    /// </summary>
    public int CurrentVersion => version;

    /// <summary>
    /// Gets all source versions that have migrations defined.
    /// </summary>
    public IEnumerable<int> SourceVersions => adjacencyList.Keys.OrderBy(v => v);

    /// <summary>
    /// Gets the total number of migration edges in the graph.
    /// </summary>
    public int EdgeCount => adjacencyList.Values.Sum(targets => targets.Count);

    /// <summary>
    /// Adds a migration edge from one version to another.
    /// </summary>
    /// <param name="fromVersion">The source version.</param>
    /// <param name="toVersion">The target version (typically fromVersion + 1).</param>
    /// <exception cref="ArgumentException">
    /// Thrown when fromVersion >= toVersion (migrations must go forward).
    /// </exception>
    public void AddEdge(int fromVersion, int toVersion)
    {
        if (fromVersion >= toVersion)
        {
            throw new ArgumentException(
                $"Migration must go forward: {fromVersion} → {toVersion} is invalid.",
                nameof(toVersion));
        }

        if (!adjacencyList.TryGetValue(fromVersion, out var targets))
        {
            targets = [];
            adjacencyList[fromVersion] = targets;
        }

        if (targets.Add(toVersion))
        {
            // Clear caches when graph structure changes
            pathCache.Clear();
            chainCache.Clear();
        }
    }

    /// <summary>
    /// Sets the current version of the component.
    /// </summary>
    /// <param name="newVersion">The current version number.</param>
    public void SetCurrentVersion(int newVersion)
    {
        if (newVersion != version)
        {
            version = newVersion;
            pathCache.Clear();
            chainCache.Clear();
        }
    }

    /// <summary>
    /// Checks if a migration path exists from one version to another.
    /// </summary>
    /// <param name="fromVersion">The source version.</param>
    /// <param name="toVersion">The target version.</param>
    /// <returns>
    /// <c>true</c> if a complete migration chain exists; <c>false</c> otherwise.
    /// </returns>
    /// <remarks>
    /// Results are cached for performance. Use <see cref="ClearCache"/> to invalidate.
    /// </remarks>
    public bool HasPath(int fromVersion, int toVersion)
    {
        if (fromVersion >= toVersion)
        {
            return false;
        }

        return pathCache.GetOrAdd((fromVersion, toVersion), key => ComputeHasPath(key.Item1, key.Item2));
    }

    /// <summary>
    /// Gets the migration chain required to migrate from one version to another.
    /// </summary>
    /// <param name="fromVersion">The source version.</param>
    /// <param name="toVersion">The target version.</param>
    /// <returns>
    /// A list of migration steps representing the shortest path, or an empty list if no path exists.
    /// </returns>
    /// <remarks>
    /// <para>
    /// For standard version-incrementing migrations (v1→v2→v3), this returns the linear chain.
    /// Results are cached for performance.
    /// </para>
    /// </remarks>
    public IReadOnlyList<MigrationStep> GetMigrationChain(int fromVersion, int toVersion)
    {
        if (fromVersion >= toVersion)
        {
            return [];
        }

        return chainCache.GetOrAdd((fromVersion, toVersion), key => ComputeMigrationChain(key.Item1, key.Item2));
    }

    /// <summary>
    /// Finds any gaps in the migration chain from version 1 to the current version.
    /// </summary>
    /// <returns>
    /// A list of version numbers that are missing migrations (i.e., no edge from v to v+1).
    /// </returns>
    public IReadOnlyList<int> FindGaps()
    {
        var gaps = new List<int>();
        for (var v = 1; v < version; v++)
        {
            if (!adjacencyList.TryGetValue(v, out var targets) || !targets.Contains(v + 1))
            {
                gaps.Add(v);
            }
        }
        return gaps;
    }

    /// <summary>
    /// Checks if the graph contains any cycles.
    /// </summary>
    /// <returns>
    /// <c>true</c> if a cycle is detected; <c>false</c> otherwise.
    /// </returns>
    /// <remarks>
    /// With version-incrementing migrations (fromVersion &lt; toVersion), cycles are
    /// structurally impossible. This method is provided for validation completeness.
    /// </remarks>
    public bool HasCycle()
    {
        // With the constraint that fromVersion < toVersion, cycles are impossible
        // because version numbers always increase. However, we verify this property.
        var visited = new HashSet<int>();
        var inStack = new HashSet<int>();

        foreach (var vertex in adjacencyList.Keys)
        {
            if (HasCycleDfs(vertex, visited, inStack))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Clears the path and chain caches.
    /// </summary>
    public void ClearCache()
    {
        pathCache.Clear();
        chainCache.Clear();
    }

    /// <summary>
    /// Generates a diagnostic string representation of the migration graph.
    /// </summary>
    /// <returns>A multi-line string describing the graph structure.</returns>
    public string ToDiagnosticString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Migration Graph for {componentTypeName}");
        sb.AppendLine($"Current Version: {version}");
        sb.AppendLine($"Edges: {EdgeCount}");
        sb.AppendLine();

        if (adjacencyList.Count == 0)
        {
            sb.AppendLine("  (no migrations defined)");
            return sb.ToString();
        }

        sb.AppendLine("Migrations:");
        foreach (var fromVersion in adjacencyList.Keys.OrderBy(v => v))
        {
            foreach (var toVersion in adjacencyList[fromVersion].OrderBy(v => v))
            {
                sb.AppendLine($"  v{fromVersion} → v{toVersion}");
            }
        }

        var gaps = FindGaps();
        if (gaps.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Gaps (missing migrations):");
            foreach (var gap in gaps)
            {
                sb.AppendLine($"  v{gap} → v{gap + 1} MISSING");
            }
        }

        return sb.ToString();
    }

    private bool ComputeHasPath(int from, int to)
    {
        // For linear chains (v → v+1 → v+2), use simple iteration
        for (var v = from; v < to; v++)
        {
            if (!adjacencyList.TryGetValue(v, out var targets) || !targets.Contains(v + 1))
            {
                return false;
            }
        }
        return true;
    }

    private IReadOnlyList<MigrationStep> ComputeMigrationChain(int from, int to)
    {
        // For linear chains, build the step list directly
        var chain = new List<MigrationStep>();
        for (var v = from; v < to; v++)
        {
            if (!adjacencyList.TryGetValue(v, out var targets) || !targets.Contains(v + 1))
            {
                // Gap found - return empty chain
                return [];
            }
            chain.Add(new MigrationStep(v, v + 1));
        }
        return chain;
    }

    private bool HasCycleDfs(int vertex, HashSet<int> visited, HashSet<int> inStack)
    {
        if (inStack.Contains(vertex))
        {
            return true; // Back edge found = cycle
        }

        if (visited.Contains(vertex))
        {
            return false; // Already processed
        }

        visited.Add(vertex);
        inStack.Add(vertex);

        if (adjacencyList.TryGetValue(vertex, out var neighbors))
        {
            foreach (var neighbor in neighbors)
            {
                if (HasCycleDfs(neighbor, visited, inStack))
                {
                    return true;
                }
            }
        }

        inStack.Remove(vertex);
        return false;
    }
}

/// <summary>
/// Represents a single step in a migration chain.
/// </summary>
/// <param name="FromVersion">The source version.</param>
/// <param name="ToVersion">The target version.</param>
public readonly record struct MigrationStep(int FromVersion, int ToVersion)
{
    /// <summary>
    /// Returns a string representation of the migration step.
    /// </summary>
    public override string ToString() => $"v{FromVersion} → v{ToVersion}";
}
