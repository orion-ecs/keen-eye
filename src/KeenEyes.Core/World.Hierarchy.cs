namespace KeenEyes;

public sealed partial class World
{
    #region Entity Hierarchy

    /// <summary>
    /// Sets the parent of an entity, establishing a parent-child relationship.
    /// </summary>
    /// <param name="child">The entity to become a child.</param>
    /// <param name="parent">
    /// The entity to become the parent. Pass <see cref="Entity.Null"/> to remove the parent
    /// (make the entity a root entity).
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the child entity is not alive, or when setting the parent would create
    /// a circular relationship (the parent is a descendant of the child).
    /// </exception>
    /// <remarks>
    /// <para>
    /// If the child already has a parent, it will be removed from the previous parent's
    /// children collection before being added to the new parent.
    /// </para>
    /// <para>
    /// This operation performs cycle detection to prevent circular hierarchies. A cycle
    /// occurs when setting an ancestor as a child's parent, which would create an infinite
    /// loop in the hierarchy.
    /// </para>
    /// <para>
    /// Parent lookup is O(1). Setting a parent is O(D) where D is the depth of the hierarchy
    /// due to cycle detection.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var parent = world.Spawn().Build();
    /// var child = world.Spawn().Build();
    ///
    /// // Establish parent-child relationship
    /// world.SetParent(child, parent);
    ///
    /// // Remove parent (make child a root entity)
    /// world.SetParent(child, Entity.Null);
    /// </code>
    /// </example>
    /// <seealso cref="GetParent(Entity)"/>
    /// <seealso cref="GetChildren(Entity)"/>
    /// <seealso cref="AddChild(Entity, Entity)"/>
    public void SetParent(Entity child, Entity parent)
        => hierarchyManager.SetParent(child, parent);

    /// <summary>
    /// Gets the parent of an entity.
    /// </summary>
    /// <param name="entity">The entity to get the parent for.</param>
    /// <returns>
    /// The parent entity, or <see cref="Entity.Null"/> if the entity has no parent
    /// (is a root entity) or is not alive.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is safe to call with stale entity handles. A stale handle refers to
    /// an entity that has been destroyed (via <see cref="Despawn"/>). In such cases,
    /// this method returns <see cref="Entity.Null"/> rather than throwing an exception.
    /// </para>
    /// <para>
    /// This operation is O(1) for the dictionary-based storage implementation.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var parent = world.Spawn().Build();
    /// var child = world.Spawn().Build();
    /// world.SetParent(child, parent);
    ///
    /// var foundParent = world.GetParent(child);
    /// Debug.Assert(foundParent == parent);
    ///
    /// var rootParent = world.GetParent(parent);
    /// Debug.Assert(!rootParent.IsValid); // Root entities have no parent
    /// </code>
    /// </example>
    /// <seealso cref="SetParent(Entity, Entity)"/>
    /// <seealso cref="GetChildren(Entity)"/>
    public Entity GetParent(Entity entity)
        => hierarchyManager.GetParent(entity);

    /// <summary>
    /// Gets all immediate children of an entity.
    /// </summary>
    /// <param name="entity">The entity to get children for.</param>
    /// <returns>
    /// An enumerable of child entities. Returns an empty sequence if the entity has no
    /// children or is not alive.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method only returns immediate children, not grandchildren or other descendants.
    /// Use <see cref="GetDescendants(Entity)"/> to get all descendants recursively.
    /// </para>
    /// <para>
    /// This method is safe to call with stale entity handles. A stale handle refers to
    /// an entity that has been destroyed (via <see cref="Despawn"/>). In such cases,
    /// this method returns an empty sequence rather than throwing an exception.
    /// </para>
    /// <para>
    /// This operation is O(C) where C is the number of children.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var parent = world.Spawn().Build();
    /// var child1 = world.Spawn().Build();
    /// var child2 = world.Spawn().Build();
    ///
    /// world.SetParent(child1, parent);
    /// world.SetParent(child2, parent);
    ///
    /// foreach (var child in world.GetChildren(parent))
    /// {
    ///     Console.WriteLine($"Child: {child}");
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="GetParent(Entity)"/>
    /// <seealso cref="GetDescendants(Entity)"/>
    public IEnumerable<Entity> GetChildren(Entity entity)
        => hierarchyManager.GetChildren(entity);

