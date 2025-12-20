namespace KeenEyes.Testing.Events;

/// <summary>
/// Provides fluent assertion methods for verifying recorded events.
/// </summary>
/// <remarks>
/// <para>
/// These extension methods provide a fluent interface for asserting on events
/// captured by <see cref="EventRecorder{T}"/>. All assertions throw
/// <see cref="EventAssertionException"/> on failure with descriptive messages.
/// </para>
/// <para>
/// Assertions can be chained for multiple verifications:
/// </para>
/// <code>
/// recorder
///     .ShouldHaveFired()
///     .ShouldHaveFiredTimes(3)
///     .ShouldHaveFiredMatching(e => e.Amount > 10);
/// </code>
/// </remarks>
public static class EventAssertions
{
    /// <summary>
    /// Asserts that at least one event was recorded.
    /// </summary>
    /// <typeparam name="T">The event type.</typeparam>
    /// <param name="recorder">The event recorder to check.</param>
    /// <returns>The recorder for method chaining.</returns>
    /// <exception cref="EventAssertionException">Thrown when no events were recorded.</exception>
    public static EventRecorder<T> ShouldHaveFired<T>(this EventRecorder<T> recorder)
    {
        ArgumentNullException.ThrowIfNull(recorder);

        if (!recorder.HasEvents)
        {
            throw new EventAssertionException(
                $"Expected event of type {typeof(T).Name} to have been fired, but no events were recorded.");
        }

        return recorder;
    }

    /// <summary>
    /// Asserts that no events were recorded.
    /// </summary>
    /// <typeparam name="T">The event type.</typeparam>
    /// <param name="recorder">The event recorder to check.</param>
    /// <returns>The recorder for method chaining.</returns>
    /// <exception cref="EventAssertionException">Thrown when events were recorded.</exception>
    public static EventRecorder<T> ShouldNotHaveFired<T>(this EventRecorder<T> recorder)
    {
        ArgumentNullException.ThrowIfNull(recorder);

        if (recorder.HasEvents)
        {
            throw new EventAssertionException(
                $"Expected event of type {typeof(T).Name} not to have been fired, but {recorder.Count} event(s) were recorded.");
        }

        return recorder;
    }

    /// <summary>
    /// Asserts that exactly the specified number of events were recorded.
    /// </summary>
    /// <typeparam name="T">The event type.</typeparam>
    /// <param name="recorder">The event recorder to check.</param>
    /// <param name="expectedCount">The expected number of events.</param>
    /// <returns>The recorder for method chaining.</returns>
    /// <exception cref="EventAssertionException">Thrown when the event count doesn't match.</exception>
    public static EventRecorder<T> ShouldHaveFiredTimes<T>(this EventRecorder<T> recorder, int expectedCount)
    {
        ArgumentNullException.ThrowIfNull(recorder);

        if (recorder.Count != expectedCount)
        {
            throw new EventAssertionException(
                $"Expected {expectedCount} event(s) of type {typeof(T).Name}, but {recorder.Count} event(s) were recorded.");
        }

        return recorder;
    }

    /// <summary>
    /// Asserts that at least the specified number of events were recorded.
    /// </summary>
    /// <typeparam name="T">The event type.</typeparam>
    /// <param name="recorder">The event recorder to check.</param>
    /// <param name="minimumCount">The minimum expected number of events.</param>
    /// <returns>The recorder for method chaining.</returns>
    /// <exception cref="EventAssertionException">Thrown when fewer events were recorded.</exception>
    public static EventRecorder<T> ShouldHaveFiredAtLeast<T>(this EventRecorder<T> recorder, int minimumCount)
    {
        ArgumentNullException.ThrowIfNull(recorder);

        if (recorder.Count < minimumCount)
        {
            throw new EventAssertionException(
                $"Expected at least {minimumCount} event(s) of type {typeof(T).Name}, but only {recorder.Count} event(s) were recorded.");
        }

        return recorder;
    }

