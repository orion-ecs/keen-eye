using KeenEyes.Events;

namespace KeenEyes;

public sealed partial class World
{
    #region Messaging

    /// <summary>
    /// Sends a message immediately to all subscribed handlers.
    /// </summary>
    /// <typeparam name="T">The message type to send.</typeparam>
    /// <param name="message">The message data to send.</param>
    /// <remarks>
    /// <para>
    /// Messages are dispatched synchronously to all handlers in registration order.
    /// If no handlers are subscribed for the message type, this method returns
    /// immediately with minimal overhead (a dictionary lookup that returns false).
    /// </para>
    /// <para>
    /// Use struct types for messages to minimize allocations. If a handler throws
    /// an exception, it will propagate to the caller and subsequent handlers will
    /// not be invoked.
    /// </para>
    /// <para>
    /// For deferred message delivery, use <see cref="QueueMessage{T}(in T)"/> instead.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Define a message type
    /// public readonly record struct DamageMessage(Entity Target, int Amount, Entity Source);
    ///
    /// // Subscribe to the message in a system
    /// var subscription = world.Subscribe&lt;DamageMessage&gt;(msg =>
    /// {
    ///     ref var health = ref world.Get&lt;Health&gt;(msg.Target);
    ///     health.Current -= msg.Amount;
    /// });
    ///
    /// // Send a message from another system
    /// world.Send(new DamageMessage(target, 25, attacker));
    /// </code>
    /// </example>
    /// <seealso cref="Subscribe{T}(Action{T})"/>
    /// <seealso cref="QueueMessage{T}(in T)"/>
    /// <seealso cref="HasMessageSubscribers{T}()"/>
    public void Send<T>(in T message)
        => messageManager.Send(in message);

    /// <summary>
    /// Subscribes a handler to messages of the specified type.
    /// </summary>
    /// <typeparam name="T">The message type to subscribe to.</typeparam>
    /// <param name="handler">The handler to invoke when messages of type <typeparamref name="T"/> are sent.</param>
    /// <returns>
    /// An <see cref="EventSubscription"/> that can be disposed to unsubscribe the handler.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="handler"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// Handlers are invoked synchronously in registration order when messages are sent.
    /// The same handler instance can be registered multiple times, in which case it will
    /// be invoked multiple times per message. Each registration returns a separate subscription.
    /// </para>
    /// <para>
    /// To unsubscribe, call <see cref="EventSubscription.Dispose"/> on the returned subscription.
    /// Subscriptions are idempotent: disposing the same subscription multiple times has no
    /// additional effect.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Subscribe to damage messages
    /// var subscription = world.Subscribe&lt;DamageMessage&gt;(msg =>
    /// {
    ///     Console.WriteLine($"Entity {msg.Target} took {msg.Amount} damage");
    /// });
    ///
    /// // Later, unsubscribe
    /// subscription.Dispose();
    /// </code>
    /// </example>
    /// <seealso cref="Send{T}(in T)"/>
    /// <seealso cref="HasMessageSubscribers{T}()"/>
    public EventSubscription Subscribe<T>(Action<T> handler)
        => messageManager.Subscribe(handler);

    /// <summary>
    /// Checks if there are any handlers subscribed to a specific message type.
    /// </summary>
    /// <typeparam name="T">The message type to check.</typeparam>
    /// <returns><c>true</c> if at least one handler is subscribed; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <para>
    /// This can be used to skip expensive message creation when no handlers are listening.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Skip creating expensive message data if no one is listening
    /// if (world.HasMessageSubscribers&lt;ExpensiveMessage&gt;())
    /// {
    ///     var messageData = CreateExpensiveMessageData();
    ///     world.Send(messageData);
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="Subscribe{T}(Action{T})"/>
    /// <seealso cref="GetMessageSubscriberCount{T}()"/>
    public bool HasMessageSubscribers<T>()
        => messageManager.HasSubscribers<T>();

