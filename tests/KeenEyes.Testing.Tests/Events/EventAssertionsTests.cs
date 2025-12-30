using KeenEyes.Events;
using KeenEyes.Testing.Events;

namespace KeenEyes.Testing.Tests.Events;

public class EventAssertionsTests
{
    private sealed record TestEvent(int Value, string Name);

    private static (EventBus bus, EventRecorder<TestEvent> recorder) CreateRecorder()
    {
        var bus = new EventBus();
        var recorder = new EventRecorder<TestEvent>(bus);
        return (bus, recorder);
    }

    #region ShouldHaveFired

    [Fact]
    public void ShouldHaveFired_WhenHasEvents_Passes()
    {
        var (bus, recorder) = CreateRecorder();
        bus.Publish(new TestEvent(1, "Test"));

        var result = recorder.ShouldHaveFired();

        Assert.Same(recorder, result);
        recorder.Dispose();
    }

    [Fact]
    public void ShouldHaveFired_WhenNoEvents_Throws()
    {
        var (_, recorder) = CreateRecorder();

        var exception = Assert.Throws<EventAssertionException>(() => recorder.ShouldHaveFired());
        Assert.Contains("TestEvent", exception.Message);
        recorder.Dispose();
    }

    [Fact]
    public void ShouldHaveFired_WithNullRecorder_ThrowsArgumentNull()
    {
        EventRecorder<TestEvent>? recorder = null;

        Assert.Throws<ArgumentNullException>(() => recorder!.ShouldHaveFired());
    }

    #endregion

    #region ShouldNotHaveFired

    [Fact]
    public void ShouldNotHaveFired_WhenNoEvents_Passes()
    {
        var (_, recorder) = CreateRecorder();

        var result = recorder.ShouldNotHaveFired();

        Assert.Same(recorder, result);
        recorder.Dispose();
    }

    [Fact]
    public void ShouldNotHaveFired_WhenHasEvents_Throws()
    {
        var (bus, recorder) = CreateRecorder();
        bus.Publish(new TestEvent(1, "Test"));

        var exception = Assert.Throws<EventAssertionException>(() => recorder.ShouldNotHaveFired());
        Assert.Contains("1 event(s)", exception.Message);
        recorder.Dispose();
    }

    #endregion

    #region ShouldHaveFiredTimes

    [Fact]
    public void ShouldHaveFiredTimes_WhenCountMatches_Passes()
    {
        var (bus, recorder) = CreateRecorder();
        bus.Publish(new TestEvent(1, "A"));
        bus.Publish(new TestEvent(2, "B"));
        bus.Publish(new TestEvent(3, "C"));

        var result = recorder.ShouldHaveFiredTimes(3);

        Assert.Same(recorder, result);
        recorder.Dispose();
    }

    [Fact]
    public void ShouldHaveFiredTimes_WhenCountDoesNotMatch_Throws()
    {
        var (bus, recorder) = CreateRecorder();
        bus.Publish(new TestEvent(1, "Test"));

        var exception = Assert.Throws<EventAssertionException>(() => recorder.ShouldHaveFiredTimes(5));
        Assert.Contains("Expected 5", exception.Message);
        Assert.Contains("1 event(s)", exception.Message);
        recorder.Dispose();
    }

    #endregion

    #region ShouldHaveFiredAtLeast

    [Fact]
    public void ShouldHaveFiredAtLeast_WhenEnoughEvents_Passes()
    {
        var (bus, recorder) = CreateRecorder();
        bus.Publish(new TestEvent(1, "A"));
        bus.Publish(new TestEvent(2, "B"));
        bus.Publish(new TestEvent(3, "C"));

        var result = recorder.ShouldHaveFiredAtLeast(2);

        Assert.Same(recorder, result);
        recorder.Dispose();
    }

