using KeenEyes.Events;
using KeenEyes.Testing.Events;

namespace KeenEyes.Testing.Tests.Events;

public partial class EventAssertionsTests
{
    [Component]
    private partial struct TestEvent
    {
        public int Value;
    }

    [Component]
    private partial struct SecondEvent
    {
#pragma warning disable CS0649 // Field never assigned - used for type registration testing
        public string Name;
#pragma warning restore CS0649
    }

    #region ShouldHaveFired Tests

    [Fact]
    public void ShouldHaveFired_WithEvents_DoesNotThrow()
    {
        using var world = new World();
        var recorder = new EventRecorder<TestEvent>(world.Events);
        world.Events.Publish(new TestEvent { Value = 1 });

        var result = recorder.ShouldHaveFired();

        Assert.Same(recorder, result);
    }

    [Fact]
    public void ShouldHaveFired_WithNoEvents_ThrowsEventAssertionException()
    {
        using var world = new World();
        var recorder = new EventRecorder<TestEvent>(world.Events);

        var ex = Assert.Throws<EventAssertionException>(() => recorder.ShouldHaveFired());

        Assert.Contains("Expected event of type TestEvent to have been fired", ex.Message);
    }

    [Fact]
    public void ShouldHaveFired_WithNullRecorder_ThrowsArgumentNullException()
    {
        EventRecorder<TestEvent> recorder = null!;

        Assert.Throws<ArgumentNullException>(() => recorder.ShouldHaveFired());
    }

    #endregion

    #region ShouldNotHaveFired Tests

    [Fact]
    public void ShouldNotHaveFired_WithNoEvents_DoesNotThrow()
    {
        using var world = new World();
        var recorder = new EventRecorder<TestEvent>(world.Events);

        var result = recorder.ShouldNotHaveFired();

        Assert.Same(recorder, result);
    }

    [Fact]
    public void ShouldNotHaveFired_WithEvents_ThrowsEventAssertionException()
    {
        using var world = new World();
        var recorder = new EventRecorder<TestEvent>(world.Events);
        world.Events.Publish(new TestEvent { Value = 1 });
        world.Events.Publish(new TestEvent { Value = 2 });

        var ex = Assert.Throws<EventAssertionException>(() => recorder.ShouldNotHaveFired());

        Assert.Contains("Expected event of type TestEvent not to have been fired", ex.Message);
        Assert.Contains("but 2 event(s) were recorded", ex.Message);
    }

    #endregion

    #region ShouldHaveFiredTimes Tests

    [Fact]
    public void ShouldHaveFiredTimes_WithMatchingCount_DoesNotThrow()
    {
        using var world = new World();
        var recorder = new EventRecorder<TestEvent>(world.Events);
        world.Events.Publish(new TestEvent { Value = 1 });
        world.Events.Publish(new TestEvent { Value = 2 });
        world.Events.Publish(new TestEvent { Value = 3 });

        var result = recorder.ShouldHaveFiredTimes(3);

        Assert.Same(recorder, result);
    }

    [Fact]
    public void ShouldHaveFiredTimes_WithMismatchedCount_ThrowsEventAssertionException()
    {
        using var world = new World();
        var recorder = new EventRecorder<TestEvent>(world.Events);
        world.Events.Publish(new TestEvent { Value = 1 });

        var ex = Assert.Throws<EventAssertionException>(() => recorder.ShouldHaveFiredTimes(3));

        Assert.Contains("Expected 3 event(s) of type TestEvent", ex.Message);
        Assert.Contains("but 1 event(s) were recorded", ex.Message);
    }

    #endregion

    #region ShouldHaveFiredAtLeast Tests

    [Fact]
    public void ShouldHaveFiredAtLeast_WithSufficientEvents_DoesNotThrow()
    {
        using var world = new World();
        var recorder = new EventRecorder<TestEvent>(world.Events);
        world.Events.Publish(new TestEvent { Value = 1 });
        world.Events.Publish(new TestEvent { Value = 2 });
        world.Events.Publish(new TestEvent { Value = 3 });

        var result = recorder.ShouldHaveFiredAtLeast(2);

        Assert.Same(recorder, result);
    }

    [Fact]
    public void ShouldHaveFiredAtLeast_WithExactCount_DoesNotThrow()
    {
        using var world = new World();
        var recorder = new EventRecorder<TestEvent>(world.Events);
        world.Events.Publish(new TestEvent { Value = 1 });
        world.Events.Publish(new TestEvent { Value = 2 });

        var result = recorder.ShouldHaveFiredAtLeast(2);

        Assert.Same(recorder, result);
    }

