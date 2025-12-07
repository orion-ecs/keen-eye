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
/// type while remaining clean for others. Use <see cref="MarkDirty{T}(Entity)"/> to manually
/// flag an entity, and <see cref="ClearDirtyFlags{T}()"/> to reset after processing.
/// </para>
/// </remarks>
internal sealed class ChangeTracker
{
    // Component type -> set of dirty entity IDs
    private readonly Dictionary<Type, HashSet<int>> dirtyEntities = [];

    // Component types with auto-tracking enabled
    private readonly HashSet<Type> autoTrackedTypes = [];

    #region Manual Tracking

    /// <summary>
    /// Marks an entity as dirty (modified) for a specific component type.
    /// </summary>
    /// <typeparam name="T">The component type to mark dirty.</typeparam>
    /// <param name="entity">The entity to mark as dirty.</param>
    /// <remarks>
    /// <para>
    /// This method is idempotent: marking an already-dirty entity has no effect.
    /// </para>
    /// <para>
    /// Call this when you modify a component via <see cref="World.Get{T}(Entity)"/>
    /// and want to track the change for later processing.
    /// </para>
    /// </remarks>
    public void MarkDirty<T>(Entity entity) where T : struct, IComponent
    {
        MarkDirtyInternal(typeof(T), entity.Id);
    }

    /// <summary>
    /// Marks an entity as dirty for a specific component type (non-generic internal version).
    /// </summary>
    internal void MarkDirty(Type componentType, int entityId)
    {
        MarkDirtyInternal(componentType, entityId);
    }

    private void MarkDirtyInternal(Type componentType, int entityId)
    {
        if (!dirtyEntities.TryGetValue(componentType, out var entitySet))
        {
            entitySet = [];
            dirtyEntities[componentType] = entitySet;
        }

        entitySet.Add(entityId);
    }

    /// <summary>
    /// Gets all entity IDs that have been marked dirty for a specific component type.
    /// </summary>
    /// <typeparam name="T">The component type to query.</typeparam>
    /// <returns>
    /// A read-only collection of entity IDs marked dirty for the component type.
    /// Returns an empty collection if no entities are dirty.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The returned collection contains entity IDs, not full <see cref="Entity"/> handles.
    /// This is because the tracker doesn't have access to entity versions. Use the world's
    /// entity pool to reconstruct full handles if needed.
    /// </para>
    /// </remarks>
    public IReadOnlyCollection<int> GetDirtyEntityIds<T>() where T : struct, IComponent
    {
        return GetDirtyEntityIdsInternal(typeof(T));
    }

    /// <summary>
    /// Gets all entity IDs marked dirty for a specific component type (non-generic version).
    /// </summary>
    internal IReadOnlyCollection<int> GetDirtyEntityIds(Type componentType)
    {
        return GetDirtyEntityIdsInternal(componentType);
    }

    private IReadOnlyCollection<int> GetDirtyEntityIdsInternal(Type componentType)
    {
        if (!dirtyEntities.TryGetValue(componentType, out var entitySet))
        {
            return Array.Empty<int>();
        }

        return entitySet;
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
        ClearDirtyFlagsInternal(typeof(T));
    }

    /// <summary>
    /// Clears dirty flags for a specific component type (non-generic version).
    /// </summary>
    internal void ClearDirtyFlags(Type componentType)
    {
        ClearDirtyFlagsInternal(componentType);
    }

    private void ClearDirtyFlagsInternal(Type componentType)
    {
        if (dirtyEntities.TryGetValue(componentType, out var entitySet))
        {
            entitySet.Clear();
        }
    }

    /// <summary>
    /// Checks if an entity is marked dirty for a specific component type.
    /// </summary>
    /// <typeparam name="T">The component type to check.</typeparam>
    /// <param name="entity">The entity to check.</param>
    /// <returns>
    /// <c>true</c> if the entity is marked dirty for the component type; <c>false</c> otherwise.
    /// </returns>
    public bool IsDirty<T>(Entity entity) where T : struct, IComponent
    {
        return IsDirtyInternal(typeof(T), entity.Id);
    }

    /// <summary>
    /// Checks if an entity is marked dirty for a specific component type (non-generic version).
    /// </summary>
    internal bool IsDirty(Type componentType, int entityId)
    {
        return IsDirtyInternal(componentType, entityId);
    }

    private bool IsDirtyInternal(Type componentType, int entityId)
    {
        if (!dirtyEntities.TryGetValue(componentType, out var entitySet))
        {
            return false;
        }

        return entitySet.Contains(entityId);
    }

    /// <summary>
    /// Clears the dirty flag for a specific entity and component type.
    /// </summary>
    /// <typeparam name="T">The component type to clear.</typeparam>
    /// <param name="entity">The entity to clear the flag for.</param>
    /// <returns>
    /// <c>true</c> if the entity was dirty and has been cleared;
    /// <c>false</c> if the entity was not dirty.
    /// </returns>
    public bool ClearDirty<T>(Entity entity) where T : struct, IComponent
    {
        return ClearDirtyInternal(typeof(T), entity.Id);
    }

    private bool ClearDirtyInternal(Type componentType, int entityId)
    {
        if (!dirtyEntities.TryGetValue(componentType, out var entitySet))
        {
            return false;
        }

        return entitySet.Remove(entityId);
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
    /// not automatically tracked. Use <see cref="MarkDirty{T}(Entity)"/> manually in those cases.
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

    /// <summary>
    /// Checks if automatic tracking is enabled for a component type (non-generic version).
    /// </summary>
    internal bool IsAutoTrackingEnabled(Type componentType)
    {
        return autoTrackedTypes.Contains(componentType);
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

    /// <summary>
    /// Gets the total number of component types that have dirty entities.
    /// </summary>
    public int TrackedTypeCount => dirtyEntities.Count(kvp => kvp.Value.Count > 0);

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
