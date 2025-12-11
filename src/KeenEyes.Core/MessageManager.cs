namespace KeenEyes;

/// <summary>
/// Non-generic interface for type-erased message queue operations.
/// Enables storing queues of different types in a single collection without reflection.
/// </summary>
internal interface IMessageQueue
{
    /// <summary>
    /// Processes all queued messages, delivering them to registered handlers.
    /// </summary>
    void ProcessQueue(object handlersObj);

    /// <summary>
    /// Clears all queued messages without processing them.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets the number of queued messages.
    /// </summary>
    int Count { get; }
}

/// <summary>
/// Typed message queue wrapper that implements the non-generic interface.
/// </summary>
internal sealed class MessageQueue<T> : IMessageQueue
{
    private readonly Queue<T> queue = new();

    public int Count => queue.Count;

    public void Enqueue(in T message) => queue.Enqueue(message);

    public void ProcessQueue(object handlersObj)
    {
        var handlerList = (List<Action<T>>)handlersObj;

        while (queue.Count > 0)
        {
            var message = queue.Dequeue();

            // Dispatch to handlers in reverse order for safe unsubscription
            for (int i = handlerList.Count - 1; i >= 0; i--)
            {
                handlerList[i](message);
            }
        }
    }

    public void Clear() => queue.Clear();
}

/// <summary>
/// Manages inter-system messaging for a world, providing a typed message bus
/// for decoupled communication between systems.
/// </summary>
/// <remarks>
/// <para>
/// This is an internal manager class that consolidates all message-related operations.
/// The public API is exposed through <see cref="World"/>.
/// </para>
/// <para>
/// The message manager provides two modes of message delivery:
/// </para>
/// <list type="bullet">
/// <item><description>Immediate delivery via <see cref="Send{T}(in T)"/> - messages are dispatched synchronously to all handlers</description></item>
/// <item><description>Deferred delivery via <see cref="Queue{T}(in T)"/> - messages are stored and processed later when <see cref="ProcessQueuedMessages"/> is called</description></item>
/// </list>
/// <para>
/// Messages should be struct types to minimize allocations. Handlers are invoked
/// synchronously in registration order.
/// </para>
/// </remarks>
internal sealed class MessageManager
{
    private readonly Dictionary<Type, object> handlers = [];
    private readonly Dictionary<Type, IMessageQueue> messageQueues = [];

    #region Subscription

    /// <summary>
    /// Subscribes a handler to messages of the specified type.
    /// </summary>
    /// <typeparam name="T">The message type to subscribe to.</typeparam>
    /// <param name="handler">The handler to invoke when messages of type <typeparamref name="T"/> are sent.</param>
    /// <returns>
    /// An <see cref="EventSubscription"/> that can be disposed to unsubscribe the handler.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="handler"/> is null.</exception>
    internal EventSubscription Subscribe<T>(Action<T> handler)
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
    /// Checks if there are any handlers registered for a specific message type.
    /// </summary>
    /// <typeparam name="T">The message type to check.</typeparam>
    /// <returns><c>true</c> if at least one handler is registered; otherwise, <c>false</c>.</returns>
    internal bool HasSubscribers<T>()
    {
        return handlers.TryGetValue(typeof(T), out var handlersObj)
            && ((List<Action<T>>)handlersObj).Count > 0;
    }

    /// <summary>
    /// Gets the number of handlers registered for a specific message type.
    /// </summary>
    /// <typeparam name="T">The message type to check.</typeparam>
    /// <returns>The number of registered handlers for the message type.</returns>
    internal int GetSubscriberCount<T>()
    {
        if (!handlers.TryGetValue(typeof(T), out var handlersObj))
        {
            return 0;
        }

        return ((List<Action<T>>)handlersObj).Count;
    }

    #endregion

    #region Immediate Delivery

    /// <summary>
    /// Sends a message immediately to all registered handlers.
    /// </summary>
    /// <typeparam name="T">The message type to send.</typeparam>
    /// <param name="message">The message data to pass to handlers.</param>
    /// <remarks>
    /// <para>
    /// Handlers are invoked synchronously in registration order. If no handlers are
    /// registered for the message type, this method returns immediately with minimal overhead.
    /// </para>
    /// <para>
    /// If a handler throws an exception, it will propagate to the caller and subsequent
    /// handlers will not be invoked.
    /// </para>
    /// </remarks>
    internal void Send<T>(in T message)
    {
        if (!handlers.TryGetValue(typeof(T), out var handlersObj))
        {
            return;
        }

        var handlerList = (List<Action<T>>)handlersObj;
        // Iterate in reverse to allow handlers to unsubscribe during iteration
        for (int i = handlerList.Count - 1; i >= 0; i--)
        {
            handlerList[i](message);
        }
    }

    #endregion

    #region Deferred Delivery

