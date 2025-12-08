namespace KeenEyes;

public sealed partial class World
{
    #region Change Tracking

    /// <summary>
    /// Manually marks an entity as dirty (modified) for a specific component type.
    /// </summary>
    /// <typeparam name="T">The component type to mark as dirty.</typeparam>
    /// <param name="entity">The entity to mark as dirty.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the entity is not alive.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Use this method to flag entities for change-based processing when modifying
    /// components via <see cref="Get{T}(Entity)"/> references. Modifications through
    /// <see cref="Set{T}(Entity, in T)"/> can be automatically tracked if
    /// <see cref="EnableAutoTracking{T}()"/> has been called.
    /// </para>
    /// <para>
    /// This method is idempotent: marking an already-dirty entity has no additional effect.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Modify a component via Get and manually mark dirty
    /// ref var position = ref world.Get&lt;Position&gt;(entity);
    /// position.X += 10;
    /// world.MarkDirty&lt;Position&gt;(entity);
    ///
    /// // Later, process dirty entities
    /// foreach (var dirtyEntity in world.GetDirtyEntities&lt;Position&gt;())
    /// {
    ///     SyncToNetwork(dirtyEntity);
    /// }
    /// world.ClearDirtyFlags&lt;Position&gt;();
    /// </code>
    /// </example>
    /// <seealso cref="GetDirtyEntities{T}()"/>
    /// <seealso cref="ClearDirtyFlags{T}()"/>
    /// <seealso cref="EnableAutoTracking{T}()"/>
    public void MarkDirty<T>(Entity entity) where T : struct, IComponent
        => changeTracker.MarkDirty<T>(entity, IsAlive);

    /// <summary>
    /// Gets all entities that have been marked dirty for a specific component type.
    /// </summary>
    /// <typeparam name="T">The component type to query.</typeparam>
    /// <returns>
    /// An enumerable of entities marked dirty for the component type.
    /// Returns an empty sequence if no entities are dirty.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The returned entities are verified to be alive. Any entities that were marked dirty
    /// but have since been despawned are not included.
    /// </para>
    /// <para>
    /// This method iterates through all dirty entity IDs and reconstructs full entity handles.
    /// For large numbers of dirty entities, consider caching the results if you need to
    /// iterate multiple times.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Process all entities with modified Position components
    /// foreach (var entity in world.GetDirtyEntities&lt;Position&gt;())
    /// {
    ///     ref readonly var pos = ref world.Get&lt;Position&gt;(entity);
    ///     SyncPositionToNetwork(entity, pos);
    /// }
    ///
    /// // Clear after processing
    /// world.ClearDirtyFlags&lt;Position&gt;();
    /// </code>
    /// </example>
    /// <seealso cref="MarkDirty{T}(Entity)"/>
    /// <seealso cref="ClearDirtyFlags{T}()"/>
    public IEnumerable<Entity> GetDirtyEntities<T>() where T : struct, IComponent
        => changeTracker.GetDirtyEntities<T>(IsAlive);

    /// <summary>
    /// Clears all dirty flags for a specific component type.
    /// </summary>
    /// <typeparam name="T">The component type to clear flags for.</typeparam>
    /// <remarks>
    /// <para>
    /// Call this method after processing dirty entities to reset the tracking state.
    /// Typically done at the end of a frame or after synchronization completes.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Process and clear in one pass
    /// foreach (var entity in world.GetDirtyEntities&lt;Position&gt;())
    /// {
    ///     ProcessEntity(entity);
    /// }
    /// world.ClearDirtyFlags&lt;Position&gt;();
    /// </code>
    /// </example>
    /// <seealso cref="MarkDirty{T}(Entity)"/>
    /// <seealso cref="GetDirtyEntities{T}()"/>
    public void ClearDirtyFlags<T>() where T : struct, IComponent
    {
        changeTracker.ClearDirtyFlags<T>();
    }

