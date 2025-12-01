using System.Collections;

namespace KeenEyes;

/// <summary>
/// Enumerator for iterating over entities matching a single-component query.
/// </summary>
public struct QueryEnumerator<T1> : IEnumerator<Entity>
    where T1 : struct, IComponent
{
    private readonly World world;
    private readonly QueryDescription description;
    private readonly IEnumerator<Entity> entityEnumerator;

    internal QueryEnumerator(World world, QueryDescription description)
    {
        this.world = world;
        this.description = description;
        // TODO: Replace with actual archetype iteration
        entityEnumerator = world.GetMatchingEntities(description).GetEnumerator();
    }

    /// <inheritdoc />
    public Entity Current => entityEnumerator.Current;
    object IEnumerator.Current => Current;

    /// <inheritdoc />
    public bool MoveNext() => entityEnumerator.MoveNext();
    /// <inheritdoc />
    public void Reset() => entityEnumerator.Reset();
    /// <inheritdoc />
    public void Dispose() => entityEnumerator.Dispose();
}

/// <summary>
/// Enumerator for iterating over entities matching a two-component query.
/// </summary>
public struct QueryEnumerator<T1, T2> : IEnumerator<Entity>
    where T1 : struct, IComponent
    where T2 : struct, IComponent
{
    private readonly World world;
    private readonly QueryDescription description;
    private readonly IEnumerator<Entity> entityEnumerator;

    internal QueryEnumerator(World world, QueryDescription description)
    {
        this.world = world;
        this.description = description;
        entityEnumerator = world.GetMatchingEntities(description).GetEnumerator();
    }

    /// <inheritdoc />
    public Entity Current => entityEnumerator.Current;
    object IEnumerator.Current => Current;

    /// <inheritdoc />
    public bool MoveNext() => entityEnumerator.MoveNext();
    /// <inheritdoc />
    public void Reset() => entityEnumerator.Reset();
    /// <inheritdoc />
    public void Dispose() => entityEnumerator.Dispose();
}

/// <summary>
/// Enumerator for iterating over entities matching a three-component query.
/// </summary>
public struct QueryEnumerator<T1, T2, T3> : IEnumerator<Entity>
    where T1 : struct, IComponent
    where T2 : struct, IComponent
    where T3 : struct, IComponent
{
    private readonly World world;
    private readonly QueryDescription description;
    private readonly IEnumerator<Entity> entityEnumerator;

    internal QueryEnumerator(World world, QueryDescription description)
    {
        this.world = world;
        this.description = description;
        entityEnumerator = world.GetMatchingEntities(description).GetEnumerator();
    }

    /// <inheritdoc />
    public Entity Current => entityEnumerator.Current;
    object IEnumerator.Current => Current;

    /// <inheritdoc />
    public bool MoveNext() => entityEnumerator.MoveNext();
    /// <inheritdoc />
    public void Reset() => entityEnumerator.Reset();
    /// <inheritdoc />
    public void Dispose() => entityEnumerator.Dispose();
}

/// <summary>
/// Enumerator for iterating over entities matching a four-component query.
/// </summary>
public struct QueryEnumerator<T1, T2, T3, T4> : IEnumerator<Entity>
    where T1 : struct, IComponent
    where T2 : struct, IComponent
    where T3 : struct, IComponent
    where T4 : struct, IComponent
{
    private readonly World world;
    private readonly QueryDescription description;
    private readonly IEnumerator<Entity> entityEnumerator;

    internal QueryEnumerator(World world, QueryDescription description)
    {
        this.world = world;
        this.description = description;
        entityEnumerator = world.GetMatchingEntities(description).GetEnumerator();
    }

    /// <inheritdoc />
    public Entity Current => entityEnumerator.Current;
    object IEnumerator.Current => Current;

    /// <inheritdoc />
    public bool MoveNext() => entityEnumerator.MoveNext();
    /// <inheritdoc />
    public void Reset() => entityEnumerator.Reset();
    /// <inheritdoc />
    public void Dispose() => entityEnumerator.Dispose();
}
