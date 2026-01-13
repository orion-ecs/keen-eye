using System.Collections.Immutable;

namespace KeenEyes;

/// <summary>
/// Represents the component read/write dependencies of a system.
/// </summary>
/// <remarks>
/// <para>
/// Component dependencies are used by the parallel system scheduler to determine
/// which systems can execute concurrently. Systems that don't have conflicting
/// dependencies (no write-write or read-write conflicts on the same component)
/// can run in parallel.
/// </para>
/// <para>
/// Dependencies can be inferred from registered queries or declared explicitly.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // A system that reads Position and writes Velocity
/// var deps = new ComponentDependencies(
///     reads: [typeof(Position)],
///     writes: [typeof(Velocity)]
/// );
///
/// // Check for conflicts with another system
/// if (deps.ConflictsWith(otherSystemDeps))
/// {
///     // Cannot run in parallel
/// }
/// </code>
/// </example>
public sealed class ComponentDependencies
{
    /// <summary>
    /// Empty dependencies - no component access.
    /// </summary>
    public static readonly ComponentDependencies Empty = new([], []);

    private readonly ImmutableHashSet<Type> readsSet;
    private readonly ImmutableHashSet<Type> writesSet;
    private readonly ImmutableHashSet<Type> allAccessedSet;

    /// <summary>
    /// Creates a new ComponentDependencies with the specified read and write types.
    /// </summary>
    /// <param name="reads">Component types that are read.</param>
    /// <param name="writes">Component types that are written.</param>
    public ComponentDependencies(IEnumerable<Type> reads, IEnumerable<Type> writes)
    {
        readsSet = reads.ToImmutableHashSet();
        writesSet = writes.ToImmutableHashSet();
        allAccessedSet = readsSet.Union(writesSet);
    }

    /// <summary>
    /// Gets the component types that are read by this system.
    /// </summary>
    public IReadOnlyCollection<Type> Reads => readsSet;

    /// <summary>
    /// Gets the component types that are written by this system.
    /// </summary>
    public IReadOnlyCollection<Type> Writes => writesSet;

    /// <summary>
    /// Gets all component types accessed by this system (reads + writes).
    /// </summary>
    public IReadOnlyCollection<Type> AllAccessed => allAccessedSet;

    /// <summary>
    /// Creates dependencies from a query description.
    /// </summary>
    /// <param name="description">The query description.</param>
    /// <returns>Component dependencies derived from the query.</returns>
    public static ComponentDependencies FromQuery(QueryDescription description)
    {
        return new ComponentDependencies(description.Read, description.Write);
    }

    /// <summary>
    /// Creates dependencies from multiple query descriptions.
    /// </summary>
    /// <param name="descriptions">The query descriptions.</param>
    /// <returns>Component dependencies merged from all queries.</returns>
    public static ComponentDependencies FromQueries(IEnumerable<QueryDescription> descriptions)
    {
        var allReads = new HashSet<Type>();
        var allWrites = new HashSet<Type>();

        foreach (var desc in descriptions)
        {
            foreach (var read in desc.Read)
            {
                allReads.Add(read);
            }

            foreach (var write in desc.Write)
            {
                allWrites.Add(write);
            }
        }

        return new ComponentDependencies(allReads, allWrites);
    }

    /// <summary>
    /// Checks if this system has a conflict with another system's dependencies.
    /// </summary>
    /// <param name="other">The other system's dependencies.</param>
    /// <returns>True if there is a conflict that prevents parallel execution.</returns>
    /// <remarks>
    /// <para>
    /// A conflict exists when:
    /// - Both systems write to the same component (write-write conflict)
    /// - One system writes and the other reads the same component (read-write conflict)
    /// </para>
    /// <para>
    /// Two systems that only read the same components can run in parallel.
    /// </para>
    /// </remarks>
    public bool ConflictsWith(ComponentDependencies other)
    {
        // Write-write conflict: both write to the same component
        if (!writesSet.IsEmpty && !other.writesSet.IsEmpty && writesSet.Overlaps(other.writesSet))
        {
            return true;
        }

        // Read-write conflict: one reads what the other writes
        if (!writesSet.IsEmpty && !other.readsSet.IsEmpty && writesSet.Overlaps(other.readsSet))
        {
            return true;
        }

        if (!readsSet.IsEmpty && !other.writesSet.IsEmpty && readsSet.Overlaps(other.writesSet))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the conflicting component types with another system.
    /// </summary>
    /// <param name="other">The other system's dependencies.</param>
    /// <returns>The set of component types that cause conflicts.</returns>
    public IReadOnlyCollection<Type> GetConflictingComponents(ComponentDependencies other)
    {
        var conflicts = new HashSet<Type>();

        // Write-write conflicts (iterate smaller set, check against larger)
        if (writesSet.Overlaps(other.writesSet))
        {
            foreach (var type in writesSet)
            {
                if (other.writesSet.Contains(type))
                {
                    conflicts.Add(type);
                }
            }
        }

        // Read-write conflicts (this writes, other reads)
        if (writesSet.Overlaps(other.readsSet))
        {
            foreach (var type in writesSet)
            {
                if (other.readsSet.Contains(type))
                {
                    conflicts.Add(type);
                }
            }
        }

        // Write-read conflicts (this reads, other writes)
        if (readsSet.Overlaps(other.writesSet))
        {
            foreach (var type in readsSet)
            {
                if (other.writesSet.Contains(type))
                {
                    conflicts.Add(type);
                }
            }
        }

        return conflicts;
    }

    /// <summary>
    /// Merges this dependencies with another, returning a new combined instance.
    /// </summary>
    /// <param name="other">The other dependencies to merge.</param>
    /// <returns>A new ComponentDependencies with all reads and writes from both.</returns>
    public ComponentDependencies Merge(ComponentDependencies other)
    {
        return new ComponentDependencies(
            readsSet.Union(other.readsSet),
            writesSet.Union(other.writesSet)
        );
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var readNames = string.Join(", ", readsSet.Select(t => t.Name));
        var writeNames = string.Join(", ", writesSet.Select(t => t.Name));
        return $"Reads: [{readNames}], Writes: [{writeNames}]";
    }
}