    [Fact]
    public void ShouldHaveFiredAtLeast_WhenNotEnoughEvents_Throws()
    {
        var (bus, recorder) = CreateRecorder();
        bus.Publish(new TestEvent(1, "Test"));

        var exception = Assert.Throws<EventAssertionException>(() => recorder.ShouldHaveFiredAtLeast(5));
        Assert.Contains("at least 5", exception.Message);
        recorder.Dispose();
    }

    #endregion

    #region ShouldHaveFiredAtMost

    [Fact]
    public void ShouldHaveFiredAtMost_WhenNotTooMany_Passes()
    {
        var (bus, recorder) = CreateRecorder();
        bus.Publish(new TestEvent(1, "A"));
        bus.Publish(new TestEvent(2, "B"));

        var result = recorder.ShouldHaveFiredAtMost(3);

        Assert.Same(recorder, result);
        recorder.Dispose();
    }

    [Fact]
    public void ShouldHaveFiredAtMost_WhenTooMany_Throws()
    {
        var (bus, recorder) = CreateRecorder();
        bus.Publish(new TestEvent(1, "A"));
        bus.Publish(new TestEvent(2, "B"));
        bus.Publish(new TestEvent(3, "C"));
        bus.Publish(new TestEvent(4, "D"));

        var exception = Assert.Throws<EventAssertionException>(() => recorder.ShouldHaveFiredAtMost(2));
        Assert.Contains("at most 2", exception.Message);
        recorder.Dispose();
    }

    #endregion

    #region ShouldHaveFiredMatching

    [Fact]
    public void ShouldHaveFiredMatching_WhenMatchExists_Passes()
    {
        var (bus, recorder) = CreateRecorder();
        bus.Publish(new TestEvent(5, "Low"));
        bus.Publish(new TestEvent(15, "High"));
        bus.Publish(new TestEvent(3, "VeryLow"));

        var result = recorder.ShouldHaveFiredMatching(e => e.Value > 10);

        Assert.Same(recorder, result);
        recorder.Dispose();
    }

    [Fact]
    public void ShouldHaveFiredMatching_WhenNoMatch_Throws()
    {
        var (bus, recorder) = CreateRecorder();
        bus.Publish(new TestEvent(1, "A"));
        bus.Publish(new TestEvent(2, "B"));

        var exception = Assert.Throws<EventAssertionException>(() =>
            recorder.ShouldHaveFiredMatching(e => e.Value > 100));
        Assert.Contains("no matching events", exception.Message);
        recorder.Dispose();
    }

    [Fact]
    public void ShouldHaveFiredMatching_WithNullPredicate_ThrowsArgumentNull()
    {
        var (_, recorder) = CreateRecorder();

        Assert.Throws<ArgumentNullException>(() => recorder.ShouldHaveFiredMatching(null!));
        recorder.Dispose();
    }

    #endregion

    #region ShouldNotHaveFiredMatching

    [Fact]
    public void ShouldNotHaveFiredMatching_WhenNoMatchExists_Passes()
    {
        var (bus, recorder) = CreateRecorder();
        bus.Publish(new TestEvent(1, "A"));
        bus.Publish(new TestEvent(2, "B"));

        var result = recorder.ShouldNotHaveFiredMatching(e => e.Value > 100);

        Assert.Same(recorder, result);
        recorder.Dispose();
    }

    [Fact]
    public void ShouldNotHaveFiredMatching_WhenMatchExists_Throws()
    {
        var (bus, recorder) = CreateRecorder();
        bus.Publish(new TestEvent(50, "High"));
        bus.Publish(new TestEvent(150, "VeryHigh"));

        var exception = Assert.Throws<EventAssertionException>(() =>
            recorder.ShouldNotHaveFiredMatching(e => e.Value > 100));
        Assert.Contains("1 matching event(s)", exception.Message);
        recorder.Dispose();
    }

    #endregion

    #region ShouldHaveFiredMatchingTimes