    /// <summary>
    /// Checks if an entity is marked dirty for a specific component type.
    /// </summary>
    /// <typeparam name="T">The component type to check.</typeparam>
    /// <param name="entity">The entity to check.</param>
    /// <returns>
    /// <c>true</c> if the entity is marked dirty for the component type;
    /// <c>false</c> if not dirty or the entity is not alive.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is safe to call with stale entity handles. A stale handle refers to
    /// an entity that has been destroyed (via <see cref="Despawn"/>). In such cases,
    /// this method returns <c>false</c> rather than throwing an exception.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// if (world.IsDirty&lt;Position&gt;(entity))
    /// {
    ///     // Entity's position has been modified since last clear
    ///     SyncPosition(entity);
    /// }
    /// </code>
    /// </example>
    public bool IsDirty<T>(Entity entity) where T : struct, IComponent
        => changeTracker.IsDirty<T>(entity, IsAlive);

    /// <summary>
    /// Enables automatic dirty tracking for a component type.
    /// </summary>
    /// <typeparam name="T">The component type to enable auto-tracking for.</typeparam>
    /// <remarks>
    /// <para>
    /// When auto-tracking is enabled, entities are automatically marked dirty when
    /// the component is modified via <see cref="Set{T}(Entity, in T)"/>. This saves
    /// the need to manually call <see cref="MarkDirty{T}(Entity)"/> after each Set.
    /// </para>
    /// <para>
    /// <strong>Important:</strong> Direct modifications via <see cref="Get{T}(Entity)"/>
    /// references are not automatically tracked, as there is no way to detect when
    /// a reference is modified. Use <see cref="MarkDirty{T}(Entity)"/> manually in those cases.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Enable auto-tracking for Position
    /// world.EnableAutoTracking&lt;Position&gt;();
    ///
    /// // Now Set() calls automatically mark entities dirty
    /// world.Set(entity, new Position { X = 100, Y = 200 });
    ///
    /// // The entity is now dirty without manual marking
    /// Debug.Assert(world.IsDirty&lt;Position&gt;(entity));
    /// </code>
    /// </example>
    /// <seealso cref="DisableAutoTracking{T}()"/>
    /// <seealso cref="IsAutoTrackingEnabled{T}()"/>
    public void EnableAutoTracking<T>() where T : struct, IComponent
    {
        changeTracker.EnableAutoTracking<T>();
    }

    /// <summary>
    /// Disables automatic dirty tracking for a component type.
    /// </summary>
    /// <typeparam name="T">The component type to disable auto-tracking for.</typeparam>
    /// <seealso cref="EnableAutoTracking{T}()"/>
    public void DisableAutoTracking<T>() where T : struct, IComponent
    {
        changeTracker.DisableAutoTracking<T>();
    }

    /// <summary>
    /// Checks if automatic dirty tracking is enabled for a component type.
    /// </summary>
    /// <typeparam name="T">The component type to check.</typeparam>
    /// <returns>
    /// <c>true</c> if auto-tracking is enabled; <c>false</c> otherwise.
    /// </returns>
    public bool IsAutoTrackingEnabled<T>() where T : struct, IComponent
    {
        return changeTracker.IsAutoTrackingEnabled<T>();
    }

    /// <summary>
    /// Gets the count of dirty entities for a specific component type.
    /// </summary>
    /// <typeparam name="T">The component type to count.</typeparam>
    /// <returns>The number of entities marked dirty for this component type.</returns>
    /// <remarks>
    /// <para>
    /// This count may include entities that have been despawned since being marked dirty.
    /// Use <see cref="GetDirtyEntities{T}()"/> to iterate only live entities.
    /// </para>
    /// </remarks>
    public int GetDirtyCount<T>() where T : struct, IComponent
    {
        return changeTracker.GetDirtyCount<T>();
    }

    /// <summary>
    /// Clears all dirty flags for all component types.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this to reset all tracking state at once, typically at the end of a frame
    /// after all change-dependent systems have run.
    /// </para>
    /// </remarks>
    public void ClearAllDirtyFlags()
    {
        changeTracker.ClearAll();
    }

    #endregion
}
