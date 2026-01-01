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
/// <para>
/// This class is thread-safe: concurrent calls to <see cref="Dispose"/> from multiple
/// threads are handled correctly, with only the first call executing the unsubscribe action.
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
/// <param name="unsubscribe">The action to execute when disposing this subscription.</param>
public sealed class EventSubscription(Action unsubscribe) : IDisposable
{
    private readonly Action unsubscribeAction = unsubscribe;
    private int disposed;

    /// <summary>
    /// Unsubscribes from the event by removing the handler.
    /// </summary>
    /// <remarks>
    /// This method is idempotent: calling it multiple times has no additional effect
    /// after the first call. This method is thread-safe.
    /// </remarks>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) == 1)
        {
            return;
        }

        unsubscribeAction();
    }
}
