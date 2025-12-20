namespace KeenEyes.Capabilities;

/// <summary>
/// Capability interface for entity hierarchy operations.
/// </summary>
/// <remarks>
/// <para>
/// This capability provides access to parent-child relationship management
/// for entities. Plugins and systems that need to traverse or manipulate
/// entity hierarchies should request this capability via
/// <see cref="IPluginContext.GetCapability{T}"/> rather than casting to
/// the concrete World type.
/// </para>
/// <para>
/// Common use cases include UI systems that need to render children,
/// transform propagation systems, and scene graph traversal.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public void Install(IPluginContext context)
/// {
///     if (context.TryGetCapability&lt;IHierarchyCapability&gt;(out var hierarchy))
///     {
///         var children = hierarchy.GetChildren(parentEntity);
///         foreach (var child in children)
///         {
///             // Process child entities
///         }
///     }
/// }
/// </code>
/// </example>
public interface IHierarchyCapability
{
    /// <summary>
    /// Sets the parent of an entity, establishing a parent-child relationship.
    /// </summary>
    /// <param name="child">The entity to become a child.</param>
    /// <param name="parent">
    /// The entity to become the parent. Pass <see cref="Entity.Null"/> to remove the parent.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the child entity is not alive, or when setting the parent would create
    /// a circular relationship.
    /// </exception>
    void SetParent(Entity child, Entity parent);

    /// <summary>
    /// Gets the parent of an entity.
    /// </summary>
    /// <param name="entity">The entity to get the parent for.</param>
    /// <returns>
    /// The parent entity, or <see cref="Entity.Null"/> if the entity has no parent.
    /// </returns>
    Entity GetParent(Entity entity);

    /// <summary>
    /// Gets all immediate children of an entity.
    /// </summary>
    /// <param name="entity">The entity to get children for.</param>
    /// <returns>An enumerable of child entities.</returns>
    IEnumerable<Entity> GetChildren(Entity entity);

    /// <summary>
    /// Adds a child entity to a parent entity.
    /// </summary>
    /// <param name="parent">The parent entity.</param>
    /// <param name="child">The entity to add as a child.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when either entity is not alive, or when the relationship would create
    /// a circular hierarchy.
    /// </exception>
    void AddChild(Entity parent, Entity child);

    /// <summary>
    /// Removes a specific child from a parent entity.
    /// </summary>
    /// <param name="parent">The parent entity.</param>
    /// <param name="child">The child entity to remove from the parent.</param>
    /// <returns>True if the child was removed; false otherwise.</returns>
    bool RemoveChild(Entity parent, Entity child);

    /// <summary>
    /// Gets all descendants of an entity (children, grandchildren, etc.).
    /// </summary>
    /// <param name="entity">The entity to get descendants for.</param>
    /// <returns>An enumerable of all descendant entities in breadth-first order.</returns>
    IEnumerable<Entity> GetDescendants(Entity entity);

    /// <summary>
    /// Gets all ancestors of an entity (parent, grandparent, etc.).
    /// </summary>
    /// <param name="entity">The entity to get ancestors for.</param>
    /// <returns>
    /// An enumerable of all ancestor entities, starting with the immediate parent.
    /// </returns>
    IEnumerable<Entity> GetAncestors(Entity entity);

    /// <summary>
    /// Gets the root entity of the hierarchy containing the given entity.
    /// </summary>
    /// <param name="entity">The entity to find the root for.</param>
    /// <returns>
    /// The root entity (topmost ancestor with no parent). If the entity itself
    /// has no parent, returns the entity itself.
    /// </returns>
    Entity GetRoot(Entity entity);

    /// <summary>
    /// Destroys an entity and all its descendants recursively.
    /// </summary>
    /// <param name="entity">The entity to destroy along with all its descendants.</param>
    /// <returns>The number of entities destroyed.</returns>
    int DespawnRecursive(Entity entity);
}
