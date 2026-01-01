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
/// <para>
/// This class is thread-safe: subscriptions, unsubscriptions, and event firing
/// can occur concurrently from multiple threads.
/// </para>
/// </remarks>
internal sealed class EntityEventHandlers
{
    private readonly Lock createdLock = new();
    private readonly Lock destroyedLock = new();

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
        lock (createdLock)
        {
            createdHandlers.Add(handler);
        }

        return new EventSubscription(() =>
        {
            lock (createdLock)
            {
                createdHandlers.Remove(handler);
            }
        });
    }

    /// <summary>
    /// Fires the entity created event.
    /// </summary>
    internal void FireCreated(Entity entity, string? name)
    {
        // Take a snapshot under lock, then invoke outside lock
        // This prevents deadlocks if handlers try to subscribe/unsubscribe
        Action<Entity, string?>[] snapshot;
        lock (createdLock)
        {
            if (createdHandlers.Count == 0)
            {
                return;
            }

            snapshot = [.. createdHandlers];
        }

        // Invoke in reverse order to match original behavior
        for (int i = snapshot.Length - 1; i >= 0; i--)
        {
            snapshot[i](entity, name);
        }
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
        lock (destroyedLock)
        {
            destroyedHandlers.Add(handler);
        }

        return new EventSubscription(() =>
        {
            lock (destroyedLock)
            {
                destroyedHandlers.Remove(handler);
            }
        });
    }

    /// <summary>
    /// Fires the entity destroyed event.
    /// </summary>
    internal void FireDestroyed(Entity entity)
    {
        Action<Entity>[] snapshot;
        lock (destroyedLock)
        {
            if (destroyedHandlers.Count == 0)
            {
                return;
            }

            snapshot = [.. destroyedHandlers];
        }

        for (int i = snapshot.Length - 1; i >= 0; i--)
        {
            snapshot[i](entity);
        }
    }

    #endregion

    /// <summary>
    /// Clears all registered entity lifecycle handlers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method is called during world disposal to ensure all entity event
    /// subscriptions are properly cleaned up. After calling this method:
    /// </para>
    /// <list type="bullet">
    /// <item><description>All "entity created" handlers are removed</description></item>
    /// <item><description>All "entity destroyed" handlers are removed</description></item>
    /// <item><description>Existing <see cref="EventSubscription"/> objects become no-ops when disposed</description></item>
    /// </list>
    /// <para>
    /// This prevents potential memory leaks from subscribers holding references to entities
    /// after world disposal.
    /// </para>
    /// </remarks>
    internal void Clear()
    {
        lock (createdLock)
        {
            createdHandlers.Clear();
        }

        lock (destroyedLock)
        {
            destroyedHandlers.Clear();
        }
    }
}
