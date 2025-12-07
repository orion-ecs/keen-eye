namespace KeenEyes.Events;

/// <summary>
/// Tracks dirty (modified) entities per component type for efficient change detection.
/// </summary>
/// <remarks>
/// <para>
/// The change tracker enables systems to efficiently query which entities have been modified
/// since the last processing cycle. This is useful for:
/// </para>
/// <list type="bullet">
/// <item><description>Network synchronization - only replicate modified entities</description></item>
/// <item><description>Undo/redo systems - track state changes for reversal</description></item>
/// <item><description>Reactive updates - systems responding to component modifications</description></item>
/// </list>
/// <para>
/// Dirty flags are tracked per component type. An entity can be marked dirty for one component
/// type while remaining clean for others. Use <see cref="MarkDirty{T}(Entity, Func{Entity, bool})"/> to manually
/// flag an entity, and <see cref="ClearDirtyFlags{T}()"/> to reset after processing.
/// </para>
/// </remarks>
internal sealed class ChangeTracker
{
    private readonly EntityPool entityPool;

    // Component type -> set of dirty entity IDs
    private readonly Dictionary<Type, HashSet<int>> dirtyEntities = [];

    // Component types with auto-tracking enabled
    private readonly HashSet<Type> autoTrackedTypes = [];

    /// <summary>
    /// Creates a new change tracker.
    /// </summary>
    /// <param name="entityPool">The entity pool for reconstructing entity handles.</param>
    internal ChangeTracker(EntityPool entityPool)
    {
        this.entityPool = entityPool;
    }

    #region Manual Tracking

    /// <summary>
    /// Marks an entity as dirty (modified) for a specific component type.
    /// </summary>
    /// <typeparam name="T">The component type to mark dirty.</typeparam>
    /// <param name="entity">The entity to mark as dirty.</param>
    /// <param name="isAlive">Function to check if the entity is alive.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the entity is not alive.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method is idempotent: marking an already-dirty entity has no effect.
    /// </para>
    /// <para>
    /// Call this when you modify a component via <see cref="World.Get{T}(Entity)"/>
    /// and want to track the change for later processing.
    /// </para>
    /// </remarks>
    public void MarkDirty<T>(Entity entity, Func<Entity, bool> isAlive) where T : struct, IComponent
    {
        if (!isAlive(entity))
        {
            throw new InvalidOperationException($"Entity {entity} is not alive.");
        }

        MarkDirtyInternal<T>(entity);
    }

    /// <summary>
    /// Marks an entity as dirty without validation (internal use only).
    /// </summary>
    internal void MarkDirtyInternal<T>(Entity entity) where T : struct, IComponent
    {
        if (!dirtyEntities.TryGetValue(typeof(T), out var entitySet))
        {
            entitySet = [];
            dirtyEntities[typeof(T)] = entitySet;
        }

        entitySet.Add(entity.Id);
    }

    /// <summary>
    /// Gets all entities that have been marked dirty for a specific component type.
    /// </summary>
    /// <typeparam name="T">The component type to query.</typeparam>
    /// <param name="isAlive">Function to check if an entity is still alive.</param>
    /// <returns>
    /// An enumerable of entities marked dirty for the component type.
    /// Returns an empty sequence if no entities are dirty.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The returned entities are verified to be alive. Any entities that were marked dirty
    /// but have since been despawned are not included.
    /// </para>
    /// </remarks>
    public IEnumerable<Entity> GetDirtyEntities<T>(Func<Entity, bool> isAlive) where T : struct, IComponent
    {
        if (!dirtyEntities.TryGetValue(typeof(T), out var entitySet))
        {
            yield break;
        }

        foreach (var entityId in entitySet)
        {
            var version = entityPool.GetVersion(entityId);
            if (version < 0)
            {
                continue;
            }

            var entity = new Entity(entityId, version);
            if (isAlive(entity))
            {
                yield return entity;
            }
        }
    }

