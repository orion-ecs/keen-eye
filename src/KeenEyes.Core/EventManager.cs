using KeenEyes.Events;

namespace KeenEyes;

/// <summary>
/// Manages all event handling for a world.
/// </summary>
/// <remarks>
/// <para>
/// This is an internal manager class that consolidates all event-related operations.
/// The public API is exposed through <see cref="World"/>.
/// </para>
/// <para>
/// The event manager handles three types of events:
/// </para>
/// <list type="bullet">
/// <item><description>User-defined events via the <see cref="EventBus"/></description></item>
/// <item><description>Component lifecycle events (added, removed, changed)</description></item>
/// <item><description>Entity lifecycle events (created, destroyed)</description></item>
/// </list>
/// </remarks>
internal sealed class EventManager
{
    private readonly EventBus eventBus = new();
    private readonly ComponentEventHandlers componentEvents = new();
    private readonly EntityEventHandlers entityEvents = new();

    /// <summary>
    /// Gets the event bus for publishing and subscribing to custom user-defined events.
    /// </summary>
    internal EventBus Bus => eventBus;

    /// <summary>
    /// Gets the component event handlers for internal use (e.g., firing events during entity creation).
    /// </summary>
    internal ComponentEventHandlers ComponentEvents => componentEvents;

    #region Component Events

    /// <summary>
    /// Registers a handler to be called when a component of type <typeparamref name="T"/>
    /// is added to an entity.
    /// </summary>
    /// <typeparam name="T">The component type to watch for additions.</typeparam>
    /// <param name="handler">The handler to invoke when the component is added.</param>
    /// <returns>A subscription that can be disposed to unsubscribe.</returns>
    internal EventSubscription OnComponentAdded<T>(Action<Entity, T> handler) where T : struct, IComponent
    {
        return componentEvents.OnAdded(handler);
    }

    /// <summary>
    /// Registers a handler to be called when a component of type <typeparamref name="T"/>
    /// is removed from an entity.
    /// </summary>
    /// <typeparam name="T">The component type to watch for removals.</typeparam>
    /// <param name="handler">The handler to invoke when the component is removed.</param>
    /// <returns>A subscription that can be disposed to unsubscribe.</returns>
    internal EventSubscription OnComponentRemoved<T>(Action<Entity> handler) where T : struct, IComponent
    {
        return componentEvents.OnRemoved<T>(handler);
    }

    /// <summary>
    /// Registers a handler to be called when a component of type <typeparamref name="T"/>
    /// is changed on an entity.
    /// </summary>
    /// <typeparam name="T">The component type to watch for changes.</typeparam>
    /// <param name="handler">
    /// The handler to invoke when the component is changed. Receives the entity,
    /// the old value, and the new value.
    /// </param>
    /// <returns>A subscription that can be disposed to unsubscribe.</returns>
    internal EventSubscription OnComponentChanged<T>(Action<Entity, T, T> handler) where T : struct, IComponent
    {
        return componentEvents.OnChanged(handler);
    }

    /// <summary>
    /// Fires the component added event.
    /// </summary>
    internal void FireComponentAdded<T>(Entity entity, in T component) where T : struct, IComponent
    {
        componentEvents.FireAdded(entity, in component);
    }

    /// <summary>
    /// Fires the component removed event.
    /// </summary>
    internal void FireComponentRemoved<T>(Entity entity) where T : struct, IComponent
    {
        componentEvents.FireRemoved<T>(entity);
    }

    /// <summary>
    /// Fires the component changed event.
    /// </summary>
    internal void FireComponentChanged<T>(Entity entity, in T oldValue, in T newValue) where T : struct, IComponent
    {
        componentEvents.FireChanged(entity, in oldValue, in newValue);
    }

    #endregion

    #region Entity Events

    /// <summary>
    /// Registers a handler to be called when an entity is created.
    /// </summary>
    /// <param name="handler">
    /// The handler to invoke when an entity is created. Receives the entity
    /// and its optional name (null if unnamed).
    /// </param>
    /// <returns>A subscription that can be disposed to unsubscribe.</returns>
    internal EventSubscription OnEntityCreated(Action<Entity, string?> handler)
    {
        return entityEvents.OnCreated(handler);
    }

    /// <summary>
    /// Registers a handler to be called when an entity is destroyed.
    /// </summary>
    /// <param name="handler">The handler to invoke when an entity is destroyed.</param>
    /// <returns>A subscription that can be disposed to unsubscribe.</returns>
    internal EventSubscription OnEntityDestroyed(Action<Entity> handler)
    {
        return entityEvents.OnDestroyed(handler);
    }

    /// <summary>
    /// Fires the entity created event.
    /// </summary>
    internal void FireEntityCreated(Entity entity, string? name)
    {
        entityEvents.FireCreated(entity, name);
    }

    /// <summary>
    /// Fires the entity destroyed event.
    /// </summary>
    internal void FireEntityDestroyed(Entity entity)
    {
        entityEvents.FireDestroyed(entity);
    }

    #endregion

    /// <summary>
    /// Clears all event handlers for all event types.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method is called during <see cref="World.Dispose()"/> to ensure all event
    /// subscriptions are properly cleaned up. After calling this method:
    /// </para>
    /// <list type="bullet">
    /// <item><description>All custom event handlers registered via <see cref="EventBus"/> are removed</description></item>
    /// <item><description>All component lifecycle handlers (added, removed, changed) are removed</description></item>
    /// <item><description>All entity lifecycle handlers (created, destroyed) are removed</description></item>
    /// <item><description>Existing <see cref="EventSubscription"/> objects become no-ops when disposed</description></item>
    /// </list>
    /// <para>
    /// This prevents potential memory leaks from long-lived subscribers holding references
    /// to the world or its components after disposal.
    /// </para>
    /// </remarks>
    internal void Clear()
    {
        eventBus.Clear();
        componentEvents.Clear();
        entityEvents.Clear();
    }
}