    /// <summary>
    /// Gets the number of handlers subscribed to a specific message type.
    /// </summary>
    /// <typeparam name="T">The message type to check.</typeparam>
    /// <returns>The number of subscribed handlers for the message type.</returns>
    /// <remarks>
    /// This method is primarily useful for testing and debugging.
    /// </remarks>
    public int GetMessageSubscriberCount<T>()
        => messageManager.GetSubscriberCount<T>();

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
    /// <example>
    /// <code>
    /// // Queue messages during system updates
    /// foreach (var entity in world.Query&lt;Collision&gt;())
    /// {
    ///     world.QueueMessage(new CollisionMessage(entity, other));
    /// }
    ///
    /// // Process all queued messages at a specific point
    /// world.ProcessQueuedMessages();
    /// </code>
    /// </example>
    /// <seealso cref="ProcessQueuedMessages"/>
    /// <seealso cref="ProcessQueuedMessages{T}"/>
    /// <seealso cref="GetQueuedMessageCount{T}()"/>
    public void QueueMessage<T>(in T message)
        => messageManager.Queue(in message);

    /// <summary>
    /// Processes all queued messages, delivering them to subscribed handlers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Messages are processed in FIFO order within each message type. The order in which
    /// different message types are processed is not guaranteed.
    /// </para>
    /// <para>
    /// After processing, all message queues are cleared. If a handler throws an exception,
    /// the exception propagates and remaining messages may not be processed.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In your game loop, process queued messages at a specific point
    /// world.Update(deltaTime);
    /// world.ProcessQueuedMessages(); // Process all messages queued during Update
    /// </code>
    /// </example>
    /// <seealso cref="QueueMessage{T}(in T)"/>
    /// <seealso cref="ProcessQueuedMessages{T}"/>
    public void ProcessQueuedMessages()
        => messageManager.ProcessQueuedMessages();

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
    /// <example>
    /// <code>
    /// // Process only damage messages at this point
    /// world.ProcessQueuedMessages&lt;DamageMessage&gt;();
    ///
    /// // Physics messages are processed later
    /// world.ProcessQueuedMessages&lt;CollisionMessage&gt;();
    /// </code>
    /// </example>
    /// <seealso cref="QueueMessage{T}(in T)"/>
    /// <seealso cref="ProcessQueuedMessages"/>
    public void ProcessQueuedMessages<T>()
        => messageManager.ProcessQueuedMessages<T>();

    /// <summary>
    /// Gets the count of queued messages for a specific type.
    /// </summary>
    /// <typeparam name="T">The message type to check.</typeparam>
    /// <returns>The number of queued messages of the specified type.</returns>
    /// <remarks>
    /// This method is primarily useful for testing and debugging.
    /// </remarks>
    public int GetQueuedMessageCount<T>()
        => messageManager.GetQueuedMessageCount<T>();

    /// <summary>
    /// Gets the total count of all queued messages across all types.
    /// </summary>
    /// <returns>The total number of queued messages.</returns>
    /// <remarks>
    /// This method is primarily useful for testing and debugging.
    /// </remarks>
    public int GetTotalQueuedMessageCount()
        => messageManager.GetTotalQueuedMessageCount();

    /// <summary>
    /// Clears all queued messages without processing them.
    /// </summary>
    /// <remarks>
    /// Use this to discard pending messages when they are no longer relevant,
    /// such as when resetting game state or changing levels.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Clear all pending messages when resetting level
    /// world.ClearQueuedMessages();
    /// </code>
    /// </example>
    /// <seealso cref="ClearQueuedMessages{T}"/>
    public void ClearQueuedMessages()
        => messageManager.ClearQueuedMessages();

    /// <summary>
    /// Clears queued messages of a specific type without processing them.
    /// </summary>
    /// <typeparam name="T">The message type to clear.</typeparam>
    /// <example>
    /// <code>
    /// // Discard all pending collision messages
    /// world.ClearQueuedMessages&lt;CollisionMessage&gt;();
    /// </code>
    /// </example>
    /// <seealso cref="ClearQueuedMessages"/>
    public void ClearQueuedMessages<T>()
        => messageManager.ClearQueuedMessages<T>();

    #endregion
}