    /// <summary>
    /// Asserts that at most the specified number of events were recorded.
    /// </summary>
    /// <typeparam name="T">The event type.</typeparam>
    /// <param name="recorder">The event recorder to check.</param>
    /// <param name="maximumCount">The maximum expected number of events.</param>
    /// <returns>The recorder for method chaining.</returns>
    /// <exception cref="EventAssertionException">Thrown when more events were recorded.</exception>
    public static EventRecorder<T> ShouldHaveFiredAtMost<T>(this EventRecorder<T> recorder, int maximumCount)
    {
        ArgumentNullException.ThrowIfNull(recorder);

        if (recorder.Count > maximumCount)
        {
            throw new EventAssertionException(
                $"Expected at most {maximumCount} event(s) of type {typeof(T).Name}, but {recorder.Count} event(s) were recorded.");
        }

        return recorder;
    }

    /// <summary>
    /// Asserts that at least one event matching the predicate was recorded.
    /// </summary>
    /// <typeparam name="T">The event type.</typeparam>
    /// <param name="recorder">The event recorder to check.</param>
    /// <param name="predicate">The predicate to match events against.</param>
    /// <returns>The recorder for method chaining.</returns>
    /// <exception cref="EventAssertionException">Thrown when no matching events were found.</exception>
    public static EventRecorder<T> ShouldHaveFiredMatching<T>(this EventRecorder<T> recorder, Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(recorder);
        ArgumentNullException.ThrowIfNull(predicate);

        if (!recorder.Any(predicate))
        {
            throw new EventAssertionException(
                $"Expected event of type {typeof(T).Name} matching the predicate, but no matching events were found among {recorder.Count} recorded event(s).");
        }

        return recorder;
    }

    /// <summary>
    /// Asserts that no events matching the predicate were recorded.
    /// </summary>
    /// <typeparam name="T">The event type.</typeparam>
    /// <param name="recorder">The event recorder to check.</param>
    /// <param name="predicate">The predicate to match events against.</param>
    /// <returns>The recorder for method chaining.</returns>
    /// <exception cref="EventAssertionException">Thrown when matching events were found.</exception>
    public static EventRecorder<T> ShouldNotHaveFiredMatching<T>(this EventRecorder<T> recorder, Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(recorder);
        ArgumentNullException.ThrowIfNull(predicate);

        int matchCount = recorder.CountMatching(predicate);
        if (matchCount > 0)
        {
            throw new EventAssertionException(
                $"Expected no events of type {typeof(T).Name} matching the predicate, but {matchCount} matching event(s) were found.");
        }

        return recorder;
    }

    /// <summary>
    /// Asserts that exactly the specified number of events matching the predicate were recorded.
    /// </summary>
    /// <typeparam name="T">The event type.</typeparam>
    /// <param name="recorder">The event recorder to check.</param>
    /// <param name="expectedCount">The expected number of matching events.</param>
    /// <param name="predicate">The predicate to match events against.</param>
    /// <returns>The recorder for method chaining.</returns>
    /// <exception cref="EventAssertionException">Thrown when the matching event count doesn't match.</exception>
    public static EventRecorder<T> ShouldHaveFiredMatchingTimes<T>(
        this EventRecorder<T> recorder,
        int expectedCount,
        Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(recorder);
        ArgumentNullException.ThrowIfNull(predicate);

        int matchCount = recorder.CountMatching(predicate);
        if (matchCount != expectedCount)
        {
            throw new EventAssertionException(
                $"Expected {expectedCount} event(s) of type {typeof(T).Name} matching the predicate, but {matchCount} matching event(s) were found.");
        }

        return recorder;
    }

    /// <summary>
    /// Asserts that events were recorded in the specified order.
    /// </summary>
    /// <typeparam name="T">The event type.</typeparam>
    /// <param name="recorder">The event recorder to check.</param>
    /// <param name="predicates">Predicates that events must match in order.</param>
    /// <returns>The recorder for method chaining.</returns>
    /// <exception cref="EventAssertionException">Thrown when events don't match the expected order.</exception>
    /// <remarks>
    /// <para>
    /// This assertion checks that events matching the predicates were recorded in the
    /// specified order, but allows other events between them. For strict ordering (no
    /// events between), use <see cref="ShouldHaveFiredExactlyInOrder{T}"/>.
    /// </para>
    /// </remarks>
    public static EventRecorder<T> ShouldHaveFiredInOrder<T>(
        this EventRecorder<T> recorder,
        params Func<T, bool>[] predicates)
    {
        ArgumentNullException.ThrowIfNull(recorder);
        ArgumentNullException.ThrowIfNull(predicates);

        if (predicates.Length == 0)
        {
            return recorder;
        }

        int predicateIndex = 0;
        foreach (var recorded in recorder.Events)
        {
            if (predicates[predicateIndex](recorded.Event))
            {
                predicateIndex++;
                if (predicateIndex >= predicates.Length)
                {
                    break;
                }
            }
        }

        if (predicateIndex < predicates.Length)
        {
            throw new EventAssertionException(
                $"Expected events of type {typeof(T).Name} in specified order. Matched {predicateIndex} of {predicates.Length} predicates.");
        }

        return recorder;
    }

