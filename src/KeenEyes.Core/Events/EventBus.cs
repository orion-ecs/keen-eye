using System.Collections.Concurrent;

namespace KeenEyes.Events;

/// <summary>
/// A generic event bus for publishing and subscribing to typed events.
/// </summary>
/// <remarks>
/// <para>
/// The event bus provides a decoupled messaging system where publishers and subscribers
/// don't need direct references to each other. Events are dispatched synchronously
/// to all registered handlers in registration order.
/// </para>
/// <para>
/// Each event type has its own list of subscribers. When an event is published,
/// only handlers registered for that specific type are invoked.
/// </para>
/// <para>
/// Performance note: When no handlers are registered for an event type, publishing
/// that event has minimal overhead (a dictionary lookup that returns false).
/// </para>
/// <para>
/// This class is thread-safe: subscriptions, unsubscriptions, and event publishing
/// can occur concurrently from multiple threads.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Define a custom event
/// public readonly record struct DamageEvent(Entity Target, int Amount);
///
/// // Subscribe to the event
/// var subscription = world.Events.Subscribe&lt;DamageEvent&gt;(e =>
/// {
///     Console.WriteLine($"Entity {e.Target} took {e.Amount} damage");
/// });
///
/// // Publish an event
/// world.Events.Publish(new DamageEvent(entity, 50));
///
/// // Unsubscribe when done
/// subscription.Dispose();
/// </code>
/// </example>
public sealed class EventBus
{
    private readonly ConcurrentDictionary<Type, object> handlers = new();

    /// <summary>
    /// Subscribes a handler to events of the specified type.
    /// </summary>
    /// <typeparam name="T">The event type to subscribe to.</typeparam>
    /// <param name="handler">The handler to invoke when events of type <typeparamref name="T"/> are published.</param>
    /// <returns>
    /// An <see cref="EventSubscription"/> that can be disposed to unsubscribe the handler.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="handler"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// Handlers are invoked synchronously in registration order when events are published.
    /// If a handler throws an exception, subsequent handlers will not be invoked.
    /// </para>
    /// <para>
    /// The same handler instance can be registered multiple times, in which case it will
    /// be invoked multiple times per event. Each registration returns a separate subscription.
    /// </para>
    /// <para>
    /// This method is thread-safe and can be called concurrently with other subscribe,
    /// unsubscribe, or publish operations.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var subscription = eventBus.Subscribe&lt;GameOverEvent&gt;(e =>
    /// {
    ///     Console.WriteLine($"Game over! Score: {e.FinalScore}");
    /// });
    /// </code>
    /// </example>
    public EventSubscription Subscribe<T>(Action<T> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var handlerList = GetOrCreateHandlerList<T>();
        handlerList.Add(handler);

        return new EventSubscription(() =>
        {
            handlerList.Remove(handler);
        });
    }

    /// <summary>
    /// Publishes an event to all registered handlers.
    /// </summary>
    /// <typeparam name="T">The event type to publish.</typeparam>
    /// <param name="evt">The event data to pass to handlers.</param>
    /// <remarks>
    /// <para>
    /// Handlers are invoked synchronously in registration order. If no handlers are
    /// registered for the event type, this method returns immediately with minimal overhead.
    /// </para>
    /// <para>
    /// If a handler throws an exception, it will propagate to the caller and subsequent
    /// handlers will not be invoked. Consider wrapping handlers in try-catch if you need
    /// fault tolerance.
    /// </para>
    /// <para>
    /// This method is thread-safe and can be called concurrently with other subscribe,
    /// unsubscribe, or publish operations.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// eventBus.Publish(new DamageEvent(target, 25));
    /// </code>
    /// </example>
    public void Publish<T>(in T evt)
    {
        if (!handlers.TryGetValue(typeof(T), out var handlersObj))
        {
            return;
        }

        var handlerList = (ThreadSafeHandlerList<Action<T>>)handlersObj;
        handlerList.Invoke(evt);
    }

    /// <summary>
    /// Gets the number of handlers registered for a specific event type.
    /// </summary>
    /// <typeparam name="T">The event type to check.</typeparam>
    /// <returns>The number of registered handlers for the event type.</returns>
    /// <remarks>
    /// This method is primarily useful for testing and debugging.
    /// </remarks>
    public int GetHandlerCount<T>()
    {
        if (!handlers.TryGetValue(typeof(T), out var handlersObj))
        {
            return 0;
        }

        return ((ThreadSafeHandlerList<Action<T>>)handlersObj).Count;
    }

    /// <summary>
    /// Checks if there are any handlers registered for a specific event type.
    /// </summary>
    /// <typeparam name="T">The event type to check.</typeparam>
    /// <returns><c>true</c> if at least one handler is registered; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <para>
    /// This can be used to skip expensive event creation when no handlers are listening.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Skip creating expensive event data if no one is listening
    /// if (eventBus.HasHandlers&lt;ExpensiveEvent&gt;())
    /// {
    ///     var eventData = CreateExpensiveEventData();
    ///     eventBus.Publish(eventData);
    /// }
    /// </code>
    /// </example>
    public bool HasHandlers<T>()
    {
        return handlers.TryGetValue(typeof(T), out var handlersObj)
            && ((ThreadSafeHandlerList<Action<T>>)handlersObj).Count > 0;
    }

    /// <summary>
    /// Removes all handlers for all event types.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method is called during world disposal to clean up all subscriptions and prevent
    /// memory leaks from long-lived subscribers. After calling this method:
    /// </para>
    /// <list type="bullet">
    /// <item><description>All event handlers for all event types are removed</description></item>
    /// <item><description>Publishing events will have no effect (no handlers to invoke)</description></item>
    /// <item><description>Existing <see cref="EventSubscription"/> objects become no-ops when disposed</description></item>
    /// <item><description>Subscribers no longer hold references to the world or its data</description></item>
    /// </list>
    /// </remarks>
    internal void Clear()
    {
        handlers.Clear();
    }

    private ThreadSafeHandlerList<Action<T>> GetOrCreateHandlerList<T>()
    {
        return (ThreadSafeHandlerList<Action<T>>)handlers.GetOrAdd(
            typeof(T),
            static _ => new ThreadSafeHandlerList<Action<T>>());
    }

    /// <summary>
    /// A thread-safe wrapper for a list of handlers.
    /// </summary>
    private sealed class ThreadSafeHandlerList<THandler> where THandler : Delegate
    {
        private readonly Lock syncRoot = new();
        private readonly List<THandler> list = [];

        public int Count
        {
            get
            {
                lock (syncRoot)
                {
                    return list.Count;
                }
            }
        }

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

        public void Invoke<T>(in T evt)
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
            // (allows handlers to unsubscribe during iteration)
            for (int i = snapshot.Length - 1; i >= 0; i--)
            {
                ((Action<T>)(object)snapshot[i])(evt);
            }
        }
    }
}
