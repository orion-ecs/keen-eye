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
/// <para>
/// This class is thread-safe: all hierarchy operations can be called concurrently
/// from multiple threads.
/// </para>
/// </remarks>
internal sealed class HierarchyManager
{
    private readonly Lock syncRoot = new();

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
            lock (syncRoot)
            {
                RemoveParentInternalNoLock(child);
            }
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

        lock (syncRoot)
        {
            // Check for cycles: ensure parent is not a descendant of child
            if (IsDescendantOfNoLock(parent, child))
            {
                throw new InvalidOperationException(
                    $"Cannot set entity {parent} as parent of {child} because it would create a circular hierarchy. " +
                    $"Entity {parent} is a descendant of entity {child}.");
            }

            // Remove from current parent if any
            RemoveParentInternalNoLock(child);

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

        int parentId;
        lock (syncRoot)
        {
            if (!entityParents.TryGetValue(entity.Id, out parentId))
            {
                return Entity.Null;
            }
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
            return [];
        }

        int[] childIdsCopy;
        lock (syncRoot)
        {
            if (!entityChildren.TryGetValue(entity.Id, out var childIds))
            {
                return [];
            }
            childIdsCopy = [.. childIds];
        }

        return GetChildrenCore(childIdsCopy);
    }

    private IEnumerable<Entity> GetChildrenCore(int[] childIds)
    {
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

        lock (syncRoot)
        {
            // Check if child is actually a child of parent
            if (!entityParents.TryGetValue(child.Id, out var currentParentId) || currentParentId != parent.Id)
            {
                return false;
            }

            // Remove the relationship
            RemoveParentInternalNoLock(child);
            return true;
        }
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
            return [];
        }

        // Snapshot the hierarchy under lock, then enumerate without holding lock
        List<int> allDescendantIds;
        lock (syncRoot)
        {
            allDescendantIds = CollectDescendantIdsNoLock(entity.Id);
        }

        return GetDescendantsCore(allDescendantIds);
    }

    private List<int> CollectDescendantIdsNoLock(int entityId)
    {
        var result = new List<int>();
        var queue = new Queue<int>();

        // Start with immediate children
        if (entityChildren.TryGetValue(entityId, out var childIds))
        {
            foreach (var childId in childIds)
            {
                queue.Enqueue(childId);
            }
        }

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            result.Add(currentId);

            // Add this entity's children to the queue
            if (entityChildren.TryGetValue(currentId, out var grandchildIds))
            {
                foreach (var grandchildId in grandchildIds)
                {
                    queue.Enqueue(grandchildId);
                }
            }
        }

        return result;
    }

    private IEnumerable<Entity> GetDescendantsCore(List<int> descendantIds)
    {
        foreach (var id in descendantIds)
        {
            var version = world.EntityPool.GetVersion(id);
            if (version < 0)
            {
                continue;
            }

            var entity = new Entity(id, version);
            if (world.IsAlive(entity))
            {
                yield return entity;
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
            return [];
        }

        // Snapshot ancestor IDs under lock
        List<int> ancestorIds;
        lock (syncRoot)
        {
            ancestorIds = [];
            var currentId = entity.Id;
            while (entityParents.TryGetValue(currentId, out var parentId))
            {
                ancestorIds.Add(parentId);
                currentId = parentId;
            }
        }

        return GetAncestorsCore(ancestorIds);
    }

    private IEnumerable<Entity> GetAncestorsCore(List<int> ancestorIds)
    {
        foreach (var parentId in ancestorIds)
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

        int rootId;
        lock (syncRoot)
        {
            rootId = entity.Id;
            while (entityParents.TryGetValue(rootId, out var parentId))
            {
                rootId = parentId;
            }
        }

        // If we found a different root, verify it's alive
        if (rootId != entity.Id)
        {
            var version = world.EntityPool.GetVersion(rootId);
            if (version < 0)
            {
                return entity; // Parent chain broken, return original
            }

            var root = new Entity(rootId, version);
            if (!world.IsAlive(root))
            {
                return entity; // Parent chain broken, return original
            }

            return root;
        }

        return entity;
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

        // Collect all entities to despawn under lock (depth-first, children before parents)
        List<Entity> toDespawn;
        lock (syncRoot)
        {
            toDespawn = [];
            CollectDescendantsDepthFirstNoLock(entity, toDespawn);
            toDespawn.Add(entity);
        }

        // Despawn all collected entities (outside lock - Despawn acquires its own locks)
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
        lock (syncRoot)
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
    }

    /// <summary>
    /// Clears all hierarchy data.
    /// </summary>
    internal void Clear()
    {
        lock (syncRoot)
        {
            entityParents.Clear();
            entityChildren.Clear();
        }
    }

    /// <summary>
    /// Removes the parent relationship for an entity without validation.
    /// Must be called while holding syncRoot.
    /// </summary>
    private void RemoveParentInternalNoLock(Entity child)
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
    /// Used for cycle detection. Must be called while holding syncRoot.
    /// </summary>
    private bool IsDescendantOfNoLock(Entity potentialDescendant, Entity potentialAncestor)
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
    /// Must be called while holding syncRoot.
    /// </summary>
    private void CollectDescendantsDepthFirstNoLock(Entity entity, List<Entity> result)
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
            CollectDescendantsDepthFirstNoLock(child, result);
            result.Add(child);
        }
    }
}
