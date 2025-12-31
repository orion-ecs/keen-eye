using KeenEyes.Network.Prediction;

namespace KeenEyes.Network.Abstractions.Tests;

/// <summary>
/// Tests for the <see cref="PredictionBuffer"/> class.
/// </summary>
public class PredictionBufferTests
{
    private struct TestComponent
    {
        public float X { get; set; }
        public float Y { get; set; }
    }

    [Fact]
    public void OldestTick_DefaultsToZero()
    {
        var buffer = new PredictionBuffer();

        Assert.Equal(0u, buffer.OldestTick);
    }

    [Fact]
    public void NewestTick_DefaultsToZero()
    {
        var buffer = new PredictionBuffer();

        Assert.Equal(0u, buffer.NewestTick);
    }

    [Fact]
    public void SaveState_SingleState_SetsOldestAndNewestTick()
    {
        var buffer = new PredictionBuffer();

        buffer.SaveState(5, typeof(TestComponent), new TestComponent { X = 1.0f, Y = 2.0f });

        Assert.Equal(5u, buffer.OldestTick);
        Assert.Equal(5u, buffer.NewestTick);
    }

    [Fact]
    public void SaveState_MultipleStates_UpdatesNewestTick()
    {
        var buffer = new PredictionBuffer();

        buffer.SaveState(1, typeof(TestComponent), new TestComponent { X = 1.0f });
        buffer.SaveState(2, typeof(TestComponent), new TestComponent { X = 2.0f });
        buffer.SaveState(3, typeof(TestComponent), new TestComponent { X = 3.0f });

        Assert.Equal(1u, buffer.OldestTick);
        Assert.Equal(3u, buffer.NewestTick);
    }

    [Fact]
    public void TryGetState_ExistingState_ReturnsTrue()
    {
        var buffer = new PredictionBuffer();
        var component = new TestComponent { X = 42.0f, Y = 24.0f };
        buffer.SaveState(5, typeof(TestComponent), component);

        var result = buffer.TryGetState(5, typeof(TestComponent), out var state);

        Assert.True(result);
        Assert.IsType<TestComponent>(state);
        var retrieved = (TestComponent)state!;
        Assert.Equal(42.0f, retrieved.X);
        Assert.Equal(24.0f, retrieved.Y);
    }

    [Fact]
    public void TryGetState_NonExistingTick_ReturnsFalse()
    {
        var buffer = new PredictionBuffer();
        buffer.SaveState(5, typeof(TestComponent), new TestComponent());

        var result = buffer.TryGetState(999, typeof(TestComponent), out _);

        Assert.False(result);
    }

    [Fact]
    public void TryGetState_NonExistingType_ReturnsFalse()
    {
        var buffer = new PredictionBuffer();
        buffer.SaveState(5, typeof(TestComponent), new TestComponent());

        var result = buffer.TryGetState(5, typeof(string), out _);

        Assert.False(result);
    }

    [Fact]
    public void GetStatesForTick_ExistingTick_ReturnsStates()
    {
        var buffer = new PredictionBuffer();
        buffer.SaveState(5, typeof(TestComponent), new TestComponent { X = 1.0f });
        buffer.SaveState(5, typeof(string), "test");

        var states = buffer.GetStatesForTick(5);

        Assert.NotNull(states);
        Assert.Equal(2, states.Count);
        Assert.True(states.ContainsKey(typeof(TestComponent)));
        Assert.True(states.ContainsKey(typeof(string)));
    }

    [Fact]
    public void GetStatesForTick_NonExistingTick_ReturnsNull()
    {
        var buffer = new PredictionBuffer();
        buffer.SaveState(5, typeof(TestComponent), new TestComponent());

        var states = buffer.GetStatesForTick(999);

        Assert.Null(states);
    }

    [Fact]
    public void RemoveOlderThan_RemovesOldStates()
    {
        var buffer = new PredictionBuffer();
        buffer.SaveState(1, typeof(TestComponent), new TestComponent { X = 1.0f });
        buffer.SaveState(2, typeof(TestComponent), new TestComponent { X = 2.0f });
        buffer.SaveState(3, typeof(TestComponent), new TestComponent { X = 3.0f });

        buffer.RemoveOlderThan(3);

        Assert.Equal(3u, buffer.OldestTick);
        Assert.Null(buffer.GetStatesForTick(1));
        Assert.Null(buffer.GetStatesForTick(2));
        Assert.NotNull(buffer.GetStatesForTick(3));
    }

