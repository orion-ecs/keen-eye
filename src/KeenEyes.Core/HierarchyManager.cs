namespace KeenEyes;

/// <summary>
/// Manages parent-child relationships between entities.
/// </summary>
/// <remarks>
/// <para>
/// This is an internal manager class that handles all entity hierarchy operations.
/// The public API is exposed through <see cref="World"/>.
/// </para>
/// <para>
/// Parent lookup is O(1). Setting a parent is O(D) where D is the depth of the hierarchy
/// due to cycle detection. Child enumeration is O(C) where C is the number of children.
/// </para>
/// </remarks>
internal sealed class HierarchyManager
{
    // childId -> parentId for O(1) parent lookup
    private readonly Dictionary<int, int> entityParents = [];

    // parentId -> set of childIds for O(C) children enumeration where C is child count
    private readonly Dictionary<int, HashSet<int>> entityChildren = [];

    private readonly World world;

    /// <summary>
    /// Creates a new hierarchy manager for the specified world.
    /// </summary>
    /// <param name="world">The world that owns this hierarchy manager.</param>
    internal HierarchyManager(World world)
    {
        this.world = world;
    }

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
    internal void SetParent(Entity child, Entity parent)
    {
        if (!world.IsAlive(child))
        {
            throw new InvalidOperationException($"Entity {child} is not alive.");
        }

        // Handle removing parent (making entity a root)
        if (!parent.IsValid)
        {
            RemoveParentInternal(child);
            return;
        }

        if (!world.IsAlive(parent))
        {
            throw new InvalidOperationException($"Parent entity {parent} is not alive.");
        }

        // Prevent self-parenting
        if (child.Id == parent.Id)
        {
            throw new InvalidOperationException("An entity cannot be its own parent.");
        }

        // Check for cycles: ensure parent is not a descendant of child
        if (IsDescendantOf(parent, child))
        {
            throw new InvalidOperationException(
                $"Cannot set entity {parent} as parent of {child} because it would create a circular hierarchy. " +
                $"Entity {parent} is a descendant of entity {child}.");
        }

        // Remove from current parent if any
        RemoveParentInternal(child);

        // Set new parent
        entityParents[child.Id] = parent.Id;

        // Add to parent's children set
        if (!entityChildren.TryGetValue(parent.Id, out var children))
        {
            children = [];
            entityChildren[parent.Id] = children;
        }
        children.Add(child.Id);
    }

    /// <summary>
    /// Gets the parent of an entity.
    /// </summary>
    /// <param name="entity">The entity to get the parent for.</param>
    /// <returns>
    /// The parent entity, or <see cref="Entity.Null"/> if the entity has no parent
    /// or is not alive.
    /// </returns>
    internal Entity GetParent(Entity entity)
    {
        if (!world.IsAlive(entity))
        {
            return Entity.Null;
        }

        if (!entityParents.TryGetValue(entity.Id, out var parentId))
        {
            return Entity.Null;
        }

        // Get the current version from the pool
        var version = world.EntityPool.GetVersion(parentId);
        if (version < 0)
        {
            return Entity.Null;
        }

        var parent = new Entity(parentId, version);

        // Verify the parent is still alive (handles edge case of stale parent reference)
        if (!world.IsAlive(parent))
        {
            return Entity.Null;
        }

        return parent;
    }

    /// <summary>
    /// Gets all immediate children of an entity.
    /// </summary>
    /// <param name="entity">The entity to get children for.</param>
    /// <returns>An enumerable of child entities.</returns>
    internal IEnumerable<Entity> GetChildren(Entity entity)
    {
        if (!world.IsAlive(entity))
        {
            yield break;
        }

        if (!entityChildren.TryGetValue(entity.Id, out var childIds))
        {
            yield break;
        }

        foreach (var childId in childIds)
        {
            var version = world.EntityPool.GetVersion(childId);
            if (version < 0)
            {
                continue;
            }

            var child = new Entity(childId, version);
            if (world.IsAlive(child))
            {
                yield return child;
            }
        }
    }