    /// <summary>
    /// Queues a message for deferred delivery.
    /// </summary>
    /// <typeparam name="T">The message type to queue.</typeparam>
    /// <param name="message">The message data to queue.</param>
    /// <remarks>
    /// <para>
    /// Queued messages are stored and delivered when <see cref="ProcessQueuedMessages"/> is called.
    /// This is useful when you want to collect messages during a system's update and process
    /// them all at once, or when you want to ensure messages are processed at a specific
    /// point in the update cycle.
    /// </para>
    /// <para>
    /// Messages are processed in FIFO order (first-in, first-out) within each message type.
    /// </para>
    /// </remarks>
    internal void Queue<T>(in T message)
    {
        var queue = GetOrCreateMessageQueue<T>();
        queue.Enqueue(message);
    }

    /// <summary>
    /// Processes all queued messages, delivering them to registered handlers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Messages are processed in FIFO order within each message type. The order in which
    /// different message types are processed is not guaranteed.
    /// </para>
    /// <para>
    /// After processing, all message queues are cleared. If a handler throws an exception,
    /// remaining messages in that type's queue will not be processed, but the queue will
    /// be cleared.
    /// </para>
    /// </remarks>
    internal void ProcessQueuedMessages()
    {
        foreach (var kvp in messageQueues)
        {
            var messageType = kvp.Key;
            var queue = kvp.Value;

            // Get the handler list for this message type
            if (!handlers.TryGetValue(messageType, out var handlersObj))
            {
                // No handlers, clear the queue and continue
                queue.Clear();
                continue;
            }

            // Process the queue using the typed interface (no reflection)
            queue.ProcessQueue(handlersObj);
        }

        // Clear all queues after processing
        foreach (var queue in messageQueues.Values)
        {
            queue.Clear();
        }
    }

    /// <summary>
    /// Processes all queued messages of a specific type.
    /// </summary>
    /// <typeparam name="T">The message type to process.</typeparam>
    /// <remarks>
    /// <para>
    /// Only processes messages of the specified type. Other queued messages remain queued.
    /// This is useful when you need fine-grained control over when specific message types
    /// are processed.
    /// </para>
    /// </remarks>
    internal void ProcessQueuedMessages<T>()
    {
        if (!messageQueues.TryGetValue(typeof(T), out var queueObj))
        {
            return;
        }

        var queue = (MessageQueue<T>)queueObj;

        if (!handlers.TryGetValue(typeof(T), out var handlersObj))
        {
            // No handlers, clear the queue
            queue.Clear();
            return;
        }

        // Process using the typed interface (no reflection)
        queue.ProcessQueue(handlersObj);
    }

    /// <summary>
    /// Gets the count of queued messages for a specific type.
    /// </summary>
    /// <typeparam name="T">The message type to check.</typeparam>
    /// <returns>The number of queued messages.</returns>
    internal int GetQueuedMessageCount<T>()
    {
        if (!messageQueues.TryGetValue(typeof(T), out var queue))
        {
            return 0;
        }

        return queue.Count;
    }

    /// <summary>
    /// Gets the total count of all queued messages across all types.
    /// </summary>
    /// <returns>The total number of queued messages.</returns>
    internal int GetTotalQueuedMessageCount()
    {
        int total = 0;
        foreach (var queue in messageQueues.Values)
        {
            total += queue.Count;
        }
        return total;
    }

    /// <summary>
    /// Clears all queued messages without processing them.
    /// </summary>
    internal void ClearQueuedMessages()
    {
        foreach (var queue in messageQueues.Values)
        {
            queue.Clear();
        }
    }

    /// <summary>
    /// Clears queued messages of a specific type without processing them.
    /// </summary>
    /// <typeparam name="T">The message type to clear.</typeparam>
    internal void ClearQueuedMessages<T>()
    {
        if (messageQueues.TryGetValue(typeof(T), out var queue))
        {
            queue.Clear();
        }
    }

    #endregion

    #region Cleanup

    /// <summary>
    /// Clears all handlers and queued messages.
    /// </summary>
    internal void Clear()
    {
        handlers.Clear();
        messageQueues.Clear();
    }

    #endregion

    #region Private Helpers

    private List<Action<T>> GetOrCreateHandlerList<T>()
    {
        if (!handlers.TryGetValue(typeof(T), out var handlersObj))
        {
            var handlerList = new List<Action<T>>();
            handlers[typeof(T)] = handlerList;
            return handlerList;
        }

        return (List<Action<T>>)handlersObj;
    }

    private MessageQueue<T> GetOrCreateMessageQueue<T>()
    {
        if (!messageQueues.TryGetValue(typeof(T), out var queueObj))
        {
            var queue = new MessageQueue<T>();
            messageQueues[typeof(T)] = queue;
            return queue;
        }

        return (MessageQueue<T>)queueObj;
    }

    #endregion
}
