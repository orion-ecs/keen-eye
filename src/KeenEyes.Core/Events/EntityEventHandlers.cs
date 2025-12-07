namespace KeenEyes.Events;

/// <summary>
/// Manages entity lifecycle event handlers.
/// </summary>
/// <remarks>
/// <para>
/// This class provides handlers for entity creation and destruction events.
/// Unlike component events which are typed per-component, entity events
/// apply to all entities regardless of their components.
/// </para>
/// <para>
/// Performance note: When no handlers are registered, event firing has
/// minimal overhead (checking an empty list).
/// </para>
/// </remarks>
internal sealed class EntityEventHandlers
{
    // Entity created: (Entity, string? name) -> void
    private readonly List<Action<Entity, string?>> createdHandlers = [];

    // Entity destroyed: (Entity) -> void
    private readonly List<Action<Entity>> destroyedHandlers = [];

    #region Created Handlers

    /// <summary>
    /// Registers a handler to be called when an entity is created.
    /// </summary>
    /// <param name="handler">
    /// The handler to invoke when an entity is created. Receives the entity
    /// and its optional name (null if unnamed).
    /// </param>
    /// <returns>A subscription that can be disposed to unsubscribe.</returns>
    public EventSubscription OnCreated(Action<Entity, string?> handler)
    {
        createdHandlers.Add(handler);

        return new EventSubscription(() =>
        {
            createdHandlers.Remove(handler);
        });
    }

    /// <summary>
    /// Fires the entity created event.
    /// </summary>
    internal void FireCreated(Entity entity, string? name)
    {
        if (createdHandlers.Count == 0)
        {
            return;
        }

        for (int i = createdHandlers.Count - 1; i >= 0; i--)
        {
            createdHandlers[i](entity, name);
        }
    }

    /// <summary>
    /// Checks if there are any handlers for entity created events.
    /// </summary>
    internal bool HasCreatedHandlers()
    {
        return createdHandlers.Count > 0;
    }

    #endregion

    #region Destroyed Handlers

    /// <summary>
    /// Registers a handler to be called when an entity is destroyed.
    /// </summary>
    /// <param name="handler">The handler to invoke when an entity is destroyed.</param>
    /// <returns>A subscription that can be disposed to unsubscribe.</returns>
    /// <remarks>
    /// <para>
    /// The handler is called before the entity is fully removed from the world,
    /// so the entity handle is still valid during the callback.
    /// </para>
    /// </remarks>
    public EventSubscription OnDestroyed(Action<Entity> handler)
    {
        destroyedHandlers.Add(handler);

        return new EventSubscription(() =>
        {
            destroyedHandlers.Remove(handler);
        });
    }

    /// <summary>
    /// Fires the entity destroyed event.
    /// </summary>
    internal void FireDestroyed(Entity entity)
    {
        if (destroyedHandlers.Count == 0)
        {
            return;
        }

        for (int i = destroyedHandlers.Count - 1; i >= 0; i--)
        {
            destroyedHandlers[i](entity);
        }
    }

    /// <summary>
    /// Checks if there are any handlers for entity destroyed events.
    /// </summary>
    internal bool HasDestroyedHandlers()
    {
        return destroyedHandlers.Count > 0;
    }

    #endregion

    /// <summary>
    /// Clears all registered handlers.
    /// </summary>
    internal void Clear()
    {
        createdHandlers.Clear();
        destroyedHandlers.Clear();
    }
}