    [Fact]
    public void ShouldHaveFiredAtLeast_WithInsufficientEvents_ThrowsEventAssertionException()
    {
        using var world = new World();
        var recorder = new EventRecorder<TestEvent>(world.Events);
        world.Events.Publish(new TestEvent { Value = 1 });

        var ex = Assert.Throws<EventAssertionException>(() => recorder.ShouldHaveFiredAtLeast(3));

        Assert.Contains("Expected at least 3 event(s) of type TestEvent", ex.Message);
        Assert.Contains("but only 1 event(s) were recorded", ex.Message);
    }

    #endregion

    #region ShouldHaveFiredAtMost Tests

    [Fact]
    public void ShouldHaveFiredAtMost_WithFewerEvents_DoesNotThrow()
    {
        using var world = new World();
        var recorder = new EventRecorder<TestEvent>(world.Events);
        world.Events.Publish(new TestEvent { Value = 1 });

        var result = recorder.ShouldHaveFiredAtMost(3);

        Assert.Same(recorder, result);
    }

    [Fact]
    public void ShouldHaveFiredAtMost_WithExactCount_DoesNotThrow()
    {
        using var world = new World();
        var recorder = new EventRecorder<TestEvent>(world.Events);
        world.Events.Publish(new TestEvent { Value = 1 });
        world.Events.Publish(new TestEvent { Value = 2 });

        var result = recorder.ShouldHaveFiredAtMost(2);

        Assert.Same(recorder, result);
    }

    [Fact]
    public void ShouldHaveFiredAtMost_WithTooManyEvents_ThrowsEventAssertionException()
    {
        using var world = new World();
        var recorder = new EventRecorder<TestEvent>(world.Events);
        world.Events.Publish(new TestEvent { Value = 1 });
        world.Events.Publish(new TestEvent { Value = 2 });
        world.Events.Publish(new TestEvent { Value = 3 });

        var ex = Assert.Throws<EventAssertionException>(() => recorder.ShouldHaveFiredAtMost(2));

        Assert.Contains("Expected at most 2 event(s) of type TestEvent", ex.Message);
        Assert.Contains("but 3 event(s) were recorded", ex.Message);
    }

    #endregion

    #region ShouldHaveFiredMatching Tests

    [Fact]
    public void ShouldHaveFiredMatching_WithMatchingEvent_DoesNotThrow()
    {
        using var world = new World();
        var recorder = new EventRecorder<TestEvent>(world.Events);
        world.Events.Publish(new TestEvent { Value = 10 });
        world.Events.Publish(new TestEvent { Value = 20 });

        var result = recorder.ShouldHaveFiredMatching(e => e.Value == 20);

        Assert.Same(recorder, result);
    }

    [Fact]
    public void ShouldHaveFiredMatching_WithNoMatchingEvent_ThrowsEventAssertionException()
    {
        using var world = new World();
        var recorder = new EventRecorder<TestEvent>(world.Events);
        world.Events.Publish(new TestEvent { Value = 10 });

        var ex = Assert.Throws<EventAssertionException>(() => recorder.ShouldHaveFiredMatching(e => e.Value > 50));

        Assert.Contains("Expected event of type TestEvent matching the predicate", ex.Message);
        Assert.Contains("but no matching events were found among 1 recorded event(s)", ex.Message);
    }

    [Fact]
    public void ShouldHaveFiredMatching_WithNullPredicate_ThrowsArgumentNullException()
    {
        using var world = new World();
        var recorder = new EventRecorder<TestEvent>(world.Events);

        Assert.Throws<ArgumentNullException>(() => recorder.ShouldHaveFiredMatching(null!));
    }

    #endregion

    #region ShouldNotHaveFiredMatching Tests

    [Fact]
    public void ShouldNotHaveFiredMatching_WithNoMatchingEvents_DoesNotThrow()
    {
        using var world = new World();
        var recorder = new EventRecorder<TestEvent>(world.Events);
        world.Events.Publish(new TestEvent { Value = 10 });

        var result = recorder.ShouldNotHaveFiredMatching(e => e.Value > 50);

        Assert.Same(recorder, result);
    }

    [Fact]
    public void ShouldNotHaveFiredMatching_WithMatchingEvents_ThrowsEventAssertionException()
    {
        using var world = new World();
        var recorder = new EventRecorder<TestEvent>(world.Events);
        world.Events.Publish(new TestEvent { Value = 10 });
        world.Events.Publish(new TestEvent { Value = 20 });

        var ex = Assert.Throws<EventAssertionException>(() => recorder.ShouldNotHaveFiredMatching(e => e.Value > 5));

        Assert.Contains("Expected no events of type TestEvent matching the predicate", ex.Message);
        Assert.Contains("but 2 matching event(s) were found", ex.Message);
    }

    #endregion

    #region ShouldHaveFiredMatchingTimes Tests

