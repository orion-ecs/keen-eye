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
    private readonly IReadOnlyList<string> withStringTags;
    private readonly IReadOnlyList<string> withoutStringTags;
    private readonly bool hasStringTagFilters;
    private int archetypeIndex;
    private int entityIndex;

    internal QueryEnumerator(World world, QueryDescription description)
    {
        this.world = world;
        archetypes = world.GetMatchingArchetypes(description);
        withStringTags = description.WithStringTags;
        withoutStringTags = description.WithoutStringTags;
        hasStringTagFilters = description.HasStringTagFilters;
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
                // Check string tag filters if any
                if (hasStringTagFilters)
                {
                    var entity = archetype.GetEntity(entityIndex);
                    if (!MatchesStringTags(entity))
                    {
                        continue; // Skip this entity, try next
                    }
                }
                return true;
            }

            // Move to next archetype
            archetypeIndex++;
            entityIndex = -1;
        }

        return false;
    }

    private readonly bool MatchesStringTags(Entity entity)
    {
        // Capture world in local to allow lambda access in struct
        var w = world;
        // All required tags must be present, and no excluded tags can be present
        return withStringTags.All(tag => w.HasTag(entity, tag))
            && !withoutStringTags.Any(tag => w.HasTag(entity, tag));
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
    private readonly IReadOnlyList<string> withStringTags;
    private readonly IReadOnlyList<string> withoutStringTags;
    private readonly bool hasStringTagFilters;
    private int archetypeIndex;
    private int entityIndex;

    internal QueryEnumerator(World world, QueryDescription description)
    {
        this.world = world;
        archetypes = world.GetMatchingArchetypes(description);
        withStringTags = description.WithStringTags;
        withoutStringTags = description.WithoutStringTags;
        hasStringTagFilters = description.HasStringTagFilters;
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
                // Check string tag filters if any
                if (hasStringTagFilters)
                {
                    var entity = archetype.GetEntity(entityIndex);
                    if (!MatchesStringTags(entity))
                    {
                        continue; // Skip this entity, try next
                    }
                }
                return true;
            }

            // Move to next archetype
            archetypeIndex++;
            entityIndex = -1;
        }

        return false;
    }

    private readonly bool MatchesStringTags(Entity entity)
    {
        // Capture world in local to allow lambda access in struct
        var w = world;
        // All required tags must be present, and no excluded tags can be present
        return withStringTags.All(tag => w.HasTag(entity, tag))
            && !withoutStringTags.Any(tag => w.HasTag(entity, tag));
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
    private readonly IReadOnlyList<string> withStringTags;
    private readonly IReadOnlyList<string> withoutStringTags;
    private readonly bool hasStringTagFilters;
    private int archetypeIndex;
    private int entityIndex;

    internal QueryEnumerator(World world, QueryDescription description)
    {
        this.world = world;
        archetypes = world.GetMatchingArchetypes(description);
        withStringTags = description.WithStringTags;
        withoutStringTags = description.WithoutStringTags;
        hasStringTagFilters = description.HasStringTagFilters;
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
                // Check string tag filters if any
                if (hasStringTagFilters)
                {
                    var entity = archetype.GetEntity(entityIndex);
                    if (!MatchesStringTags(entity))
                    {
                        continue; // Skip this entity, try next
                    }
                }
                return true;
            }

            // Move to next archetype
            archetypeIndex++;
            entityIndex = -1;
        }

        return false;
    }

    private readonly bool MatchesStringTags(Entity entity)
    {
        // Capture world in local to allow lambda access in struct
        var w = world;
        // All required tags must be present, and no excluded tags can be present
        return withStringTags.All(tag => w.HasTag(entity, tag))
            && !withoutStringTags.Any(tag => w.HasTag(entity, tag));
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
    private readonly IReadOnlyList<string> withStringTags;
    private readonly IReadOnlyList<string> withoutStringTags;
    private readonly bool hasStringTagFilters;
    private int archetypeIndex;
    private int entityIndex;

    internal QueryEnumerator(World world, QueryDescription description)
    {
        this.world = world;
        archetypes = world.GetMatchingArchetypes(description);
        withStringTags = description.WithStringTags;
        withoutStringTags = description.WithoutStringTags;
        hasStringTagFilters = description.HasStringTagFilters;
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
                // Check string tag filters if any
                if (hasStringTagFilters)
                {
                    var entity = archetype.GetEntity(entityIndex);
                    if (!MatchesStringTags(entity))
                    {
                        continue; // Skip this entity, try next
                    }
                }
                return true;
            }

            // Move to next archetype
            archetypeIndex++;
            entityIndex = -1;
        }

        return false;
    }

    private readonly bool MatchesStringTags(Entity entity)
    {
        // Capture world in local to allow lambda access in struct
        var w = world;
        // All required tags must be present, and no excluded tags can be present
        return withStringTags.All(tag => w.HasTag(entity, tag))
            && !withoutStringTags.Any(tag => w.HasTag(entity, tag));
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