    /// <summary>
    /// Adds a child entity to a parent entity.
    /// </summary>
    /// <param name="parent">The parent entity.</param>
    /// <param name="child">The entity to add as a child.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when either entity is not alive, or when the relationship would create
    /// a circular hierarchy.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This is a convenience method equivalent to calling <c>SetParent(child, parent)</c>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var parent = world.Spawn().Build();
    /// var child = world.Spawn().Build();
    ///
    /// world.AddChild(parent, child);
    /// // Equivalent to: world.SetParent(child, parent);
    /// </code>
    /// </example>
    /// <seealso cref="SetParent(Entity, Entity)"/>
    /// <seealso cref="RemoveChild(Entity, Entity)"/>
    public void AddChild(Entity parent, Entity child)
        => hierarchyManager.AddChild(parent, child);

    /// <summary>
    /// Removes a specific child from a parent entity.
    /// </summary>
    /// <param name="parent">The parent entity.</param>
    /// <param name="child">The child entity to remove from the parent.</param>
    /// <returns>
    /// <c>true</c> if the child was removed from the parent; <c>false</c> if the parent
    /// is not alive, the child is not alive, or the child was not a child of the parent.
    /// </returns>
    /// <remarks>
    /// <para>
    /// After removal, the child becomes a root entity (has no parent).
    /// </para>
    /// <para>
    /// This operation is idempotent: calling it multiple times with the same arguments
    /// will return <c>false</c> after the first successful removal.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var parent = world.Spawn().Build();
    /// var child = world.Spawn().Build();
    ///
    /// world.SetParent(child, parent);
    /// bool removed = world.RemoveChild(parent, child);
    /// Debug.Assert(removed);
    /// Debug.Assert(!world.GetParent(child).IsValid); // Child is now a root
    /// </code>
    /// </example>
    /// <seealso cref="AddChild(Entity, Entity)"/>
    /// <seealso cref="SetParent(Entity, Entity)"/>
    public bool RemoveChild(Entity parent, Entity child)
        => hierarchyManager.RemoveChild(parent, child);

    /// <summary>
    /// Gets all descendants of an entity (children, grandchildren, etc.).
    /// </summary>
    /// <param name="entity">The entity to get descendants for.</param>
    /// <returns>
    /// An enumerable of all descendant entities in breadth-first order.
    /// Returns an empty sequence if the entity has no descendants or is not alive.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method performs a breadth-first traversal of the hierarchy starting from
    /// the given entity. The entity itself is not included in the results.
    /// </para>
    /// <para>
    /// This method is safe to call with stale entity handles. A stale handle refers to
    /// an entity that has been destroyed (via <see cref="Despawn"/>). In such cases,
    /// this method returns an empty sequence rather than throwing an exception.
    /// </para>
    /// <para>
    /// This operation is O(N) where N is the total number of descendants.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create a hierarchy: root -> child -> grandchild
    /// var root = world.Spawn().Build();
    /// var child = world.Spawn().Build();
    /// var grandchild = world.Spawn().Build();
    ///
    /// world.SetParent(child, root);
    /// world.SetParent(grandchild, child);
    ///
    /// // GetDescendants returns: child, grandchild
    /// foreach (var descendant in world.GetDescendants(root))
    /// {
    ///     Console.WriteLine($"Descendant: {descendant}");
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="GetChildren(Entity)"/>
    /// <seealso cref="GetAncestors(Entity)"/>
    public IEnumerable<Entity> GetDescendants(Entity entity)
        => hierarchyManager.GetDescendants(entity);