    [Fact]
    public void ShouldHaveFiredMatchingTimes_WithMatchingCount_DoesNotThrow()
    {
        using var world = new World();
        var recorder = new EventRecorder<TestEvent>(world.Events);
        world.Events.Publish(new TestEvent { Value = 10 });
        world.Events.Publish(new TestEvent { Value = 20 });
        world.Events.Publish(new TestEvent { Value = 30 });

        var result = recorder.ShouldHaveFiredMatchingTimes(2, e => e.Value >= 20);

        Assert.Same(recorder, result);
    }

    [Fact]
    public void ShouldHaveFiredMatchingTimes_WithMismatchedCount_ThrowsEventAssertionException()
    {
        using var world = new World();
        var recorder = new EventRecorder<TestEvent>(world.Events);
        world.Events.Publish(new TestEvent { Value = 10 });
        world.Events.Publish(new TestEvent { Value = 20 });

        var ex = Assert.Throws<EventAssertionException>(() => recorder.ShouldHaveFiredMatchingTimes(3, e => e.Value > 0));

        Assert.Contains("Expected 3 event(s) of type TestEvent matching the predicate", ex.Message);
        Assert.Contains("but 2 matching event(s) were found", ex.Message);
    }

    #endregion

    #region ShouldHaveFiredInOrder Tests

    [Fact]
    public void ShouldHaveFiredInOrder_WithEventsInOrder_DoesNotThrow()
    {
        using var world = new World();
        var recorder = new EventRecorder<TestEvent>(world.Events);
        world.Events.Publish(new TestEvent { Value = 10 });
        world.Events.Publish(new TestEvent { Value = 20 });
        world.Events.Publish(new TestEvent { Value = 30 });

        var result = recorder.ShouldHaveFiredInOrder(
            e => e.Value == 10,
            e => e.Value == 20,
            e => e.Value == 30);

        Assert.Same(recorder, result);
    }

    [Fact]
    public void ShouldHaveFiredInOrder_WithEventsInOrderAndExtraEvents_DoesNotThrow()
    {
        using var world = new World();
        var recorder = new EventRecorder<TestEvent>(world.Events);
        world.Events.Publish(new TestEvent { Value = 10 });
        world.Events.Publish(new TestEvent { Value = 15 });
        world.Events.Publish(new TestEvent { Value = 20 });
        world.Events.Publish(new TestEvent { Value = 25 });

        var result = recorder.ShouldHaveFiredInOrder(
            e => e.Value == 10,
            e => e.Value == 20);

        Assert.Same(recorder, result);
    }

    [Fact]
    public void ShouldHaveFiredInOrder_WithEventsOutOfOrder_ThrowsEventAssertionException()
    {
        using var world = new World();
        var recorder = new EventRecorder<TestEvent>(world.Events);
        world.Events.Publish(new TestEvent { Value = 20 });
        world.Events.Publish(new TestEvent { Value = 10 });

        var ex = Assert.Throws<EventAssertionException>(() =>
            recorder.ShouldHaveFiredInOrder(
                e => e.Value == 10,
                e => e.Value == 20));

        Assert.Contains("Expected events of type TestEvent in specified order", ex.Message);
        Assert.Contains("Matched 1 of 2 predicates", ex.Message);
    }

    [Fact]
    public void ShouldHaveFiredInOrder_WithEmptyPredicates_DoesNotThrow()
    {
        using var world = new World();
        var recorder = new EventRecorder<TestEvent>(world.Events);

        var result = recorder.ShouldHaveFiredInOrder();

        Assert.Same(recorder, result);
    }

    #endregion

    #region ShouldHaveFiredExactlyInOrder Tests

    [Fact]
    public void ShouldHaveFiredExactlyInOrder_WithExactMatch_DoesNotThrow()
    {
        using var world = new World();
        var recorder = new EventRecorder<TestEvent>(world.Events);
        world.Events.Publish(new TestEvent { Value = 10 });
        world.Events.Publish(new TestEvent { Value = 20 });

        var result = recorder.ShouldHaveFiredExactlyInOrder(
            e => e.Value == 10,
            e => e.Value == 20);

        Assert.Same(recorder, result);
    }

    [Fact]
    public void ShouldHaveFiredExactlyInOrder_WithExtraEvents_ThrowsEventAssertionException()
    {
        using var world = new World();
        var recorder = new EventRecorder<TestEvent>(world.Events);
        world.Events.Publish(new TestEvent { Value = 10 });
        world.Events.Publish(new TestEvent { Value = 15 });
        world.Events.Publish(new TestEvent { Value = 20 });

        var ex = Assert.Throws<EventAssertionException>(() =>
            recorder.ShouldHaveFiredExactlyInOrder(
                e => e.Value == 10,
                e => e.Value == 20));

        Assert.Contains("Expected exactly 2 event(s) of type TestEvent", ex.Message);
        Assert.Contains("but 3 event(s) were recorded", ex.Message);
    }

