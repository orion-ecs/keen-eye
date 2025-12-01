using System.Collections;

namespace KeenEyes;

/// <summary>
/// Describes which components a query includes, requires, and excludes.
/// </summary>
public sealed class QueryDescription
{
    private readonly List<Type> read = [];
    private readonly List<Type> write = [];
    private readonly List<Type> with = [];
    private readonly List<Type> without = [];

    /// <summary>Components that will be read (ref readonly).</summary>
    public IReadOnlyList<Type> Read => read;

    /// <summary>Components that will be written (ref).</summary>
    public IReadOnlyList<Type> Write => write;

    /// <summary>Components that must be present (filter only).</summary>
    public IReadOnlyList<Type> With => with;

    /// <summary>Components that must NOT be present.</summary>
    public IReadOnlyList<Type> Without => without;

    /// <summary>All components that must be present (Read + Write + With).</summary>
    public IEnumerable<Type> AllRequired => read.Concat(write).Concat(with).Distinct();

    internal void AddRead<T>() where T : struct, IComponent => read.Add(typeof(T));
    internal void AddWrite<T>() where T : struct, IComponent => write.Add(typeof(T));
    internal void AddWith<T>() where T : struct, IComponent => with.Add(typeof(T));
    internal void AddWithout<T>() where T : struct, IComponent => without.Add(typeof(T));

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
/// <typeparam name="T1">First component type.</typeparam>
public readonly struct QueryBuilder<T1> : IEnumerable<Entity>
    where T1 : struct, IComponent
{
    private readonly World world;
    private readonly QueryDescription description;

    internal QueryBuilder(World world)
    {
        this.world = world;
        description = new QueryDescription();
        description.AddWrite<T1>();
    }

    private QueryBuilder(World world, QueryDescription description)
    {
        this.world = world;
        this.description = description;
    }

    /// <summary>Requires the entity to have this component (filter only, not accessed).</summary>
    public QueryBuilder<T1> With<TWith>() where TWith : struct, IComponent
    {
        description.AddWith<TWith>();
        return new QueryBuilder<T1>(world, description);
    }

    /// <summary>Excludes entities that have this component.</summary>
    public QueryBuilder<T1> Without<TWithout>() where TWithout : struct, IComponent
    {
        description.AddWithout<TWithout>();
        return new QueryBuilder<T1>(world, description);
    }

    /// <summary>Gets the query description.</summary>
    public QueryDescription Description => description;

    /// <summary>Gets the world this query operates on.</summary>
    public World World => world;

    /// <summary>Gets an enumerator for iterating over matching entities.</summary>
    public QueryEnumerator<T1> GetEnumerator() => new(world, description);
    IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// Fluent builder for constructing queries with two component types.
/// </summary>
public readonly struct QueryBuilder<T1, T2> : IEnumerable<Entity>
    where T1 : struct, IComponent
    where T2 : struct, IComponent
{
    private readonly World world;
    private readonly QueryDescription description;

    internal QueryBuilder(World world)
    {
        this.world = world;
        description = new QueryDescription();
        description.AddWrite<T1>();
        description.AddWrite<T2>();
    }

    private QueryBuilder(World world, QueryDescription description)
    {
        this.world = world;
        this.description = description;
    }

    /// <summary>Requires the entity to have this component (filter only, not accessed).</summary>
    public QueryBuilder<T1, T2> With<TWith>() where TWith : struct, IComponent
    {
        description.AddWith<TWith>();
        return new QueryBuilder<T1, T2>(world, description);
    }

    /// <summary>Excludes entities that have this component.</summary>
    public QueryBuilder<T1, T2> Without<TWithout>() where TWithout : struct, IComponent
    {
        description.AddWithout<TWithout>();
        return new QueryBuilder<T1, T2>(world, description);
    }

    /// <summary>Gets the query description.</summary>
    public QueryDescription Description => description;

    /// <summary>Gets an enumerator for iterating over matching entities.</summary>
    public QueryEnumerator<T1, T2> GetEnumerator() => new(world, description);
    IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// Fluent builder for constructing queries with three component types.
/// </summary>
public readonly struct QueryBuilder<T1, T2, T3> : IEnumerable<Entity>
    where T1 : struct, IComponent
    where T2 : struct, IComponent
    where T3 : struct, IComponent
{
    private readonly World world;
    private readonly QueryDescription description;

    internal QueryBuilder(World world)
    {
        this.world = world;
        description = new QueryDescription();
        description.AddWrite<T1>();
        description.AddWrite<T2>();
        description.AddWrite<T3>();
    }

    private QueryBuilder(World world, QueryDescription description)
    {
        this.world = world;
        this.description = description;
    }

    /// <summary>Requires the entity to have this component (filter only, not accessed).</summary>
    public QueryBuilder<T1, T2, T3> With<TWith>() where TWith : struct, IComponent
    {
        description.AddWith<TWith>();
        return new QueryBuilder<T1, T2, T3>(world, description);
    }

    /// <summary>Excludes entities that have this component.</summary>
    public QueryBuilder<T1, T2, T3> Without<TWithout>() where TWithout : struct, IComponent
    {
        description.AddWithout<TWithout>();
        return new QueryBuilder<T1, T2, T3>(world, description);
    }

    /// <summary>Gets the query description.</summary>
    public QueryDescription Description => description;

    /// <summary>Gets an enumerator for iterating over matching entities.</summary>
    public QueryEnumerator<T1, T2, T3> GetEnumerator() => new(world, description);
    IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// Fluent builder for constructing queries with four component types.
/// </summary>
public readonly struct QueryBuilder<T1, T2, T3, T4> : IEnumerable<Entity>
    where T1 : struct, IComponent
    where T2 : struct, IComponent
    where T3 : struct, IComponent
    where T4 : struct, IComponent
{
    private readonly World world;
    private readonly QueryDescription description;

    internal QueryBuilder(World world)
    {
        this.world = world;
        description = new QueryDescription();
        description.AddWrite<T1>();
        description.AddWrite<T2>();
        description.AddWrite<T3>();
        description.AddWrite<T4>();
    }

    private QueryBuilder(World world, QueryDescription description)
    {
        this.world = world;
        this.description = description;
    }

    /// <summary>Requires the entity to have this component (filter only, not accessed).</summary>
    public QueryBuilder<T1, T2, T3, T4> With<TWith>() where TWith : struct, IComponent
    {
        description.AddWith<TWith>();
        return new QueryBuilder<T1, T2, T3, T4>(world, description);
    }

    /// <summary>Excludes entities that have this component.</summary>
    public QueryBuilder<T1, T2, T3, T4> Without<TWithout>() where TWithout : struct, IComponent
    {
        description.AddWithout<TWithout>();
        return new QueryBuilder<T1, T2, T3, T4>(world, description);
    }

    /// <summary>Gets the query description.</summary>
    public QueryDescription Description => description;

    /// <summary>Gets an enumerator for iterating over matching entities.</summary>
    public QueryEnumerator<T1, T2, T3, T4> GetEnumerator() => new(world, description);
    IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
