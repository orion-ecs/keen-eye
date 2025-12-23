using KeenEyes.Events;
using KeenEyes.Testing.Events;

namespace KeenEyes.Testing.Tests.Events;

public class EventRecorderTests
{
    private record struct TestEvent(int Value, string Message);
    private record struct DamageEvent(int Amount, Entity Target);

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidEventBus_CreatesRecorder()
    {
        using var world = new World();
        var eventBus = world.Events;

        using var recorder = new EventRecorder<TestEvent>(eventBus);

        Assert.NotNull(recorder);
        Assert.Empty(recorder.Events);
        Assert.Equal(0, recorder.Count);
        Assert.False(recorder.HasEvents);
    }

    [Fact]
    public void Constructor_WithNullEventBus_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new EventRecorder<TestEvent>(null!));
    }

    [Fact]
    public void Constructor_WithClock_UsesClockForTimestamps()
    {
        using var world = new World();
        var eventBus = world.Events;
        var clock = new TestClock();
        clock.SetTime(100f);

        using var recorder = new EventRecorder<TestEvent>(eventBus, clock);
        eventBus.Publish(new TestEvent(1, "Test"));

        Assert.Single(recorder.Events);
        Assert.Equal(100f, recorder.Events[0].Timestamp);
    }

    [Fact]
    public void Constructor_WithoutClock_UsesZeroTimestamps()
    {
        using var world = new World();
        var eventBus = world.Events;

        using var recorder = new EventRecorder<TestEvent>(eventBus);
        eventBus.Publish(new TestEvent(1, "Test"));

        Assert.Single(recorder.Events);
        Assert.Equal(0f, recorder.Events[0].Timestamp);
    }

    #endregion

    #region Event Recording Tests

    [Fact]
    public void RecordEvent_WhenEventPublished_RecordsEvent()
    {
        using var world = new World();
        var eventBus = world.Events;
        using var recorder = new EventRecorder<TestEvent>(eventBus);

        var testEvent = new TestEvent(42, "Hello");
        eventBus.Publish(testEvent);

        Assert.Single(recorder.Events);
        Assert.Equal(42, recorder.Events[0].Event.Value);
        Assert.Equal("Hello", recorder.Events[0].Event.Message);
    }

    [Fact]
    public void RecordEvent_MultipleEvents_RecordsAllInOrder()
    {
        using var world = new World();
        var eventBus = world.Events;
        using var recorder = new EventRecorder<TestEvent>(eventBus);

        eventBus.Publish(new TestEvent(1, "First"));
        eventBus.Publish(new TestEvent(2, "Second"));
        eventBus.Publish(new TestEvent(3, "Third"));

        Assert.Equal(3, recorder.Count);
        Assert.Equal(1, recorder.Events[0].Event.Value);
        Assert.Equal(2, recorder.Events[1].Event.Value);
        Assert.Equal(3, recorder.Events[2].Event.Value);
    }

    [Fact]
    public void RecordEvent_WithSequenceNumbers_AssignsSequentialNumbers()
    {
        using var world = new World();
        var eventBus = world.Events;
        using var recorder = new EventRecorder<TestEvent>(eventBus);

        eventBus.Publish(new TestEvent(1, "A"));
        eventBus.Publish(new TestEvent(2, "B"));
        eventBus.Publish(new TestEvent(3, "C"));

        Assert.Equal(0, recorder.Events[0].SequenceNumber);
        Assert.Equal(1, recorder.Events[1].SequenceNumber);
        Assert.Equal(2, recorder.Events[2].SequenceNumber);
    }

    [Fact]
    public void RecordEvent_WithClock_RecordsTimestamps()
    {
        using var world = new World();
        var eventBus = world.Events;
        var clock = new TestClock();
        using var recorder = new EventRecorder<TestEvent>(eventBus, clock);

        clock.SetTime(10f);
        eventBus.Publish(new TestEvent(1, "A"));

        clock.SetTime(25f);
        eventBus.Publish(new TestEvent(2, "B"));

        Assert.Equal(10f, recorder.Events[0].Timestamp);
        Assert.Equal(25f, recorder.Events[1].Timestamp);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Count_WithNoEvents_ReturnsZero()
    {
        using var world = new World();
        using var recorder = new EventRecorder<TestEvent>(world.Events);

        Assert.Equal(0, recorder.Count);
    }

    [Fact]
    public void Count_WithEvents_ReturnsEventCount()
    {
        using var world = new World();
        using var recorder = new EventRecorder<TestEvent>(world.Events);

        world.Events.Publish(new TestEvent(1, "A"));
        world.Events.Publish(new TestEvent(2, "B"));

        Assert.Equal(2, recorder.Count);
    }

    [Fact]
    public void HasEvents_WithNoEvents_ReturnsFalse()
    {
        using var world = new World();
        using var recorder = new EventRecorder<TestEvent>(world.Events);

        Assert.False(recorder.HasEvents);
    }

    [Fact]
    public void HasEvents_WithEvents_ReturnsTrue()
    {
        using var world = new World();
        using var recorder = new EventRecorder<TestEvent>(world.Events);

        world.Events.Publish(new TestEvent(1, "A"));

        Assert.True(recorder.HasEvents);
    }

    [Fact]
    public void LastEvent_WithNoEvents_ReturnsDefault()
    {
        using var world = new World();
        using var recorder = new EventRecorder<TestEvent>(world.Events);

        var last = recorder.LastEvent;

        Assert.Equal(default(TestEvent), last);
    }

    [Fact]
    public void LastEvent_WithEvents_ReturnsLastEvent()
    {
        using var world = new World();
        using var recorder = new EventRecorder<TestEvent>(world.Events);

        world.Events.Publish(new TestEvent(1, "First"));
        world.Events.Publish(new TestEvent(2, "Second"));
        world.Events.Publish(new TestEvent(3, "Third"));

        Assert.Equal(3, recorder.LastEvent!.Value);
        Assert.Equal("Third", recorder.LastEvent.Message);
    }

    [Fact]
    public void LastRecordedEvent_WithNoEvents_ReturnsNull()
    {
        using var world = new World();
        using var recorder = new EventRecorder<TestEvent>(world.Events);

        Assert.Null(recorder.LastRecordedEvent);
    }

    [Fact]
    public void LastRecordedEvent_WithEvents_ReturnsLastRecordedEvent()
    {
        using var world = new World();
        var clock = new TestClock();
        using var recorder = new EventRecorder<TestEvent>(world.Events, clock);

        clock.SetTime(50f);
        world.Events.Publish(new TestEvent(1, "First"));
        world.Events.Publish(new TestEvent(2, "Last"));

        var last = recorder.LastRecordedEvent;
        Assert.NotNull(last);
        Assert.Equal(2, last.Value.Event.Value);
        Assert.Equal(50f, last.Value.Timestamp);
        Assert.Equal(1, last.Value.SequenceNumber);
    }

    [Fact]
    public void FirstEvent_WithNoEvents_ReturnsDefault()
    {
        using var world = new World();
        using var recorder = new EventRecorder<TestEvent>(world.Events);

        var first = recorder.FirstEvent;

        Assert.Equal(default(TestEvent), first);
    }

    [Fact]
    public void FirstEvent_WithEvents_ReturnsFirstEvent()
    {
        using var world = new World();
        using var recorder = new EventRecorder<TestEvent>(world.Events);

        world.Events.Publish(new TestEvent(1, "First"));
        world.Events.Publish(new TestEvent(2, "Second"));

        Assert.Equal(1, recorder.FirstEvent!.Value);
        Assert.Equal("First", recorder.FirstEvent.Message);
    }

    [Fact]
    public void FirstRecordedEvent_WithNoEvents_ReturnsNull()
    {
        using var world = new World();
        using var recorder = new EventRecorder<TestEvent>(world.Events);

        Assert.Null(recorder.FirstRecordedEvent);
    }

    [Fact]
    public void FirstRecordedEvent_WithEvents_ReturnsFirstRecordedEvent()
    {
        using var world = new World();
        var clock = new TestClock();
        using var recorder = new EventRecorder<TestEvent>(world.Events, clock);

        clock.SetTime(10f);
        world.Events.Publish(new TestEvent(1, "First"));
        clock.SetTime(20f);
        world.Events.Publish(new TestEvent(2, "Second"));

        var first = recorder.FirstRecordedEvent;
        Assert.NotNull(first);
        Assert.Equal(1, first.Value.Event.Value);
        Assert.Equal(10f, first.Value.Timestamp);
        Assert.Equal(0, first.Value.SequenceNumber);
    }

    #endregion

    #region Query Methods Tests

    [Fact]
    public void Where_WithPredicate_ReturnsMatchingEvents()
    {
        using var world = new World();
        using var recorder = new EventRecorder<TestEvent>(world.Events);

        world.Events.Publish(new TestEvent(10, "A"));
        world.Events.Publish(new TestEvent(20, "B"));
        world.Events.Publish(new TestEvent(30, "C"));

        var matching = recorder.Where(e => e.Value > 15).ToList();

        Assert.Equal(2, matching.Count);
        Assert.Equal(20, matching[0].Event.Value);
        Assert.Equal(30, matching[1].Event.Value);
    }

    [Fact]
    public void Where_WithNullPredicate_ThrowsArgumentNullException()
    {
        using var world = new World();
        using var recorder = new EventRecorder<TestEvent>(world.Events);

        Assert.Throws<ArgumentNullException>(() => recorder.Where(null!).ToList());
    }

    [Fact]
    public void WhereRecorded_WithPredicate_ReturnsMatchingRecordedEvents()
    {
        using var world = new World();
        var clock = new TestClock();
        using var recorder = new EventRecorder<TestEvent>(world.Events, clock);

        clock.SetTime(5f);
        world.Events.Publish(new TestEvent(1, "A"));
        clock.SetTime(15f);
        world.Events.Publish(new TestEvent(2, "B"));
        clock.SetTime(25f);
        world.Events.Publish(new TestEvent(3, "C"));

        var matching = recorder.WhereRecorded(e => e.Timestamp >= 10f).ToList();

        Assert.Equal(2, matching.Count);
        Assert.Equal(2, matching[0].Event.Value);
        Assert.Equal(3, matching[1].Event.Value);
    }

    [Fact]
    public void WhereRecorded_WithNullPredicate_ThrowsArgumentNullException()
    {
        using var world = new World();
        using var recorder = new EventRecorder<TestEvent>(world.Events);

        Assert.Throws<ArgumentNullException>(() => recorder.WhereRecorded(null!).ToList());
    }

    [Fact]
    public void Any_WithMatchingPredicate_ReturnsTrue()
    {
        using var world = new World();
        using var recorder = new EventRecorder<TestEvent>(world.Events);

        world.Events.Publish(new TestEvent(10, "A"));
        world.Events.Publish(new TestEvent(20, "B"));

        Assert.True(recorder.Any(e => e.Value == 20));
    }

    [Fact]
    public void Any_WithNonMatchingPredicate_ReturnsFalse()
    {
        using var world = new World();
        using var recorder = new EventRecorder<TestEvent>(world.Events);

        world.Events.Publish(new TestEvent(10, "A"));

        Assert.False(recorder.Any(e => e.Value == 100));
    }

    [Fact]
    public void Any_WithNullPredicate_ThrowsArgumentNullException()
    {
        using var world = new World();
        using var recorder = new EventRecorder<TestEvent>(world.Events);

        Assert.Throws<ArgumentNullException>(() => recorder.Any(null!));
    }

    [Fact]
    public void CountMatching_ReturnsMatchingCount()
    {
        using var world = new World();
        using var recorder = new EventRecorder<TestEvent>(world.Events);

        world.Events.Publish(new TestEvent(10, "A"));
        world.Events.Publish(new TestEvent(20, "B"));
        world.Events.Publish(new TestEvent(30, "C"));

        Assert.Equal(2, recorder.CountMatching(e => e.Value >= 20));
    }

    [Fact]
    public void CountMatching_WithNoMatches_ReturnsZero()
    {
        using var world = new World();
        using var recorder = new EventRecorder<TestEvent>(world.Events);

        world.Events.Publish(new TestEvent(10, "A"));

        Assert.Equal(0, recorder.CountMatching(e => e.Value > 100));
    }

    [Fact]
    public void CountMatching_WithNullPredicate_ThrowsArgumentNullException()
    {
        using var world = new World();
        using var recorder = new EventRecorder<TestEvent>(world.Events);

        Assert.Throws<ArgumentNullException>(() => recorder.CountMatching(null!));
    }

    #endregion

    #region Clear and Reset Tests

    [Fact]
    public void Clear_RemovesAllEvents()
    {
        using var world = new World();
        using var recorder = new EventRecorder<TestEvent>(world.Events);

        world.Events.Publish(new TestEvent(1, "A"));
        world.Events.Publish(new TestEvent(2, "B"));

        recorder.Clear();

        Assert.Empty(recorder.Events);
        Assert.Equal(0, recorder.Count);
        Assert.False(recorder.HasEvents);
    }

    [Fact]
    public void Clear_DoesNotResetSequenceCounter()
    {
        using var world = new World();
        using var recorder = new EventRecorder<TestEvent>(world.Events);

        world.Events.Publish(new TestEvent(1, "A"));
        world.Events.Publish(new TestEvent(2, "B"));

        recorder.Clear();

        world.Events.Publish(new TestEvent(3, "C"));

        Assert.Single(recorder.Events);
        Assert.Equal(2, recorder.Events[0].SequenceNumber);
    }

    [Fact]
    public void Reset_RemovesAllEvents()
    {
        using var world = new World();
        using var recorder = new EventRecorder<TestEvent>(world.Events);

        world.Events.Publish(new TestEvent(1, "A"));
        world.Events.Publish(new TestEvent(2, "B"));

        recorder.Reset();

        Assert.Empty(recorder.Events);
        Assert.Equal(0, recorder.Count);
    }

    [Fact]
    public void Reset_ResetsSequenceCounter()
    {
        using var world = new World();
        using var recorder = new EventRecorder<TestEvent>(world.Events);

        world.Events.Publish(new TestEvent(1, "A"));
        world.Events.Publish(new TestEvent(2, "B"));

        recorder.Reset();

        world.Events.Publish(new TestEvent(3, "C"));

        Assert.Single(recorder.Events);
        Assert.Equal(0, recorder.Events[0].SequenceNumber);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_StopsRecordingEvents()
    {
        using var world = new World();
        var recorder = new EventRecorder<TestEvent>(world.Events);

        world.Events.Publish(new TestEvent(1, "A"));
        recorder.Dispose();
        world.Events.Publish(new TestEvent(2, "B"));

        Assert.Single(recorder.Events);
    }

    [Fact]
    public void Dispose_MultipleTimes_DoesNotThrow()
    {
        using var world = new World();
        var recorder = new EventRecorder<TestEvent>(world.Events);

        recorder.Dispose();
        recorder.Dispose();
        recorder.Dispose();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void FullWorkflow_RecordFilterAndVerify()
    {
        using var world = new World();
        var clock = new TestClock();
        using var recorder = new EventRecorder<DamageEvent>(world.Events, clock);

        var entity1 = world.Spawn().Build();
        var entity2 = world.Spawn().Build();

        clock.SetTime(1f);
        world.Events.Publish(new DamageEvent(50, entity1));

        clock.SetTime(2f);
        world.Events.Publish(new DamageEvent(25, entity2));

        clock.SetTime(3f);
        world.Events.Publish(new DamageEvent(75, entity1));

        // Verify all events
        Assert.Equal(3, recorder.Count);

        // Filter by entity
        var entity1Damage = recorder.Where(e => e.Target == entity1).ToList();
        Assert.Equal(2, entity1Damage.Count);

        // Filter by amount
        var highDamage = recorder.Where(e => e.Amount >= 50).ToList();
        Assert.Equal(2, highDamage.Count);

        // Verify timestamps
        Assert.Equal(1f, recorder.Events[0].Timestamp);
        Assert.Equal(2f, recorder.Events[1].Timestamp);
        Assert.Equal(3f, recorder.Events[2].Timestamp);

        // Clear and verify new events
        recorder.Clear();
        world.Events.Publish(new DamageEvent(100, entity1));
        Assert.Single(recorder.Events);
    }

    #endregion
}

public class RecordedEventTests
{
    private record struct TestEvent(int Value);

    [Fact]
    public void Constructor_CreatesRecordWithAllFields()
    {
        var evt = new TestEvent(42);
        var recorded = new RecordedEvent<TestEvent>(evt, 123.45f, 7);

        Assert.Equal(42, recorded.Event.Value);
        Assert.Equal(123.45f, recorded.Timestamp);
        Assert.Equal(7, recorded.SequenceNumber);
    }

    [Fact]
    public void Equality_WithSameValues_AreEqual()
    {
        var evt = new TestEvent(42);
        var recorded1 = new RecordedEvent<TestEvent>(evt, 100f, 5);
        var recorded2 = new RecordedEvent<TestEvent>(evt, 100f, 5);

        Assert.Equal(recorded1, recorded2);
    }

    [Fact]
    public void Equality_WithDifferentValues_AreNotEqual()
    {
        var evt1 = new TestEvent(42);
        var evt2 = new TestEvent(43);
        var recorded1 = new RecordedEvent<TestEvent>(evt1, 100f, 5);
        var recorded2 = new RecordedEvent<TestEvent>(evt2, 100f, 5);

        Assert.NotEqual(recorded1, recorded2);
    }
}
