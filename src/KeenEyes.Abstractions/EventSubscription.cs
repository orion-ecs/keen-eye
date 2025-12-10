namespace KeenEyes;

/// <summary>
/// Represents a subscription to an event that can be unsubscribed by disposing.
/// </summary>
/// <remarks>
/// <para>
/// Event subscriptions are returned by event registration methods and implement
/// <see cref="IDisposable"/> to allow clean unsubscription. Disposing a subscription
/// removes the handler from the event source.
/// </para>
/// <para>
/// Subscriptions are idempotent: disposing the same subscription multiple times
/// has no additional effect after the first disposal.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Subscribe to an event
/// var subscription = world.OnEntityCreated((entity, name) =>
/// {
///     Console.WriteLine($"Entity created: {entity}");
/// });
///
/// // Later, unsubscribe by disposing
/// subscription.Dispose();
/// </code>
/// </example>
public sealed class EventSubscription : IDisposable
{
    private readonly Action unsubscribeAction;
    private bool disposed;

    /// <summary>
    /// Creates a new event subscription with the specified unsubscribe action.
    /// </summary>
    /// <param name="unsubscribe">The action to execute when disposing this subscription.</param>
    public EventSubscription(Action unsubscribe)
    {
        unsubscribeAction = unsubscribe;
    }

    /// <summary>
    /// Unsubscribes from the event by removing the handler.
    /// </summary>
    /// <remarks>
    /// This method is idempotent: calling it multiple times has no additional effect
    /// after the first call.
    /// </remarks>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        unsubscribeAction();
    }
}
