using KeenEyes.Events;

namespace KeenEyes.Testing.Events;

/// <summary>
/// A timestamped record of an event that was fired.
/// </summary>
/// <typeparam name="T">The type of event that was recorded.</typeparam>
/// <param name="Event">The event data.</param>
/// <param name="Timestamp">The timestamp when the event was recorded, in milliseconds.</param>
/// <param name="SequenceNumber">The sequence number of this event (order in which it was recorded).</param>
public readonly record struct RecordedEvent<T>(T Event, float Timestamp, int SequenceNumber);

/// <summary>
/// Records events of a specific type from an EventBus for later verification.
/// </summary>
/// <typeparam name="T">The type of event to record.</typeparam>
/// <remarks>
/// <para>
/// EventRecorder attaches to an <see cref="EventBus"/> and captures all events of type
/// <typeparamref name="T"/> along with timestamps. This enables verification of event
/// firing in tests without needing inline event handlers.
/// </para>
/// <para>
/// When used with a <see cref="TestClock"/>, timestamps are synchronized with the
/// simulation time. Without a clock, timestamps default to 0.
/// </para>
/// <para>
/// Use the extension methods in <see cref="EventAssertions"/> for fluent assertions on
/// recorded events.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var eventBus = world.Events;
/// var clock = new TestClock();
/// using var recorder = new EventRecorder&lt;DamageEvent&gt;(eventBus, clock);
///
/// // Fire some events
/// eventBus.Publish(new DamageEvent(entity, 50));
/// clock.Step();
/// eventBus.Publish(new DamageEvent(entity, 25));
///
/// // Verify events were recorded
/// Assert.Equal(2, recorder.Count);
/// Assert.Equal(50, recorder.Events[0].Event.Amount);
/// Assert.Equal(25, recorder.Events[1].Event.Amount);
///
/// // Or use fluent assertions
/// recorder.ShouldHaveFired();
/// recorder.ShouldHaveFiredTimes(2);
/// </code>
/// </example>
public sealed class EventRecorder<T> : IDisposable
{
    private readonly List<RecordedEvent<T>> events = [];
    private readonly EventSubscription subscription;
    private readonly Func<float> getTimestamp;
    private int sequenceCounter;
    private bool disposed;

    /// <summary>
    /// Creates a new event recorder attached to the specified event bus.
    /// </summary>
    /// <param name="eventBus">The event bus to record events from.</param>
    /// <param name="clock">Optional clock for timestamp synchronization. If null, timestamps default to 0.</param>
    public EventRecorder(EventBus eventBus, TestClock? clock = null)
    {
        ArgumentNullException.ThrowIfNull(eventBus);

        getTimestamp = clock is not null
            ? () => clock.CurrentTime
            : () => 0f;

        subscription = eventBus.Subscribe<T>(RecordEvent);
    }

    /// <summary>
    /// Gets all recorded events in order.
    /// </summary>
    public IReadOnlyList<RecordedEvent<T>> Events => events;

    /// <summary>
    /// Gets the number of events that have been recorded.
    /// </summary>
    public int Count => events.Count;

    /// <summary>
    /// Gets whether any events have been recorded.
    /// </summary>
    public bool HasEvents => events.Count > 0;

    /// <summary>
    /// Gets the last event that was recorded, or null if no events have been recorded.
    /// </summary>
    public T? LastEvent => events.Count > 0 ? events[^1].Event : default;

    /// <summary>
    /// Gets the last recorded event with its metadata, or null if no events have been recorded.
    /// </summary>
    public RecordedEvent<T>? LastRecordedEvent => events.Count > 0 ? events[^1] : null;

    /// <summary>
    /// Gets the first event that was recorded, or null if no events have been recorded.
    /// </summary>
    public T? FirstEvent => events.Count > 0 ? events[0].Event : default;

    /// <summary>
    /// Gets the first recorded event with its metadata, or null if no events have been recorded.
    /// </summary>
    public RecordedEvent<T>? FirstRecordedEvent => events.Count > 0 ? events[0] : null;

    /// <summary>
    /// Clears all recorded events.
    /// </summary>
    /// <remarks>
    /// This resets the event list but does not reset the sequence counter.
    /// Use this between test phases to isolate event verification.
    /// </remarks>
    public void Clear()
    {
        events.Clear();
    }

    /// <summary>
    /// Clears all recorded events and resets the sequence counter.
    /// </summary>
    public void Reset()
    {
        events.Clear();
        sequenceCounter = 0;
    }

    /// <summary>
    /// Gets all events matching a predicate.
    /// </summary>
    /// <param name="predicate">The predicate to match events against.</param>
    /// <returns>Events matching the predicate.</returns>
    public IEnumerable<RecordedEvent<T>> Where(Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return events.Where(e => predicate(e.Event));
    }

    /// <summary>
    /// Gets all events matching a predicate including metadata.
    /// </summary>
    /// <param name="predicate">The predicate to match recorded events against.</param>
    /// <returns>Recorded events matching the predicate.</returns>
    public IEnumerable<RecordedEvent<T>> WhereRecorded(Func<RecordedEvent<T>, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return events.Where(predicate);
    }

    /// <summary>
    /// Checks if any event matches the specified predicate.
    /// </summary>
    /// <param name="predicate">The predicate to match events against.</param>
    /// <returns>True if any event matches; otherwise, false.</returns>
    public bool Any(Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return events.Any(e => predicate(e.Event));
    }

    /// <summary>
    /// Counts events matching a predicate.
    /// </summary>
    /// <param name="predicate">The predicate to match events against.</param>
    /// <returns>The number of matching events.</returns>
    public int CountMatching(Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return events.Count(e => predicate(e.Event));
    }

    /// <summary>
    /// Stops recording events and cleans up resources.
    /// </summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        subscription.Dispose();
    }

    private void RecordEvent(T evt)
    {
        events.Add(new RecordedEvent<T>(evt, getTimestamp(), sequenceCounter++));
    }
}
