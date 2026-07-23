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
    private int chunkIndex;
    private int indexInChunk;

    internal QueryEnumerator(World world, QueryDescription description)
    {
        this.world = world;
        archetypes = world.GetMatchingArchetypes(description);
        withStringTags = description.WithStringTags;
        withoutStringTags = description.WithoutStringTags;
        hasStringTagFilters = description.HasStringTagFilters;
        archetypeIndex = 0;
        chunkIndex = 0;
        indexInChunk = -1;
    }

    /// <inheritdoc />
    public readonly Entity Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (archetypeIndex < archetypes.Count)
            {
                var chunks = archetypes[archetypeIndex].Chunks;
                if (chunkIndex < chunks.Count)
                {
                    return chunks[chunkIndex].GetEntity(indexInChunk);
                }
            }
            return Entity.Null;
        }
    }

    readonly object IEnumerator.Current => Current;

    /// <inheritdoc />
    /// <remarks>
    /// Iteration walks each archetype chunk by its live <see cref="ArchetypeChunk.Count"/> and
    /// advances chunk-by-chunk, so entities in later chunks are never skipped and holes left by
    /// swap-back removal in earlier chunks are never yielded (see issue #1092).
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext()
    {
        while (archetypeIndex < archetypes.Count)
        {
            var chunks = archetypes[archetypeIndex].Chunks;

            while (chunkIndex < chunks.Count)
            {
                var chunk = chunks[chunkIndex];
                indexInChunk++;

                if (indexInChunk < chunk.Count)
                {
                    // Check string tag filters if any
                    if (hasStringTagFilters)
                    {
                        var entity = chunk.GetEntity(indexInChunk);
                        if (!MatchesStringTags(entity))
                        {
                            continue; // Skip this entity, try next slot in this chunk
                        }
                    }
                    return true;
                }

                // Move to the next chunk in this archetype
                chunkIndex++;
                indexInChunk = -1;
            }

            // Move to the next archetype
            archetypeIndex++;
            chunkIndex = 0;
            indexInChunk = -1;
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
        chunkIndex = 0;
        indexInChunk = -1;
    }

    /// <inheritdoc />
    public readonly void Dispose()
    {
        // No resources to dispose
    }
}
