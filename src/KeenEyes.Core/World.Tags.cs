namespace KeenEyes;

public sealed partial class World
{
    #region String Tags

    /// <summary>
    /// Adds a string tag to an entity.
    /// </summary>
    /// <param name="entity">The entity to add the tag to.</param>
    /// <param name="tag">The tag to add. Cannot be null, empty, or whitespace.</param>
    /// <returns>
    /// <c>true</c> if the tag was added; <c>false</c> if the entity already had the tag.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when the entity is not alive.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tag"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tag"/> is empty or whitespace.</exception>
    /// <remarks>
    /// <para>
    /// String tags provide runtime-flexible tagging for scenarios like designer-driven
    /// content tagging, serialization, and editor tooling. Unlike type-safe tag components
    /// (<see cref="ITagComponent"/>), string tags can be assigned dynamically without
    /// compile-time type definitions.
    /// </para>
    /// <para>
    /// This operation is O(1).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var enemy = world.Spawn()
    ///     .With(new Position { X = 0, Y = 0 })
    ///     .Build();
    ///
    /// world.AddTag(enemy, "Enemy");
    /// world.AddTag(enemy, "Hostile");
    /// world.AddTag(enemy, "Boss");
    ///
    /// // Check if entity has tag
    /// if (world.HasTag(enemy, "Boss"))
    /// {
    ///     // Special boss handling
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="RemoveTag(Entity, string)"/>
    /// <seealso cref="HasTag(Entity, string)"/>
    /// <seealso cref="GetTags(Entity)"/>
    public bool AddTag(Entity entity, string tag)
    {
        if (!IsAlive(entity))
        {
            throw new InvalidOperationException($"Entity {entity} is not alive.");
        }

        return tagManager.AddTag(entity.Id, tag);
    }

    /// <summary>
    /// Removes a string tag from an entity.
    /// </summary>
    /// <param name="entity">The entity to remove the tag from.</param>
    /// <param name="tag">The tag to remove. Cannot be null, empty, or whitespace.</param>
    /// <returns>
    /// <c>true</c> if the tag was removed; <c>false</c> if the entity didn't have the tag.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when the entity is not alive.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tag"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tag"/> is empty or whitespace.</exception>
    /// <remarks>
    /// <para>
    /// This operation is O(1).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Remove a tag when enemy becomes friendly
    /// world.RemoveTag(entity, "Hostile");
    /// world.AddTag(entity, "Friendly");
    /// </code>
    /// </example>
    /// <seealso cref="AddTag(Entity, string)"/>
    /// <seealso cref="HasTag(Entity, string)"/>
    public bool RemoveTag(Entity entity, string tag)
    {
        if (!IsAlive(entity))
        {
            throw new InvalidOperationException($"Entity {entity} is not alive.");
        }

        return tagManager.RemoveTag(entity.Id, tag);
    }

    /// <summary>
    /// Checks if an entity has a specific string tag.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <param name="tag">The tag to check for. Cannot be null, empty, or whitespace.</param>
    /// <returns>
    /// <c>true</c> if the entity is alive and has the specified tag;
    /// <c>false</c> if the entity is not alive or doesn't have the tag.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tag"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tag"/> is empty or whitespace.</exception>
    /// <remarks>
    /// <para>
    /// This method is safe to call with stale entity handles. A stale handle refers to
    /// an entity that has been destroyed (via <see cref="Despawn"/>). In such cases,
    /// this method returns <c>false</c> rather than throwing an exception.
    /// </para>
    /// <para>
    /// This operation is O(1).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// foreach (var entity in world.Query&lt;Position&gt;())
    /// {
    ///     if (world.HasTag(entity, "Player"))
    ///     {
    ///         // Handle player entity
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="AddTag(Entity, string)"/>
    /// <seealso cref="GetTags(Entity)"/>
    public bool HasTag(Entity entity, string tag)
    {
        if (!IsAlive(entity))
        {
            return false;
        }

        return tagManager.HasTag(entity.Id, tag);
    }

    /// <summary>
    /// Gets all string tags on an entity.
    /// </summary>
    /// <param name="entity">The entity to get tags for.</param>
    /// <returns>
    /// A read-only collection of tags on the entity. Returns an empty collection
    /// if the entity is not alive or has no tags.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is safe to call with stale entity handles. A stale handle refers to
    /// an entity that has been destroyed (via <see cref="Despawn"/>). In such cases,
    /// this method returns an empty collection rather than throwing an exception.
    /// </para>
    /// <para>
    /// This operation is O(1) to obtain the collection.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var tags = world.GetTags(entity);
    /// Console.WriteLine($"Entity has {tags.Count} tags:");
    /// foreach (var tag in tags)
    /// {
    ///     Console.WriteLine($"  - {tag}");
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="AddTag(Entity, string)"/>
    /// <seealso cref="HasTag(Entity, string)"/>
    public IReadOnlyCollection<string> GetTags(Entity entity)
    {
        if (!IsAlive(entity))
        {
            return [];
        }

        return tagManager.GetTags(entity.Id);
    }

    /// <summary>
    /// Gets all entities that have a specific string tag.
    /// </summary>
    /// <param name="tag">The tag to query for. Cannot be null, empty, or whitespace.</param>
    /// <returns>
    /// An enumerable of entities that have the specified tag.
    /// Returns an empty sequence if no entities have the tag.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tag"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tag"/> is empty or whitespace.</exception>
    /// <remarks>
    /// <para>
    /// This method filters out any stale entity references, ensuring all returned
    /// entities are alive.
    /// </para>
    /// <para>
    /// This operation is O(N) where N is the number of entities with the tag.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Process all enemies
    /// foreach (var entity in world.QueryByTag("Enemy"))
    /// {
    ///     ref var pos = ref world.Get&lt;Position&gt;(entity);
    ///     // Update enemy position
    /// }
    ///
    /// // Count players
    /// var playerCount = world.QueryByTag("Player").Count();
    /// </code>
    /// </example>
    /// <seealso cref="AddTag(Entity, string)"/>
    /// <seealso cref="HasTag(Entity, string)"/>
    public IEnumerable<Entity> QueryByTag(string tag)
    {
        var entityIds = tagManager.GetEntitiesWithTag(tag);

        foreach (var entityId in entityIds)
        {
            var version = entityPool.GetVersion(entityId);
            if (version < 0)
            {
                continue;
            }

            var entity = new Entity(entityId, version);
            if (IsAlive(entity))
            {
                yield return entity;
            }
        }
    }

    #endregion
}
