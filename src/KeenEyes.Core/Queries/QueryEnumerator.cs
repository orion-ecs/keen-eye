using System.Collections;
using System.Runtime.CompilerServices;

namespace KeenEyes;

/// <summary>
/// Enumerator for iterating over entities matching a query.
/// Uses archetype-based iteration for cache-friendly performance.
/// </summary>
public struct QueryEnumerator : IEnumerator<Entity>
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
    public readonly Entity Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (archetypeIndex < archetypes.Count)
            {
                return archetypes[archetypeIndex].GetEntity(entityIndex);
            }
            return Entity.Null;
        }
    }

    readonly object IEnumerator.Current => Current;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        // All required tags must be present
        foreach (var tag in withStringTags)
        {
            if (!world.HasTag(entity, tag))
            {
                return false;
            }
        }

        // No excluded tags can be present
        foreach (var tag in withoutStringTags)
        {
            if (world.HasTag(entity, tag))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public void Reset()
    {
        archetypeIndex = 0;
        entityIndex = -1;
    }

    /// <inheritdoc />
    public readonly void Dispose()
    {
        // No resources to dispose
    }
}