    [Fact]
    public void ShouldHaveFiredMatchingTimes_WhenCountMatches_Passes()
    {
        var (bus, recorder) = CreateRecorder();
        bus.Publish(new TestEvent(5, "Low"));
        bus.Publish(new TestEvent(15, "High"));
        bus.Publish(new TestEvent(25, "VeryHigh"));
        bus.Publish(new TestEvent(3, "VeryLow"));

        var result = recorder.ShouldHaveFiredMatchingTimes(2, e => e.Value > 10);

        Assert.Same(recorder, result);
        recorder.Dispose();
    }

    [Fact]
    public void ShouldHaveFiredMatchingTimes_WhenCountDoesNotMatch_Throws()
    {
        var (bus, recorder) = CreateRecorder();
        bus.Publish(new TestEvent(15, "High"));

        var exception = Assert.Throws<EventAssertionException>(() =>
            recorder.ShouldHaveFiredMatchingTimes(5, e => e.Value > 10));
        Assert.Contains("Expected 5", exception.Message);
        recorder.Dispose();
    }

    #endregion

    #region ShouldHaveFiredInOrder

    [Fact]
    public void ShouldHaveFiredInOrder_WhenOrderMatches_Passes()
    {
        var (bus, recorder) = CreateRecorder();
        bus.Publish(new TestEvent(1, "First"));
        bus.Publish(new TestEvent(2, "Second"));
        bus.Publish(new TestEvent(3, "Third"));

        var result = recorder.ShouldHaveFiredInOrder(
            e => e.Name == "First",
            e => e.Name == "Second",
            e => e.Name == "Third");

        Assert.Same(recorder, result);
        recorder.Dispose();
    }

    [Fact]
    public void ShouldHaveFiredInOrder_AllowsOtherEventsBetween()
    {
        var (bus, recorder) = CreateRecorder();
        bus.Publish(new TestEvent(1, "First"));
        bus.Publish(new TestEvent(99, "Other"));
        bus.Publish(new TestEvent(2, "Second"));

        var result = recorder.ShouldHaveFiredInOrder(
            e => e.Name == "First",
            e => e.Name == "Second");

        Assert.Same(recorder, result);
        recorder.Dispose();
    }

    [Fact]
    public void ShouldHaveFiredInOrder_WhenOrderWrong_Throws()
    {
        var (bus, recorder) = CreateRecorder();
        bus.Publish(new TestEvent(2, "Second"));
        bus.Publish(new TestEvent(1, "First"));

        var exception = Assert.Throws<EventAssertionException>(() =>
            recorder.ShouldHaveFiredInOrder(
                e => e.Name == "First",
                e => e.Name == "Second"));
        Assert.Contains("Matched 1 of 2", exception.Message);
        recorder.Dispose();
    }

    [Fact]
    public void ShouldHaveFiredInOrder_WithEmptyPredicates_Passes()
    {
        var (bus, recorder) = CreateRecorder();
        bus.Publish(new TestEvent(1, "Test"));

        var result = recorder.ShouldHaveFiredInOrder();

        Assert.Same(recorder, result);
        recorder.Dispose();
    }

    #endregion

    #region ShouldHaveFiredExactlyInOrder

    [Fact]
    public void ShouldHaveFiredExactlyInOrder_WhenExactMatch_Passes()
    {
        var (bus, recorder) = CreateRecorder();
        bus.Publish(new TestEvent(1, "A"));
        bus.Publish(new TestEvent(2, "B"));

        var result = recorder.ShouldHaveFiredExactlyInOrder(
            e => e.Name == "A",
            e => e.Name == "B");

        Assert.Same(recorder, result);
        recorder.Dispose();
    }