    /// <summary>
    /// Adds a child entity to a parent entity.
    /// </summary>
    /// <param name="parent">The parent entity.</param>
    /// <param name="child">The entity to add as a child.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when either entity is not alive, or when the relationship would create
    /// a circular hierarchy.
    /// </exception>
    internal void AddChild(Entity parent, Entity child)
    {
        if (!world.IsAlive(parent))
        {
            throw new InvalidOperationException($"Parent entity {parent} is not alive.");
        }

        SetParent(child, parent);
    }

    /// <summary>
    /// Removes a specific child from a parent entity.
    /// </summary>
    /// <param name="parent">The parent entity.</param>
    /// <param name="child">The child entity to remove.</param>
    /// <returns>True if the child was removed; false otherwise.</returns>
    internal bool RemoveChild(Entity parent, Entity child)
    {
        if (!world.IsAlive(parent) || !world.IsAlive(child))
        {
            return false;
        }

        // Check if child is actually a child of parent
        if (!entityParents.TryGetValue(child.Id, out var currentParentId) || currentParentId != parent.Id)
        {
            return false;
        }

        // Remove the relationship
        RemoveParentInternal(child);
        return true;
    }

    /// <summary>
    /// Gets all descendants of an entity (children, grandchildren, etc.).
    /// </summary>
    /// <param name="entity">The entity to get descendants for.</param>
    /// <returns>An enumerable of all descendant entities in breadth-first order.</returns>
    internal IEnumerable<Entity> GetDescendants(Entity entity)
    {
        if (!world.IsAlive(entity))
        {
            yield break;
        }

        // Breadth-first traversal using a queue
        var queue = new Queue<int>();

        // Start with immediate children
        if (entityChildren.TryGetValue(entity.Id, out var childIds))
        {
            foreach (var childId in childIds)
            {
                queue.Enqueue(childId);
            }
        }

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            var version = world.EntityPool.GetVersion(currentId);
            if (version < 0)
            {
                continue;
            }

            var current = new Entity(currentId, version);
            if (!world.IsAlive(current))
            {
                continue;
            }

            yield return current;

            // Add this entity's children to the queue
            if (entityChildren.TryGetValue(currentId, out var grandchildIds))
            {
                foreach (var grandchildId in grandchildIds)
                {
                    queue.Enqueue(grandchildId);
                }
            }
        }
    }

    /// <summary>
    /// Gets all ancestors of an entity (parent, grandparent, etc.).
    /// </summary>
    /// <param name="entity">The entity to get ancestors for.</param>
    /// <returns>An enumerable of all ancestor entities, starting with the immediate parent.</returns>
    internal IEnumerable<Entity> GetAncestors(Entity entity)
    {
        if (!world.IsAlive(entity))
        {
            yield break;
        }

        var currentId = entity.Id;
        while (entityParents.TryGetValue(currentId, out var parentId))
        {
            var version = world.EntityPool.GetVersion(parentId);
            if (version < 0)
            {
                yield break;
            }

            var parent = new Entity(parentId, version);
            if (!world.IsAlive(parent))
            {
                yield break;
            }

            yield return parent;
            currentId = parentId;
        }
    }

    /// <summary>
    /// Gets the root entity of the hierarchy containing the given entity.
    /// </summary>
    /// <param name="entity">The entity to find the root for.</param>
    /// <returns>
    /// The root entity. If the entity itself has no parent, returns the entity itself.
    /// Returns <see cref="Entity.Null"/> if the entity is not alive.
    /// </returns>
    internal Entity GetRoot(Entity entity)
    {
        if (!world.IsAlive(entity))
        {
            return Entity.Null;
        }

        var current = entity;
        while (entityParents.TryGetValue(current.Id, out var parentId))
        {
            var version = world.EntityPool.GetVersion(parentId);
            if (version < 0)
            {
                break;
            }

            var parent = new Entity(parentId, version);
            if (!world.IsAlive(parent))
            {
                break;
            }

            current = parent;
        }

        return current;
    }

    /// <summary>
    /// Destroys an entity and all its descendants recursively.
    /// </summary>
    /// <param name="entity">The entity to destroy along with all its descendants.</param>
    /// <returns>The number of entities destroyed.</returns>
    internal int DespawnRecursive(Entity entity)
    {
        if (!world.IsAlive(entity))
        {
            return 0;
        }

        // Collect all entities to despawn (depth-first, children before parents)
        var toDespawn = new List<Entity>();
        CollectDescendantsDepthFirst(entity, toDespawn);
        toDespawn.Add(entity);

        // Despawn all collected entities
        int count = 0;
        foreach (var e in toDespawn)
        {
            if (world.Despawn(e))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Cleans up hierarchy relationships when an entity is being despawned.
    /// Removes the entity from its parent's children and orphans any children.
    /// </summary>
    /// <param name="entity">The entity being despawned.</param>
    internal void CleanupEntity(Entity entity)
    {
        // Remove from parent's children set if this entity has a parent
        if (entityParents.TryGetValue(entity.Id, out var parentId))
        {
            entityParents.Remove(entity.Id);
            if (entityChildren.TryGetValue(parentId, out var siblings))
            {
                siblings.Remove(entity.Id);
                if (siblings.Count == 0)
                {
                    entityChildren.Remove(parentId);
                }
            }
        }

        // Orphan all children (remove their parent reference)
        if (entityChildren.TryGetValue(entity.Id, out var children))
        {
            foreach (var childId in children)
            {
                entityParents.Remove(childId);
            }
            entityChildren.Remove(entity.Id);
        }
    }

    /// <summary>
    /// Clears all hierarchy data.
    /// </summary>
    internal void Clear()
    {
        entityParents.Clear();
        entityChildren.Clear();
    }

    /// <summary>
    /// Removes the parent relationship for an entity without validation.
    /// </summary>
    private void RemoveParentInternal(Entity child)
    {
        if (entityParents.TryGetValue(child.Id, out var oldParentId))
        {
            entityParents.Remove(child.Id);
            if (entityChildren.TryGetValue(oldParentId, out var siblings))
            {
                siblings.Remove(child.Id);
                if (siblings.Count == 0)
                {
                    entityChildren.Remove(oldParentId);
                }
            }
        }
    }

    /// <summary>
    /// Checks if a potential descendant is in the descendant tree of an ancestor.
    /// Used for cycle detection.
    /// </summary>
    private bool IsDescendantOf(Entity potentialDescendant, Entity potentialAncestor)
    {
        // Walk up the hierarchy from potentialDescendant
        var currentId = potentialDescendant.Id;
        while (entityParents.TryGetValue(currentId, out var parentId))
        {
            if (parentId == potentialAncestor.Id)
            {
                return true;
            }
            currentId = parentId;
        }
        return false;
    }

    /// <summary>
    /// Collects all descendants in depth-first order (children before parents).
    /// </summary>
    private void CollectDescendantsDepthFirst(Entity entity, List<Entity> result)
    {
        if (!entityChildren.TryGetValue(entity.Id, out var childIds))
        {
            return;
        }

        // Create a copy of child IDs to avoid modification during iteration
        var childIdsCopy = childIds.ToArray();

        foreach (var childId in childIdsCopy)
        {
            var version = world.EntityPool.GetVersion(childId);
            if (version < 0)
            {
                continue;
            }

            var child = new Entity(childId, version);
            if (!world.IsAlive(child))
            {
                continue;
            }

            // Recurse first (depth-first)
            CollectDescendantsDepthFirst(child, result);
            result.Add(child);
        }
    }
}