    /// <summary>
    /// Asserts that exactly the specified events were recorded in strict order.
    /// </summary>
    /// <typeparam name="T">The event type.</typeparam>
    /// <param name="recorder">The event recorder to check.</param>
    /// <param name="predicates">Predicates that events must match in exact order.</param>
    /// <returns>The recorder for method chaining.</returns>
    /// <exception cref="EventAssertionException">Thrown when events don't match exactly.</exception>
    /// <remarks>
    /// <para>
    /// This assertion requires that the number of recorded events equals the number of
    /// predicates, and each event matches its corresponding predicate in order.
    /// </para>
    /// </remarks>
    public static EventRecorder<T> ShouldHaveFiredExactlyInOrder<T>(
        this EventRecorder<T> recorder,
        params Func<T, bool>[] predicates)
    {
        ArgumentNullException.ThrowIfNull(recorder);
        ArgumentNullException.ThrowIfNull(predicates);

        if (recorder.Count != predicates.Length)
        {
            throw new EventAssertionException(
                $"Expected exactly {predicates.Length} event(s) of type {typeof(T).Name}, but {recorder.Count} event(s) were recorded.");
        }

        for (int i = 0; i < predicates.Length; i++)
        {
            if (!predicates[i](recorder.Events[i].Event))
            {
                throw new EventAssertionException(
                    $"Event at index {i} of type {typeof(T).Name} did not match the expected predicate.");
            }
        }

        return recorder;
    }

    /// <summary>
    /// Asserts that the last recorded event matches the predicate.
    /// </summary>
    /// <typeparam name="T">The event type.</typeparam>
    /// <param name="recorder">The event recorder to check.</param>
    /// <param name="predicate">The predicate the last event must match.</param>
    /// <returns>The recorder for method chaining.</returns>
    /// <exception cref="EventAssertionException">Thrown when no events or the last event doesn't match.</exception>
    public static EventRecorder<T> LastEventShouldMatch<T>(this EventRecorder<T> recorder, Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(recorder);
        ArgumentNullException.ThrowIfNull(predicate);

        if (!recorder.HasEvents)
        {
            throw new EventAssertionException(
                $"Expected last event of type {typeof(T).Name} to match predicate, but no events were recorded.");
        }

        if (!predicate(recorder.LastEvent!))
        {
            throw new EventAssertionException(
                $"Last event of type {typeof(T).Name} did not match the expected predicate.");
        }

        return recorder;
    }

    /// <summary>
    /// Asserts that the first recorded event matches the predicate.
    /// </summary>
    /// <typeparam name="T">The event type.</typeparam>
    /// <param name="recorder">The event recorder to check.</param>
    /// <param name="predicate">The predicate the first event must match.</param>
    /// <returns>The recorder for method chaining.</returns>
    /// <exception cref="EventAssertionException">Thrown when no events or the first event doesn't match.</exception>
    public static EventRecorder<T> FirstEventShouldMatch<T>(this EventRecorder<T> recorder, Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(recorder);
        ArgumentNullException.ThrowIfNull(predicate);

        if (!recorder.HasEvents)
        {
            throw new EventAssertionException(
                $"Expected first event of type {typeof(T).Name} to match predicate, but no events were recorded.");
        }

        if (!predicate(recorder.FirstEvent!))
        {
            throw new EventAssertionException(
                $"First event of type {typeof(T).Name} did not match the expected predicate.");
        }

        return recorder;
    }
}

/// <summary>
/// Exception thrown when an event assertion fails.
/// </summary>
public class EventAssertionException : Exception
{
    /// <summary>
    /// Creates a new event assertion exception with the specified message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public EventAssertionException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a new event assertion exception with the specified message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public EventAssertionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
