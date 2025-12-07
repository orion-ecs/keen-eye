namespace KeenEyes.Events;

/// <summary>
/// Manages typed component lifecycle event handlers.
/// </summary>
/// <remarks>
/// <para>
/// This class provides separate handler lists for each component type, enabling
/// efficient dispatching of component-specific events. Each component type has
/// its own list of handlers, so adding/removing handlers for one component type
/// doesn't affect others.
/// </para>
/// <para>
/// Performance note: When no handlers are registered for a component type,
/// event firing has minimal overhead (a dictionary lookup that returns false).
/// </para>
/// </remarks>
internal sealed class ComponentEventHandlers
{
    // Component added: (Entity, T) -> void
    private readonly Dictionary<Type, object> addedHandlers = [];

    // Component removed: (Entity) -> void (component value is gone)
    private readonly Dictionary<Type, object> removedHandlers = [];

    // Component changed: (Entity, T oldValue, T newValue) -> void
    private readonly Dictionary<Type, object> changedHandlers = [];

    #region Added Handlers

    /// <summary>
    /// Registers a handler to be called when a component of type <typeparamref name="T"/>
    /// is added to an entity.
    /// </summary>
    /// <typeparam name="T">The component type to watch for additions.</typeparam>
    /// <param name="handler">The handler to invoke when the component is added.</param>
    /// <returns>A subscription that can be disposed to unsubscribe.</returns>
    public EventSubscription OnAdded<T>(Action<Entity, T> handler) where T : struct, IComponent
    {
        var handlerList = GetOrCreateHandlerList<Action<Entity, T>>(addedHandlers, typeof(T));
        handlerList.Add(handler);

        return new EventSubscription(() =>
        {
            handlerList.Remove(handler);
        });
    }

    /// <summary>
    /// Fires the component added event for a specific component type.
    /// </summary>
    internal void FireAdded<T>(Entity entity, in T component) where T : struct, IComponent
    {
        if (!addedHandlers.TryGetValue(typeof(T), out var handlersObj))
        {
            return;
        }

        var handlerList = (List<Action<Entity, T>>)handlersObj;
        for (int i = handlerList.Count - 1; i >= 0; i--)
        {
            handlerList[i](entity, component);
        }
    }

    #endregion

    #region Removed Handlers

    /// <summary>
    /// Registers a handler to be called when a component of type <typeparamref name="T"/>
    /// is removed from an entity.
    /// </summary>
    /// <typeparam name="T">The component type to watch for removals.</typeparam>
    /// <param name="handler">The handler to invoke when the component is removed.</param>
    /// <returns>A subscription that can be disposed to unsubscribe.</returns>
    /// <remarks>
    /// <para>
    /// The handler receives only the entity, not the component value, because the
    /// component data may already be overwritten when the event fires.
    /// </para>
    /// </remarks>
    public EventSubscription OnRemoved<T>(Action<Entity> handler) where T : struct, IComponent
    {
        var handlerList = GetOrCreateHandlerList<Action<Entity>>(removedHandlers, typeof(T));
        handlerList.Add(handler);

        return new EventSubscription(() =>
        {
            handlerList.Remove(handler);
        });
    }

    /// <summary>
    /// Fires the component removed event for a specific component type.
    /// </summary>
    internal void FireRemoved<T>(Entity entity) where T : struct, IComponent
    {
        if (!removedHandlers.TryGetValue(typeof(T), out var handlersObj))
        {
            return;
        }

        var handlerList = (List<Action<Entity>>)handlersObj;
        for (int i = handlerList.Count - 1; i >= 0; i--)
        {
            handlerList[i](entity);
        }
    }

    #endregion

    #region Changed Handlers

    /// <summary>
    /// Registers a handler to be called when a component of type <typeparamref name="T"/>
    /// is changed on an entity via <see cref="World.Set{T}(Entity, in T)"/>.
    /// </summary>
    /// <typeparam name="T">The component type to watch for changes.</typeparam>
    /// <param name="handler">
    /// The handler to invoke when the component is changed. Receives the entity,
    /// the old component value, and the new component value.
    /// </param>
    /// <returns>A subscription that can be disposed to unsubscribe.</returns>
    /// <remarks>
    /// <para>
    /// This event is only fired when using <see cref="World.Set{T}(Entity, in T)"/>.
    /// Direct modifications via <see cref="World.Get{T}(Entity)"/> references do not
    /// trigger this event since there is no way to detect when a reference is modified.
    /// </para>
    /// </remarks>
    public EventSubscription OnChanged<T>(Action<Entity, T, T> handler) where T : struct, IComponent
    {
        var handlerList = GetOrCreateHandlerList<Action<Entity, T, T>>(changedHandlers, typeof(T));
        handlerList.Add(handler);

        return new EventSubscription(() =>
        {
            handlerList.Remove(handler);
        });
    }

    /// <summary>
    /// Fires the component changed event for a specific component type.
    /// </summary>
    internal void FireChanged<T>(Entity entity, in T oldValue, in T newValue) where T : struct, IComponent
    {
        if (!changedHandlers.TryGetValue(typeof(T), out var handlersObj))
        {
            return;
        }

        var handlerList = (List<Action<Entity, T, T>>)handlersObj;
        for (int i = handlerList.Count - 1; i >= 0; i--)
        {
            handlerList[i](entity, oldValue, newValue);
        }
    }

    #endregion

    /// <summary>
    /// Clears all registered handlers.
    /// </summary>
    internal void Clear()
    {
        addedHandlers.Clear();
        removedHandlers.Clear();
        changedHandlers.Clear();
    }

    private static List<THandler> GetOrCreateHandlerList<THandler>(Dictionary<Type, object> handlers, Type componentType)
    {
        if (!handlers.TryGetValue(componentType, out var handlersObj))
        {
            var handlerList = new List<THandler>();
            handlers[componentType] = handlerList;
            return handlerList;
        }

        return (List<THandler>)handlersObj;
    }
}
