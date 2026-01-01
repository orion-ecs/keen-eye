using System.Collections.Concurrent;

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
/// <para>
/// This class is thread-safe: subscriptions, unsubscriptions, and event firing
/// can occur concurrently from multiple threads.
/// </para>
/// </remarks>
internal sealed class ComponentEventHandlers
{
    // Component added: (Entity, T) -> void
    private readonly ConcurrentDictionary<Type, object> addedHandlers = new();

    // Component removed: (Entity) -> void (component value is gone)
    private readonly ConcurrentDictionary<Type, object> removedHandlers = new();

    // Component changed: (Entity, T oldValue, T newValue) -> void
    private readonly ConcurrentDictionary<Type, object> changedHandlers = new();

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

        var handlerList = (ThreadSafeHandlerList<Action<Entity, T>>)handlersObj;
        handlerList.InvokeAdded(entity, component);
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

        var handlerList = (ThreadSafeHandlerList<Action<Entity>>)handlersObj;
        handlerList.InvokeRemoved(entity);
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

        var handlerList = (ThreadSafeHandlerList<Action<Entity, T, T>>)handlersObj;
        handlerList.InvokeChanged(entity, oldValue, newValue);
    }

    #endregion

    /// <summary>
    /// Clears all registered component lifecycle handlers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method is called during world disposal to ensure all component event
    /// subscriptions are properly cleaned up. After calling this method:
    /// </para>
    /// <list type="bullet">
    /// <item><description>All "component added" handlers for all component types are removed</description></item>
    /// <item><description>All "component removed" handlers for all component types are removed</description></item>
    /// <item><description>All "component changed" handlers for all component types are removed</description></item>
    /// <item><description>Existing <see cref="EventSubscription"/> objects become no-ops when disposed</description></item>
    /// </list>
    /// <para>
    /// This prevents potential memory leaks from subscribers holding references to components
    /// or entities after world disposal.
    /// </para>
    /// </remarks>
    internal void Clear()
    {
        addedHandlers.Clear();
        removedHandlers.Clear();
        changedHandlers.Clear();
    }

    private static ThreadSafeHandlerList<THandler> GetOrCreateHandlerList<THandler>(
        ConcurrentDictionary<Type, object> handlers,
        Type componentType) where THandler : Delegate
    {
        return (ThreadSafeHandlerList<THandler>)handlers.GetOrAdd(
            componentType,
            static _ => new ThreadSafeHandlerList<THandler>());
    }

    /// <summary>
    /// A thread-safe wrapper for a list of handlers.
    /// </summary>
    private sealed class ThreadSafeHandlerList<THandler> where THandler : Delegate
    {
        private readonly Lock syncRoot = new();
        private readonly List<THandler> list = [];

        public void Add(THandler handler)
        {
            lock (syncRoot)
            {
                list.Add(handler);
            }
        }

        public void Remove(THandler handler)
        {
            lock (syncRoot)
            {
                list.Remove(handler);
            }
        }

        public void InvokeAdded<T>(Entity entity, in T component) where T : struct, IComponent
        {
            // Take a snapshot under lock, then invoke outside lock
            // This prevents deadlocks if handlers try to subscribe/unsubscribe
            THandler[] snapshot;
            lock (syncRoot)
            {
                if (list.Count == 0)
                {
                    return;
                }

                snapshot = [.. list];
            }

            // Invoke in reverse order to match original behavior
            for (int i = snapshot.Length - 1; i >= 0; i--)
            {
                ((Action<Entity, T>)(object)snapshot[i])(entity, component);
            }
        }

        public void InvokeRemoved(Entity entity)
        {
            THandler[] snapshot;
            lock (syncRoot)
            {
                if (list.Count == 0)
                {
                    return;
                }

                snapshot = [.. list];
            }

            for (int i = snapshot.Length - 1; i >= 0; i--)
            {
                ((Action<Entity>)(object)snapshot[i])(entity);
            }
        }

        public void InvokeChanged<T>(Entity entity, in T oldValue, in T newValue) where T : struct, IComponent
        {
            THandler[] snapshot;
            lock (syncRoot)
            {
                if (list.Count == 0)
                {
                    return;
                }

                snapshot = [.. list];
            }

            for (int i = snapshot.Length - 1; i >= 0; i--)
            {
                ((Action<Entity, T, T>)(object)snapshot[i])(entity, oldValue, newValue);
            }
        }
    }
}
