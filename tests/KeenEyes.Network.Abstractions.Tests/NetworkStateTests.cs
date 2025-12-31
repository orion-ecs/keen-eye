using KeenEyes.Network.Components;

namespace KeenEyes.Network.Abstractions.Tests;

/// <summary>
/// Tests for the <see cref="NetworkState"/> component.
/// </summary>
public class NetworkStateTests
{
    [Fact]
    public void NeedsFullSync_DefaultsToFalse()
    {
        var state = new NetworkState();

        Assert.False(state.NeedsFullSync);
    }

    [Fact]
    public void LastSentTick_DefaultsToZero()
    {
        var state = new NetworkState();

        Assert.Equal(0u, state.LastSentTick);
    }

    [Fact]
    public void LastReceivedTick_DefaultsToZero()
    {
        var state = new NetworkState();

        Assert.Equal(0u, state.LastReceivedTick);
    }

    [Fact]
    public void NeedsFullSync_CanBeSetToTrue()
    {
        var state = new NetworkState { NeedsFullSync = true };

        Assert.True(state.NeedsFullSync);
    }

    [Fact]
    public void LastSentTick_CanBeSet()
    {
        var state = new NetworkState { LastSentTick = 100 };

        Assert.Equal(100u, state.LastSentTick);
    }

    [Fact]
    public void LastReceivedTick_CanBeSet()
    {
        var state = new NetworkState { LastReceivedTick = 200 };

        Assert.Equal(200u, state.LastReceivedTick);
    }

    [Fact]
    public void AccumulatedPriority_DefaultsToZero()
    {
        var state = new NetworkState();

        Assert.Equal(0f, state.AccumulatedPriority);
    }

    [Fact]
    public void AccumulatedPriority_CanBeSet()
    {
        var state = new NetworkState { AccumulatedPriority = 5.5f };

        Assert.Equal(5.5f, state.AccumulatedPriority);
    }

    [Fact]
    public void NetworkState_AllPropertiesCanBeSetTogether()
    {
        var state = new NetworkState
        {
            LastSentTick = 100,
            LastReceivedTick = 200,
            AccumulatedPriority = 10f,
            NeedsFullSync = true
        };

        Assert.Equal(100u, state.LastSentTick);
        Assert.Equal(200u, state.LastReceivedTick);
        Assert.Equal(10f, state.AccumulatedPriority);
        Assert.True(state.NeedsFullSync);
    }
}

/// <summary>
/// Tests for the <see cref="SnapshotBuffer"/> class.
/// </summary>
public class SnapshotBufferTests
{
    private record struct TestPosition(float X, float Y);
    private record struct TestVelocity(float X, float Y);

    [Fact]
    public void SnapshotBuffer_InitialState_HasEmptyDictionaries()
    {
        var buffer = new SnapshotBuffer();

        Assert.Empty(buffer.FromSnapshots);
        Assert.Empty(buffer.ToSnapshots);
    }

    [Fact]
    public void PushSnapshot_FirstPush_OnlyAddsToToSnapshots()
    {
        var buffer = new SnapshotBuffer();
        var position = new TestPosition(1f, 2f);

        buffer.PushSnapshot(typeof(TestPosition), position);

        Assert.Empty(buffer.FromSnapshots);
        Assert.Single(buffer.ToSnapshots);
        Assert.Equal(position, buffer.ToSnapshots[typeof(TestPosition)]);
    }

    [Fact]
    public void PushSnapshot_SecondPush_MovesToFromAndAddNewTo()
    {
        var buffer = new SnapshotBuffer();
        var position1 = new TestPosition(1f, 2f);
        var position2 = new TestPosition(3f, 4f);

        buffer.PushSnapshot(typeof(TestPosition), position1);
        buffer.PushSnapshot(typeof(TestPosition), position2);

        Assert.Single(buffer.FromSnapshots);
        Assert.Single(buffer.ToSnapshots);
        Assert.Equal(position1, buffer.FromSnapshots[typeof(TestPosition)]);
        Assert.Equal(position2, buffer.ToSnapshots[typeof(TestPosition)]);
    }

    [Fact]
    public void PushSnapshot_MultipleComponentTypes_TrackedSeparately()
    {
        var buffer = new SnapshotBuffer();
        var position = new TestPosition(1f, 2f);
        var velocity = new TestVelocity(3f, 4f);

        buffer.PushSnapshot(typeof(TestPosition), position);
        buffer.PushSnapshot(typeof(TestVelocity), velocity);

        Assert.Equal(2, buffer.ToSnapshots.Count);
        Assert.Equal(position, buffer.ToSnapshots[typeof(TestPosition)]);
        Assert.Equal(velocity, buffer.ToSnapshots[typeof(TestVelocity)]);
    }

    [Fact]
    public void Clear_RemovesAllSnapshots()
    {
        var buffer = new SnapshotBuffer();
        var position1 = new TestPosition(1f, 2f);
        var position2 = new TestPosition(3f, 4f);

        buffer.PushSnapshot(typeof(TestPosition), position1);
        buffer.PushSnapshot(typeof(TestPosition), position2);

        buffer.Clear();

        Assert.Empty(buffer.FromSnapshots);
        Assert.Empty(buffer.ToSnapshots);
    }

    [Fact]
    public void PushSnapshot_ThreePushes_KeepsOnlyLastTwoSnapshots()
    {
        var buffer = new SnapshotBuffer();
        var position1 = new TestPosition(1f, 2f);
        var position2 = new TestPosition(3f, 4f);
        var position3 = new TestPosition(5f, 6f);

        buffer.PushSnapshot(typeof(TestPosition), position1);
        buffer.PushSnapshot(typeof(TestPosition), position2);
        buffer.PushSnapshot(typeof(TestPosition), position3);

        // position1 should be gone, position2 in From, position3 in To
        Assert.Equal(position2, buffer.FromSnapshots[typeof(TestPosition)]);
        Assert.Equal(position3, buffer.ToSnapshots[typeof(TestPosition)]);
    }
}
