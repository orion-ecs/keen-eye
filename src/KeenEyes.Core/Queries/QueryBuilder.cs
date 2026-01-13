using System.Collections;
using System.Collections.Immutable;

namespace KeenEyes;

/// <summary>
/// Describes which components a query includes, requires, and excludes.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Thread Safety:</strong> QueryDescription is NOT thread-safe. Like <see cref="World"/>,
/// all operations must be called from a single thread. The lazy-initialized <see cref="AllRequired"/>
/// cache uses an unsynchronized check-then-act pattern for performance; concurrent modification
/// while accessing this property results in undefined behavior.
/// </para>
/// </remarks>
public sealed class QueryDescription
{
    private readonly List<Type> read = [];
    private readonly List<Type> write = [];
    private readonly List<Type> with = [];
    private readonly List<Type> without = [];
    private readonly List<string> withStringTags = [];
    private readonly List<string> withoutStringTags = [];
    private ImmutableArray<Type>? allRequiredCache;

    /// <summary>Components that will be read (ref readonly).</summary>
    public IReadOnlyList<Type> Read => read;

    /// <summary>Components that will be written (ref).</summary>
    public IReadOnlyList<Type> Write => write;

    /// <summary>Components that must be present (filter only).</summary>
    public IReadOnlyList<Type> With => with;

    /// <summary>Components that must NOT be present.</summary>
    public IReadOnlyList<Type> Without => without;

    /// <summary>String tags that must be present.</summary>
    public IReadOnlyList<string> WithStringTags => withStringTags;

    /// <summary>String tags that must NOT be present.</summary>
    public IReadOnlyList<string> WithoutStringTags => withoutStringTags;

    /// <summary>All components that must be present (Read + Write + With).</summary>
    public ImmutableArray<Type> AllRequired
    {
        get
        {
            if (allRequiredCache is null)
            {
                // Avoid LINQ allocations by using HashSet directly
                var hashSet = new HashSet<Type>();
                foreach (var t in read)
                {
                    hashSet.Add(t);
                }
                foreach (var t in write)
                {
                    hashSet.Add(t);
                }
                foreach (var t in with)
                {
                    hashSet.Add(t);
                }
                allRequiredCache = [.. hashSet];
            }
            return allRequiredCache.Value;
        }
    }

    /// <summary>Whether any string tag filters are applied.</summary>
    public bool HasStringTagFilters => withStringTags.Count > 0 || withoutStringTags.Count > 0;

    /// <summary>
    /// Adds a component type as read access.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    public void AddRead<T>() where T : struct, IComponent
    {
        read.Add(typeof(T));
        allRequiredCache = null; // Invalidate cache
    }

    /// <summary>
    /// Adds a component type as write access.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    public void AddWrite<T>() where T : struct, IComponent
    {
        write.Add(typeof(T));
        allRequiredCache = null; // Invalidate cache
    }

    /// <summary>
    /// Adds a component type as a filter (must be present).
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    public void AddWith<T>() where T : struct, IComponent
    {
        with.Add(typeof(T));
        allRequiredCache = null; // Invalidate cache
    }

    /// <summary>
    /// Adds a component type as an exclusion filter (must NOT be present).
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    public void AddWithout<T>() where T : struct, IComponent => without.Add(typeof(T));

    /// <summary>
    /// Adds a string tag filter (must be present).
    /// </summary>
    /// <param name="tag">The tag that must be present.</param>
    public void AddWithStringTag(string tag) => withStringTags.Add(tag);

    /// <summary>
    /// Adds a string tag exclusion filter (must NOT be present).
    /// </summary>
    /// <param name="tag">The tag that must NOT be present.</param>
    public void AddWithoutStringTag(string tag) => withoutStringTags.Add(tag);

