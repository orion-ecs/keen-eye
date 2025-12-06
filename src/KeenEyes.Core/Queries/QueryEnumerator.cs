using System.Collections;

namespace KeenEyes;

/// <summary>
/// Enumerator for iterating over entities matching a single-component query.
/// Uses archetype-based iteration for cache-friendly performance.
/// </summary>
public struct QueryEnumerator<T1> : IEnumerator<Entity>
    where T1 : struct, IComponent
{
    private readonly World world;
    private readonly IReadOnlyList<Archetype> archetypes;
    private int archetypeIndex;
    private int entityIndex;

    internal QueryEnumerator(World world, QueryDescription description)
    {
        this.world = world;
        archetypes = world.GetMatchingArchetypes(description);
        archetypeIndex = 0;
        entityIndex = -1;
    }

    /// <inheritdoc />
    public Entity Current
    {
        get
        {
            if (archetypeIndex < archetypes.Count)
            {
                return archetypes[archetypeIndex].GetEntity(entityIndex);
            }
            return Entity.Null;
        }
    }

    object IEnumerator.Current => Current;

    /// <inheritdoc />
    public bool MoveNext()
    {
        while (archetypeIndex < archetypes.Count)
        {
            var archetype = archetypes[archetypeIndex];
            entityIndex++;

            if (entityIndex < archetype.Count)
            {
                return true;
            }

            // Move to next archetype
            archetypeIndex++;
            entityIndex = -1;
        }

        return false;
    }

    /// <inheritdoc />
    public void Reset()
    {
        archetypeIndex = 0;
        entityIndex = -1;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // No resources to dispose
    }
}

/// <summary>
/// Enumerator for iterating over entities matching a two-component query.
/// Uses archetype-based iteration for cache-friendly performance.
/// </summary>
public struct QueryEnumerator<T1, T2> : IEnumerator<Entity>
    where T1 : struct, IComponent
    where T2 : struct, IComponent
{
    private readonly World world;
    private readonly IReadOnlyList<Archetype> archetypes;
    private int archetypeIndex;
    private int entityIndex;

    internal QueryEnumerator(World world, QueryDescription description)
    {
        this.world = world;
        archetypes = world.GetMatchingArchetypes(description);
        archetypeIndex = 0;
        entityIndex = -1;
    }

    /// <inheritdoc />
    public Entity Current
    {
        get
        {
            if (archetypeIndex < archetypes.Count)
            {
                return archetypes[archetypeIndex].GetEntity(entityIndex);
            }
            return Entity.Null;
        }
    }

    object IEnumerator.Current => Current;

    /// <inheritdoc />
    public bool MoveNext()
    {
        while (archetypeIndex < archetypes.Count)
        {
            var archetype = archetypes[archetypeIndex];
            entityIndex++;

            if (entityIndex < archetype.Count)
            {
                return true;
            }

            // Move to next archetype
            archetypeIndex++;
            entityIndex = -1;
        }

        return false;
    }

    /// <inheritdoc />
    public void Reset()
    {
        archetypeIndex = 0;
        entityIndex = -1;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // No resources to dispose
    }
}

/// <summary>
/// Enumerator for iterating over entities matching a three-component query.
/// Uses archetype-based iteration for cache-friendly performance.
/// </summary>
public struct QueryEnumerator<T1, T2, T3> : IEnumerator<Entity>
    where T1 : struct, IComponent
    where T2 : struct, IComponent
    where T3 : struct, IComponent
{
    private readonly World world;
    private readonly IReadOnlyList<Archetype> archetypes;
    private int archetypeIndex;
    private int entityIndex;

    internal QueryEnumerator(World world, QueryDescription description)
    {
        this.world = world;
        archetypes = world.GetMatchingArchetypes(description);
        archetypeIndex = 0;
        entityIndex = -1;
    }

    /// <inheritdoc />
    public Entity Current
    {
        get
        {
            if (archetypeIndex < archetypes.Count)
            {
                return archetypes[archetypeIndex].GetEntity(entityIndex);
            }
            return Entity.Null;
        }
    }

    object IEnumerator.Current => Current;

    /// <inheritdoc />
    public bool MoveNext()
    {
        while (archetypeIndex < archetypes.Count)
        {
            var archetype = archetypes[archetypeIndex];
            entityIndex++;

            if (entityIndex < archetype.Count)
            {
                return true;
            }

            // Move to next archetype
            archetypeIndex++;
            entityIndex = -1;
        }

        return false;
    }

    /// <inheritdoc />
    public void Reset()
    {
        archetypeIndex = 0;
        entityIndex = -1;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // No resources to dispose
    }
}

/// <summary>
/// Enumerator for iterating over entities matching a four-component query.
/// Uses archetype-based iteration for cache-friendly performance.
/// </summary>
public struct QueryEnumerator<T1, T2, T3, T4> : IEnumerator<Entity>
    where T1 : struct, IComponent
    where T2 : struct, IComponent
    where T3 : struct, IComponent
    where T4 : struct, IComponent
{
    private readonly World world;
    private readonly IReadOnlyList<Archetype> archetypes;
    private int archetypeIndex;
    private int entityIndex;

    internal QueryEnumerator(World world, QueryDescription description)
    {
        this.world = world;
        archetypes = world.GetMatchingArchetypes(description);
        archetypeIndex = 0;
        entityIndex = -1;
    }

    /// <inheritdoc />
    public Entity Current
    {
        get
        {
            if (archetypeIndex < archetypes.Count)
            {
                return archetypes[archetypeIndex].GetEntity(entityIndex);
            }
            return Entity.Null;
        }
    }

    object IEnumerator.Current => Current;

    /// <inheritdoc />
    public bool MoveNext()
    {
        while (archetypeIndex < archetypes.Count)
        {
            var archetype = archetypes[archetypeIndex];
            entityIndex++;

            if (entityIndex < archetype.Count)
            {
                return true;
            }

            // Move to next archetype
            archetypeIndex++;
            entityIndex = -1;
        }

        return false;
    }

    /// <inheritdoc />
    public void Reset()
    {
        archetypeIndex = 0;
        entityIndex = -1;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // No resources to dispose
    }
}
