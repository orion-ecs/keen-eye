namespace KeenEyes.Capabilities;

/// <summary>
/// Capability interface for string-based entity tagging.
/// </summary>
/// <remarks>
/// <para>
/// This capability provides runtime-flexible tagging for scenarios like
/// designer-driven content tagging, serialization, and editor tooling.
/// Unlike type-safe tag components, string tags can be assigned dynamically
/// without compile-time type definitions.
/// </para>
/// <para>
/// Plugins that need to work with string tags should request this capability via
/// <see cref="IPluginContext.GetCapability{T}"/> rather than casting to
/// the concrete World type.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public void Install(IPluginContext context)
/// {
///     if (context.TryGetCapability&lt;ITagCapability&gt;(out var tags))
///     {
///         // Add tags to entities
///         tags.AddTag(entity, "Enemy");
///         tags.AddTag(entity, "Boss");
///
///         // Query by tag
///         foreach (var enemy in tags.QueryByTag("Enemy"))
///         {
///             // Process enemies
///         }
///     }
/// }
/// </code>
/// </example>
public interface ITagCapability
{
    /// <summary>
    /// Adds a string tag to an entity.
    /// </summary>
    /// <param name="entity">The entity to add the tag to.</param>
    /// <param name="tag">The tag to add. Cannot be null, empty, or whitespace.</param>
    /// <returns>True if the tag was added; false if the entity already had the tag.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the entity is not alive.</exception>
    /// <exception cref="ArgumentNullException">Thrown when tag is null.</exception>
    /// <exception cref="ArgumentException">Thrown when tag is empty or whitespace.</exception>
    bool AddTag(Entity entity, string tag);

    /// <summary>
    /// Removes a string tag from an entity.
    /// </summary>
    /// <param name="entity">The entity to remove the tag from.</param>
    /// <param name="tag">The tag to remove. Cannot be null, empty, or whitespace.</param>
    /// <returns>True if the tag was removed; false if the entity didn't have the tag.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the entity is not alive.</exception>
    /// <exception cref="ArgumentNullException">Thrown when tag is null.</exception>
    /// <exception cref="ArgumentException">Thrown when tag is empty or whitespace.</exception>
    bool RemoveTag(Entity entity, string tag);

    /// <summary>
    /// Checks if an entity has a specific string tag.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <param name="tag">The tag to check for. Cannot be null, empty, or whitespace.</param>
    /// <returns>True if the entity is alive and has the specified tag.</returns>
    /// <exception cref="ArgumentNullException">Thrown when tag is null.</exception>
    /// <exception cref="ArgumentException">Thrown when tag is empty or whitespace.</exception>
    bool HasTag(Entity entity, string tag);

    /// <summary>
    /// Gets all string tags on an entity.
    /// </summary>
    /// <param name="entity">The entity to get tags for.</param>
    /// <returns>
    /// A read-only collection of tags on the entity. Returns an empty collection
    /// if the entity is not alive or has no tags.
    /// </returns>
    IReadOnlyCollection<string> GetTags(Entity entity);

    /// <summary>
    /// Gets all entities that have a specific string tag.
    /// </summary>
    /// <param name="tag">The tag to query for. Cannot be null, empty, or whitespace.</param>
    /// <returns>An enumerable of entities that have the specified tag.</returns>
    /// <exception cref="ArgumentNullException">Thrown when tag is null.</exception>
    /// <exception cref="ArgumentException">Thrown when tag is empty or whitespace.</exception>
    IEnumerable<Entity> QueryByTag(string tag);
}