    /// <summary>
    /// Checks if an entity with the given components matches this query.
    /// </summary>
    public bool Matches(IEnumerable<Type> entityComponents)
    {
        var componentSet = entityComponents.ToHashSet();

        // Must have all required components
        foreach (var required in AllRequired)
        {
            if (!componentSet.Contains(required))
            {
                return false;
            }
        }

        // Must not have any excluded components
        foreach (var excluded in without)
        {
            if (componentSet.Contains(excluded))
            {
                return false;
            }
        }

        return true;
    }
}

/// <summary>
/// Fluent builder for constructing queries.
/// </summary>
public readonly struct QueryBuilder : IQueryBuilder
{
    private readonly World world;
    private readonly QueryDescription description;

    internal QueryBuilder(World world)
    {
        this.world = world;
        description = new QueryDescription();
    }

    private QueryBuilder(World world, QueryDescription description)
    {
        this.world = world;
        this.description = description;
    }

    /// <summary>
    /// Adds a component type to query for with write access.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <returns>This builder for chaining.</returns>
    internal QueryBuilder WithWrite<T>() where T : struct, IComponent
    {
        description.AddWrite<T>();
        return this;
    }

    /// <summary>Requires the entity to have this component (filter only, not accessed).</summary>
    public QueryBuilder With<TWith>() where TWith : struct, IComponent
    {
        description.AddWith<TWith>();
        return new QueryBuilder(world, description);
    }

    /// <summary>Excludes entities that have this component.</summary>
    public QueryBuilder Without<TWithout>() where TWithout : struct, IComponent
    {
        description.AddWithout<TWithout>();
        return new QueryBuilder(world, description);
    }

    /// <summary>Requires the entity to have this string tag.</summary>
    /// <param name="tag">The string tag that must be present.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tag"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tag"/> is empty or whitespace.</exception>
    public QueryBuilder WithTag(string tag)
    {
        ValidateTag(tag);
        description.AddWithStringTag(tag);
        return new QueryBuilder(world, description);
    }

    /// <summary>Excludes entities that have this string tag.</summary>
    /// <param name="tag">The string tag that must NOT be present.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tag"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tag"/> is empty or whitespace.</exception>
    public QueryBuilder WithoutTag(string tag)
    {
        ValidateTag(tag);
        description.AddWithoutStringTag(tag);
        return new QueryBuilder(world, description);
    }

    /// <summary>Gets the query description.</summary>
    public QueryDescription Description => description;

    /// <summary>Gets the world this query operates on.</summary>
    public World World => world;

    /// <summary>Gets an enumerator for iterating over matching entities.</summary>
    public QueryEnumerator GetEnumerator() => new(world, description);
    IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Counts the number of entities matching this query.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method is optimized to avoid LINQ overhead and is safe to use in hot paths.
    /// When no string tag filters are applied, it sums archetype counts directly (O(archetypes)).
    /// When string tag filters are present, it must iterate entities (O(entities)).
    /// </para>
    /// </remarks>
    /// <returns>The number of entities matching the query.</returns>
    public int Count()
    {
        var archetypes = world.GetMatchingArchetypes(description);

        // Fast path: no string tag filters, just sum archetype counts
        if (!description.HasStringTagFilters)
        {
            var count = 0;
            for (var i = 0; i < archetypes.Count; i++)
            {
                count += archetypes[i].Count;
            }
            return count;
        }

        // Slow path: need to check string tags per entity
        var total = 0;
        foreach (var _ in this)
        {
            total++;
        }
        return total;
    }

    // Explicit interface implementations
    IQueryBuilder IQueryBuilder.With<TWith>() => With<TWith>();
    IQueryBuilder IQueryBuilder.Without<TWithout>() => Without<TWithout>();
    IQueryBuilder IQueryBuilder.WithTag(string tag) => WithTag(tag);
    IQueryBuilder IQueryBuilder.WithoutTag(string tag) => WithoutTag(tag);
    int IQueryBuilder.Count() => Count();

    private static void ValidateTag(string tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        if (string.IsNullOrWhiteSpace(tag))
        {
            throw new ArgumentException("Tag cannot be empty or whitespace.", nameof(tag));
        }
    }
}