    /// <summary>
    /// Clears all dirty flags for a specific component type.
    /// </summary>
    /// <typeparam name="T">The component type to clear flags for.</typeparam>
    /// <remarks>
    /// <para>
    /// Call this after processing dirty entities to reset the tracking state.
    /// Typically done at the end of a frame or after synchronization.
    /// </para>
    /// </remarks>
    public void ClearDirtyFlags<T>() where T : struct, IComponent
    {
        if (dirtyEntities.TryGetValue(typeof(T), out var entitySet))
        {
            entitySet.Clear();
        }
    }

    /// <summary>
    /// Checks if an entity is marked dirty for a specific component type.
    /// </summary>
    /// <typeparam name="T">The component type to check.</typeparam>
    /// <param name="entity">The entity to check.</param>
    /// <param name="isAlive">Function to check if an entity is still alive.</param>
    /// <returns>
    /// <c>true</c> if the entity is marked dirty for the component type;
    /// <c>false</c> if not dirty or the entity is not alive.
    /// </returns>
    public bool IsDirty<T>(Entity entity, Func<Entity, bool> isAlive) where T : struct, IComponent
    {
        if (!isAlive(entity))
        {
            return false;
        }

        if (!dirtyEntities.TryGetValue(typeof(T), out var entitySet))
        {
            return false;
        }

        return entitySet.Contains(entity.Id);
    }

    #endregion

    #region Automatic Tracking

    /// <summary>
    /// Enables automatic dirty tracking for a component type.
    /// </summary>
    /// <typeparam name="T">The component type to enable auto-tracking for.</typeparam>
    /// <remarks>
    /// <para>
    /// When auto-tracking is enabled for a component type, entities are automatically
    /// marked dirty when the component is modified via <see cref="World.Set{T}(Entity, in T)"/>.
    /// </para>
    /// <para>
    /// Note: Direct modifications via <see cref="World.Get{T}(Entity)"/> references are
    /// not automatically tracked. Use <see cref="MarkDirty{T}(Entity, Func{Entity, bool})"/> manually in those cases.
    /// </para>
    /// </remarks>
    public void EnableAutoTracking<T>() where T : struct, IComponent
    {
        autoTrackedTypes.Add(typeof(T));
    }

    /// <summary>
    /// Disables automatic dirty tracking for a component type.
    /// </summary>
    /// <typeparam name="T">The component type to disable auto-tracking for.</typeparam>
    public void DisableAutoTracking<T>() where T : struct, IComponent
    {
        autoTrackedTypes.Remove(typeof(T));
    }

    /// <summary>
    /// Checks if automatic tracking is enabled for a component type.
    /// </summary>
    /// <typeparam name="T">The component type to check.</typeparam>
    /// <returns>
    /// <c>true</c> if auto-tracking is enabled; <c>false</c> otherwise.
    /// </returns>
    public bool IsAutoTrackingEnabled<T>() where T : struct, IComponent
    {
        return autoTrackedTypes.Contains(typeof(T));
    }

    #endregion

    #region Statistics

    /// <summary>
    /// Gets the count of dirty entities for a specific component type.
    /// </summary>
    /// <typeparam name="T">The component type to count.</typeparam>
    /// <returns>The number of entities marked dirty for this component type.</returns>
    public int GetDirtyCount<T>() where T : struct, IComponent
    {
        if (!dirtyEntities.TryGetValue(typeof(T), out var entitySet))
        {
            return 0;
        }

        return entitySet.Count;
    }

    #endregion

    #region Cleanup

    /// <summary>
    /// Clears all dirty flags for all component types.
    /// </summary>
    public void ClearAll()
    {
        foreach (var entitySet in dirtyEntities.Values)
        {
            entitySet.Clear();
        }
    }

    /// <summary>
    /// Removes an entity from all dirty tracking (typically called when entity is despawned).
    /// </summary>
    internal void RemoveEntity(int entityId)
    {
        foreach (var entitySet in dirtyEntities.Values)
        {
            entitySet.Remove(entityId);
        }
    }

    /// <summary>
    /// Clears all tracking state including auto-tracked types.
    /// </summary>
    internal void Clear()
    {
        dirtyEntities.Clear();
        autoTrackedTypes.Clear();
    }

    #endregion
}
