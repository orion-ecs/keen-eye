namespace KeenEyes;

/// <summary>
/// Manages string-based entity tags for runtime-flexible tagging.
/// </summary>
/// <remarks>
/// <para>
/// This is an internal manager class that handles all string tag operations.
/// The public API is exposed through <see cref="World"/>.
/// </para>
/// <para>
/// Unlike type-safe tag components (<see cref="ITagComponent"/>), string tags
/// provide runtime flexibility for scenarios like:
/// </para>
/// <list type="bullet">
/// <item><description>Designer-driven content tagging</description></item>
/// <item><description>Serialization-friendly entity categorization</description></item>
/// <item><description>Dynamic tag assignment from data files</description></item>
/// <item><description>Editor tooling and debugging</description></item>
/// </list>
/// <para>
/// The manager uses dual indexing for O(1) tag checks and efficient queries:
/// Entity → Tags mapping and Tag → Entities reverse index.
/// </para>
/// </remarks>
internal sealed class TagManager
{
    // Entity ID -> Set of tags on that entity (for O(1) HasTag and GetTags)
    private readonly Dictionary<int, HashSet<string>> entityTags = [];

    // Tag -> Set of entity IDs with that tag (for O(N) QueryByTag where N is matching entities)
    private readonly Dictionary<string, HashSet<int>> tagToEntities = [];

    /// <summary>
    /// Adds a tag to an entity.
    /// </summary>
    /// <param name="entityId">The entity ID to add the tag to.</param>
    /// <param name="tag">The tag to add.</param>
    /// <returns>
    /// <c>true</c> if the tag was added; <c>false</c> if the entity already had the tag.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tag"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tag"/> is empty or whitespace.</exception>
    internal bool AddTag(int entityId, string tag)
    {
        ValidateTag(tag);

        // Get or create the entity's tag set
        if (!entityTags.TryGetValue(entityId, out var tags))
        {
            tags = [];
            entityTags[entityId] = tags;
        }

        // Add to entity's tags
        if (!tags.Add(tag))
        {
            return false; // Already had this tag
        }

        // Add to reverse index
        if (!tagToEntities.TryGetValue(tag, out var entities))
        {
            entities = [];
            tagToEntities[tag] = entities;
        }

        entities.Add(entityId);
        return true;
    }

    /// <summary>
    /// Removes a tag from an entity.
    /// </summary>
    /// <param name="entityId">The entity ID to remove the tag from.</param>
    /// <param name="tag">The tag to remove.</param>
    /// <returns>
    /// <c>true</c> if the tag was removed; <c>false</c> if the entity didn't have the tag.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tag"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tag"/> is empty or whitespace.</exception>
    internal bool RemoveTag(int entityId, string tag)
    {
        ValidateTag(tag);

        // Remove from entity's tags
        if (!entityTags.TryGetValue(entityId, out var tags) || !tags.Remove(tag))
        {
            return false; // Entity didn't have this tag
        }

        // Clean up empty tag set
        if (tags.Count == 0)
        {
            entityTags.Remove(entityId);
        }

        // Remove from reverse index
        if (tagToEntities.TryGetValue(tag, out var entities))
        {
            entities.Remove(entityId);

            // Clean up empty entity set
            if (entities.Count == 0)
            {
                tagToEntities.Remove(tag);
            }
        }

        return true;
    }

    /// <summary>
    /// Checks if an entity has a specific tag.
    /// </summary>
    /// <param name="entityId">The entity ID to check.</param>
    /// <param name="tag">The tag to check for.</param>
    /// <returns><c>true</c> if the entity has the tag; <c>false</c> otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tag"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tag"/> is empty or whitespace.</exception>
    internal bool HasTag(int entityId, string tag)
    {
        ValidateTag(tag);
        return entityTags.TryGetValue(entityId, out var tags) && tags.Contains(tag);
    }

    /// <summary>
    /// Gets all tags on an entity.
    /// </summary>
    /// <param name="entityId">The entity ID to get tags for.</param>
    /// <returns>
    /// A read-only collection of tags on the entity. Returns an empty collection
    /// if the entity has no tags.
    /// </returns>
    internal IReadOnlyCollection<string> GetTags(int entityId)
    {
        if (entityTags.TryGetValue(entityId, out var tags))
        {
            return tags;
        }

        return [];
    }

    /// <summary>
    /// Gets all entity IDs that have a specific tag.
    /// </summary>
    /// <param name="tag">The tag to query for.</param>
    /// <returns>
    /// A read-only collection of entity IDs that have the specified tag.
    /// Returns an empty collection if no entities have the tag.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tag"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tag"/> is empty or whitespace.</exception>
    internal IReadOnlyCollection<int> GetEntitiesWithTag(string tag)
    {
        ValidateTag(tag);

        if (tagToEntities.TryGetValue(tag, out var entities))
        {
            return entities;
        }

        return [];
    }

    /// <summary>
    /// Removes all tags from an entity.
    /// </summary>
    /// <param name="entityId">The entity ID to remove all tags from.</param>
    /// <remarks>
    /// This method is called during entity despawn to clean up tag mappings.
    /// </remarks>
    internal void RemoveAllTags(int entityId)
    {
        if (!entityTags.TryGetValue(entityId, out var tags))
        {
            return;
        }

        // Remove entity from all tag reverse indexes (only process tags that exist in reverse index)
        foreach (var tag in tags)
        {
            if (tagToEntities.TryGetValue(tag, out var entities))
            {
                entities.Remove(entityId);

                // Clean up empty entity set
                if (entities.Count == 0)
                {
                    tagToEntities.Remove(tag);
                }
            }
        }

        // Remove entity's tag set
        entityTags.Remove(entityId);
    }

    /// <summary>
    /// Clears all tag mappings.
    /// </summary>
    internal void Clear()
    {
        entityTags.Clear();
        tagToEntities.Clear();
    }

    /// <summary>
    /// Validates that a tag is not null, empty, or whitespace.
    /// </summary>
    private static void ValidateTag(string tag)
    {
        ArgumentNullException.ThrowIfNull(tag);

        if (string.IsNullOrWhiteSpace(tag))
        {
            throw new ArgumentException("Tag cannot be empty or whitespace.", nameof(tag));
        }
    }
}