    [Fact]
    public void ShouldHaveFiredExactlyInOrder_WithMismatchedEvent_ThrowsEventAssertionException()
    {
        using var world = new World();
        var recorder = new EventRecorder<TestEvent>(world.Events);
        world.Events.Publish(new TestEvent { Value = 10 });
        world.Events.Publish(new TestEvent { Value = 30 });

        var ex = Assert.Throws<EventAssertionException>(() =>
            recorder.ShouldHaveFiredExactlyInOrder(
                e => e.Value == 10,
                e => e.Value == 20));

        Assert.Contains("Event at index 1 of type TestEvent did not match the expected predicate", ex.Message);
    }

    #endregion

    #region LastEventShouldMatch Tests

    [Fact]
    public void LastEventShouldMatch_WithMatchingLastEvent_DoesNotThrow()
    {
        using var world = new World();
        var recorder = new EventRecorder<TestEvent>(world.Events);
        world.Events.Publish(new TestEvent { Value = 10 });
        world.Events.Publish(new TestEvent { Value = 20 });

        var result = recorder.LastEventShouldMatch(e => e.Value == 20);

        Assert.Same(recorder, result);
    }

    [Fact]
    public void LastEventShouldMatch_WithNonMatchingLastEvent_ThrowsEventAssertionException()
    {
        using var world = new World();
        var recorder = new EventRecorder<TestEvent>(world.Events);
        world.Events.Publish(new TestEvent { Value = 10 });

        var ex = Assert.Throws<EventAssertionException>(() => recorder.LastEventShouldMatch(e => e.Value > 50));

        Assert.Contains("Last event of type TestEvent did not match the expected predicate", ex.Message);
    }

    [Fact]
    public void LastEventShouldMatch_WithNoEvents_ThrowsEventAssertionException()
    {
        using var world = new World();
        var recorder = new EventRecorder<TestEvent>(world.Events);

        var ex = Assert.Throws<EventAssertionException>(() => recorder.LastEventShouldMatch(e => e.Value > 0));

        Assert.Contains("Expected last event of type TestEvent to match predicate", ex.Message);
        Assert.Contains("but no events were recorded", ex.Message);
    }

    #endregion

    #region FirstEventShouldMatch Tests

    [Fact]
    public void FirstEventShouldMatch_WithMatchingFirstEvent_DoesNotThrow()
    {
        using var world = new World();
        var recorder = new EventRecorder<TestEvent>(world.Events);
        world.Events.Publish(new TestEvent { Value = 10 });
        world.Events.Publish(new TestEvent { Value = 20 });

        var result = recorder.FirstEventShouldMatch(e => e.Value == 10);

        Assert.Same(recorder, result);
    }

    [Fact]
    public void FirstEventShouldMatch_WithNonMatchingFirstEvent_ThrowsEventAssertionException()
    {
        using var world = new World();
        var recorder = new EventRecorder<TestEvent>(world.Events);
        world.Events.Publish(new TestEvent { Value = 10 });

        var ex = Assert.Throws<EventAssertionException>(() => recorder.FirstEventShouldMatch(e => e.Value > 50));

        Assert.Contains("First event of type TestEvent did not match the expected predicate", ex.Message);
    }

    [Fact]
    public void FirstEventShouldMatch_WithNoEvents_ThrowsEventAssertionException()
    {
        using var world = new World();
        var recorder = new EventRecorder<TestEvent>(world.Events);

        var ex = Assert.Throws<EventAssertionException>(() => recorder.FirstEventShouldMatch(e => e.Value > 0));

        Assert.Contains("Expected first event of type TestEvent to match predicate", ex.Message);
        Assert.Contains("but no events were recorded", ex.Message);
    }

    #endregion

    #region Chaining Tests

    [Fact]
    public void AssertionMethods_CanBeChained()
    {
        using var world = new World();
        var recorder = new EventRecorder<TestEvent>(world.Events);
        world.Events.Publish(new TestEvent { Value = 10 });
        world.Events.Publish(new TestEvent { Value = 20 });
        world.Events.Publish(new TestEvent { Value = 30 });

        recorder
            .ShouldHaveFired()
            .ShouldHaveFiredTimes(3)
            .ShouldHaveFiredAtLeast(2)
            .ShouldHaveFiredAtMost(5)
            .ShouldHaveFiredMatching(e => e.Value == 20)
            .ShouldNotHaveFiredMatching(e => e.Value > 100)
            .FirstEventShouldMatch(e => e.Value == 10)
            .LastEventShouldMatch(e => e.Value == 30);
    }

    #endregion
}