    /// <summary>
    /// Gets all ancestors of an entity (parent, grandparent, etc.).
    /// </summary>
    /// <param name="entity">The entity to get ancestors for.</param>
    /// <returns>
    /// An enumerable of all ancestor entities, starting with the immediate parent
    /// and ending with the root. Returns an empty sequence if the entity has no
    /// parent or is not alive.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method walks up the hierarchy from the given entity to the root.
    /// The entity itself is not included in the results.
    /// </para>
    /// <para>
    /// This method is safe to call with stale entity handles. A stale handle refers to
    /// an entity that has been destroyed (via <see cref="Despawn"/>). In such cases,
    /// this method returns an empty sequence rather than throwing an exception.
    /// </para>
    /// <para>
    /// This operation is O(D) where D is the depth of the hierarchy.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create a hierarchy: root -> child -> grandchild
    /// var root = world.Spawn().Build();
    /// var child = world.Spawn().Build();
    /// var grandchild = world.Spawn().Build();
    ///
    /// world.SetParent(child, root);
    /// world.SetParent(grandchild, child);
    ///
    /// // GetAncestors(grandchild) returns: child, root
    /// foreach (var ancestor in world.GetAncestors(grandchild))
    /// {
    ///     Console.WriteLine($"Ancestor: {ancestor}");
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="GetParent(Entity)"/>
    /// <seealso cref="GetRoot(Entity)"/>
    /// <seealso cref="GetDescendants(Entity)"/>
    public IEnumerable<Entity> GetAncestors(Entity entity)
        => hierarchyManager.GetAncestors(entity);

    /// <summary>
    /// Gets the root entity of the hierarchy containing the given entity.
    /// </summary>
    /// <param name="entity">The entity to find the root for.</param>
    /// <returns>
    /// The root entity (the topmost ancestor with no parent). If the entity itself
    /// has no parent, returns the entity itself. Returns <see cref="Entity.Null"/>
    /// if the entity is not alive.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method walks up the hierarchy until it finds an entity with no parent.
    /// If the given entity has no parent, it is itself the root and is returned.
    /// </para>
    /// <para>
    /// This method is safe to call with stale entity handles. A stale handle refers to
    /// an entity that has been destroyed (via <see cref="Despawn"/>). In such cases,
    /// this method returns <see cref="Entity.Null"/> rather than throwing an exception.
    /// </para>
    /// <para>
    /// This operation is O(D) where D is the depth of the hierarchy.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create a hierarchy: root -> child -> grandchild
    /// var root = world.Spawn().Build();
    /// var child = world.Spawn().Build();
    /// var grandchild = world.Spawn().Build();
    ///
    /// world.SetParent(child, root);
    /// world.SetParent(grandchild, child);
    ///
    /// var foundRoot = world.GetRoot(grandchild);
    /// Debug.Assert(foundRoot == root);
    ///
    /// // Root entity returns itself
    /// Debug.Assert(world.GetRoot(root) == root);
    /// </code>
    /// </example>
    /// <seealso cref="GetParent(Entity)"/>
    /// <seealso cref="GetAncestors(Entity)"/>
    public Entity GetRoot(Entity entity)
        => hierarchyManager.GetRoot(entity);

    /// <summary>
    /// Destroys an entity and all its descendants recursively.
    /// </summary>
    /// <param name="entity">The entity to destroy along with all its descendants.</param>
    /// <returns>
    /// The number of entities destroyed (including the root entity and all descendants).
    /// Returns 0 if the entity was not alive.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method performs a depth-first traversal to destroy all descendants before
    /// destroying the entity itself. This ensures proper cleanup of the hierarchy.
    /// </para>
    /// <para>
    /// Unlike <see cref="Despawn(Entity)"/> which orphans children, this method
    /// completely removes the entity and its entire subtree from the world.
    /// </para>
    /// <para>
    /// This method is safe to call with stale entity handles. A stale handle refers to
    /// an entity that has been destroyed (via <see cref="Despawn"/>). In such cases,
    /// this method returns 0 rather than throwing an exception.
    /// </para>
    /// <para>
    /// This operation is O(N) where N is the total number of entities in the subtree.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create a hierarchy: root -> child -> grandchild
    /// var root = world.Spawn().Build();
    /// var child = world.Spawn().Build();
    /// var grandchild = world.Spawn().Build();
    ///
    /// world.SetParent(child, root);
    /// world.SetParent(grandchild, child);
    ///
    /// // Destroy entire hierarchy
    /// int count = world.DespawnRecursive(root);
    /// Debug.Assert(count == 3); // root, child, grandchild
    /// Debug.Assert(!world.IsAlive(root));
    /// Debug.Assert(!world.IsAlive(child));
    /// Debug.Assert(!world.IsAlive(grandchild));
    /// </code>
    /// </example>
    /// <seealso cref="Despawn(Entity)"/>
    /// <seealso cref="GetDescendants(Entity)"/>
    public int DespawnRecursive(Entity entity)
        => hierarchyManager.DespawnRecursive(entity);

    #endregion
}