    [Fact]
    public void ShouldHaveFiredExactlyInOrder_WhenCountMismatch_Throws()
    {
        var (bus, recorder) = CreateRecorder();
        bus.Publish(new TestEvent(1, "A"));
        bus.Publish(new TestEvent(2, "B"));
        bus.Publish(new TestEvent(3, "C"));

        var exception = Assert.Throws<EventAssertionException>(() =>
            recorder.ShouldHaveFiredExactlyInOrder(
                e => e.Name == "A",
                e => e.Name == "B"));
        Assert.Contains("exactly 2", exception.Message);
        recorder.Dispose();
    }

    [Fact]
    public void ShouldHaveFiredExactlyInOrder_WhenPredicateDoesNotMatch_Throws()
    {
        var (bus, recorder) = CreateRecorder();
        bus.Publish(new TestEvent(1, "A"));
        bus.Publish(new TestEvent(2, "Wrong"));

        var exception = Assert.Throws<EventAssertionException>(() =>
            recorder.ShouldHaveFiredExactlyInOrder(
                e => e.Name == "A",
                e => e.Name == "B"));
        Assert.Contains("index 1", exception.Message);
        recorder.Dispose();
    }

    #endregion

    #region LastEventShouldMatch

    [Fact]
    public void LastEventShouldMatch_WhenMatches_Passes()
    {
        var (bus, recorder) = CreateRecorder();
        bus.Publish(new TestEvent(1, "First"));
        bus.Publish(new TestEvent(2, "Last"));

        var result = recorder.LastEventShouldMatch(e => e.Name == "Last");

        Assert.Same(recorder, result);
        recorder.Dispose();
    }

    [Fact]
    public void LastEventShouldMatch_WhenNoEvents_Throws()
    {
        var (_, recorder) = CreateRecorder();

        var exception = Assert.Throws<EventAssertionException>(() =>
            recorder.LastEventShouldMatch(e => e.Value > 0));
        Assert.Contains("no events were recorded", exception.Message);
        recorder.Dispose();
    }

    [Fact]
    public void LastEventShouldMatch_WhenDoesNotMatch_Throws()
    {
        var (bus, recorder) = CreateRecorder();
        bus.Publish(new TestEvent(1, "Wrong"));

        var exception = Assert.Throws<EventAssertionException>(() =>
            recorder.LastEventShouldMatch(e => e.Name == "Expected"));
        Assert.Contains("Last event", exception.Message);
        Assert.Contains("did not match", exception.Message);
        recorder.Dispose();
    }

    #endregion

    #region FirstEventShouldMatch

    [Fact]
    public void FirstEventShouldMatch_WhenMatches_Passes()
    {
        var (bus, recorder) = CreateRecorder();
        bus.Publish(new TestEvent(1, "First"));
        bus.Publish(new TestEvent(2, "Second"));

        var result = recorder.FirstEventShouldMatch(e => e.Name == "First");

        Assert.Same(recorder, result);
        recorder.Dispose();
    }

    [Fact]
    public void FirstEventShouldMatch_WhenNoEvents_Throws()
    {
        var (_, recorder) = CreateRecorder();

        var exception = Assert.Throws<EventAssertionException>(() =>
            recorder.FirstEventShouldMatch(e => e.Value > 0));
        Assert.Contains("no events were recorded", exception.Message);
        recorder.Dispose();
    }

    [Fact]
    public void FirstEventShouldMatch_WhenDoesNotMatch_Throws()
    {
        var (bus, recorder) = CreateRecorder();
        bus.Publish(new TestEvent(1, "Wrong"));

        var exception = Assert.Throws<EventAssertionException>(() =>
            recorder.FirstEventShouldMatch(e => e.Name == "Expected"));
        Assert.Contains("First event", exception.Message);
        Assert.Contains("did not match", exception.Message);
        recorder.Dispose();
    }

    #endregion
}

public class EventAssertionExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        var ex = new EventAssertionException("Test message");

        Assert.Equal("Test message", ex.Message);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_SetsBoth()
    {
        var inner = new Exception("Inner");
        var ex = new EventAssertionException("Test message", inner);

        Assert.Equal("Test message", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }
}