    [Fact]
    public void Clear_ResetsBuffer()
    {
        var buffer = new PredictionBuffer();
        buffer.SaveState(1, typeof(TestComponent), new TestComponent { X = 1.0f });
        buffer.SaveState(2, typeof(TestComponent), new TestComponent { X = 2.0f });

        buffer.Clear();

        Assert.Equal(0u, buffer.OldestTick);
        Assert.Equal(0u, buffer.NewestTick);
        Assert.Null(buffer.GetStatesForTick(1));
        Assert.Null(buffer.GetStatesForTick(2));
    }

    [Fact]
    public void SaveState_ExceedsMaxTicks_CleansUpOldTicks()
    {
        var buffer = new PredictionBuffer(maxTicks: 4);

        // Save more states than maxTicks
        for (uint i = 1; i <= 6; i++)
        {
            buffer.SaveState(i, typeof(TestComponent), new TestComponent { X = i });
        }

        // Old ticks should be cleaned up
        Assert.Null(buffer.GetStatesForTick(1));
        Assert.Null(buffer.GetStatesForTick(2));

        // Recent ticks should exist
        Assert.NotNull(buffer.GetStatesForTick(5));
        Assert.NotNull(buffer.GetStatesForTick(6));
    }

    [Fact]
    public void SaveState_MultipleComponentsSameTick_StoresBothComponents()
    {
        var buffer = new PredictionBuffer();
        var comp1 = new TestComponent { X = 1.0f };
        var comp2 = "test string";

        buffer.SaveState(5, typeof(TestComponent), comp1);
        buffer.SaveState(5, typeof(string), comp2);

        buffer.TryGetState(5, typeof(TestComponent), out var state1);
        buffer.TryGetState(5, typeof(string), out var state2);

        Assert.Equal(1.0f, ((TestComponent)state1!).X);
        Assert.Equal("test string", state2);
    }

    [Fact]
    public void SaveState_OverwritesExistingStateForSameTickAndType()
    {
        var buffer = new PredictionBuffer();
        buffer.SaveState(5, typeof(TestComponent), new TestComponent { X = 1.0f });
        buffer.SaveState(5, typeof(TestComponent), new TestComponent { X = 99.0f });

        buffer.TryGetState(5, typeof(TestComponent), out var state);

        Assert.Equal(99.0f, ((TestComponent)state!).X);
    }

    [Fact]
    public void RemoveOlderThan_RemovesAllStates_ResetsToZero()
    {
        var buffer = new PredictionBuffer();
        buffer.SaveState(1, typeof(TestComponent), new TestComponent { X = 1.0f });
        buffer.SaveState(2, typeof(TestComponent), new TestComponent { X = 2.0f });
        buffer.SaveState(3, typeof(TestComponent), new TestComponent { X = 3.0f });

        // Remove all states by specifying tick beyond newest
        buffer.RemoveOlderThan(10);

        Assert.Equal(0u, buffer.OldestTick);
        Assert.Equal(0u, buffer.NewestTick);
        Assert.Null(buffer.GetStatesForTick(1));
        Assert.Null(buffer.GetStatesForTick(2));
        Assert.Null(buffer.GetStatesForTick(3));
    }

    [Fact]
    public void CleanupOldTicks_WithNonContiguousTicks_FindsNextOldest()
    {
        // Use a small buffer to force cleanup
        var buffer = new PredictionBuffer(maxTicks: 3);

        // Add non-contiguous ticks (gaps in sequence)
        buffer.SaveState(1, typeof(TestComponent), new TestComponent { X = 1.0f });
        buffer.SaveState(3, typeof(TestComponent), new TestComponent { X = 3.0f });
        buffer.SaveState(5, typeof(TestComponent), new TestComponent { X = 5.0f });

        // Add more to trigger cleanup
        buffer.SaveState(7, typeof(TestComponent), new TestComponent { X = 7.0f });
        buffer.SaveState(9, typeof(TestComponent), new TestComponent { X = 9.0f });

        // Old ticks should be cleaned up
        Assert.Null(buffer.GetStatesForTick(1));
        Assert.Null(buffer.GetStatesForTick(3));

        // Recent ticks should exist
        Assert.NotNull(buffer.GetStatesForTick(7));
        Assert.NotNull(buffer.GetStatesForTick(9));
    }
}
